using Microsoft.Xna.Framework;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace VeinMiner;

[ApiVersion(2, 1)]
public class VeinMiner : TerrariaPlugin
{
    public override string Name => "VeinMiner";
    public override string Author => "Neoslyke, Megghy, YSpoof, Maxthegreat99, 肝帝熙恩, Cai";
    public override Version Version => new Version(2, 1, 0);
    public override string Description => "Mine entire ore veins at once.";

    public static Configuration Config { get; private set; } = new();

    private const string DataKey = "VeinMiner";

    public VeinMiner(Main game) : base(game) { }

    public override void Initialize()
    {
        Config = Configuration.Load();

        Commands.ChatCommands.Add(new Command("veinminer.use", VeinMinerCommand, "veinminer", "vm")
        {
            HelpText = "Toggles VeinMiner on/off. Use /vm msg to toggle messages only."
        });

        GetDataHandlers.TileEdit += OnTileEdit;
        GeneralHooks.ReloadEvent += OnReload;
        ServerApi.Hooks.ServerJoin.Register(this, OnPlayerJoin);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Commands.ChatCommands.RemoveAll(c => c.CommandDelegate == VeinMinerCommand);
            GetDataHandlers.TileEdit -= OnTileEdit;
            GeneralHooks.ReloadEvent -= OnReload;
            ServerApi.Hooks.ServerJoin.Deregister(this, OnPlayerJoin);
        }
        base.Dispose(disposing);
    }

    private void OnReload(ReloadEventArgs args)
    {
        Config = Configuration.Load();
        args.Player?.SendSuccessMessage("[VeinMiner] Configuration reloaded.");
    }

    private void VeinMinerCommand(CommandArgs args)
    {
        var player = args.Player;
        var status = player.GetData<PlayerStatus>(DataKey);

        if (status == null)
        {
            status = new PlayerStatus();
            player.SetData(DataKey, status);
        }

        if (args.Parameters.Count >= 1 && args.Parameters[0].ToLower() == "msg")
        {
            status.ShowMessages = !status.ShowMessages;
            player.SendSuccessMessage($"[VeinMiner] Mining messages {(status.ShowMessages ? "enabled" : "disabled")}.");
        }
        else
        {
            status.Enabled = !status.Enabled;
            player.SendSuccessMessage($"[VeinMiner] {(status.Enabled ? "Enabled" : "Disabled")}. Use /vm msg to toggle messages.");
        }
    }

    private static void OnPlayerJoin(JoinEventArgs args)
    {
        var player = TShock.Players[args.Who];
        if (player != null)
        {
            player.SetData(DataKey, new PlayerStatus());
        }
    }

    private static void OnTileEdit(object? sender, GetDataHandlers.TileEditEventArgs args)
    {
        if (!Config.Enable) return;

        var tile = Main.tile[args.X, args.Y];
        if (tile == null) return;

        if (!args.Player.HasPermission("veinminer.use")) return;

        var status = args.Player.GetData<PlayerStatus>(DataKey);
        if (status == null || !status.Enabled) return;

        if (!Config.TargetTiles.Contains(tile.type)) return;

        if (args.Action != GetDataHandlers.EditAction.KillTile || args.EditData != 0) return;

        args.Handled = true;
        MineVein(args.Player, args.X, args.Y, tile.type);
    }

    private static void MineVein(TSPlayer player, int x, int y, int tileType)
    {
        var vein = GetVein(new List<Point>(), x, y, tileType);
        var item = InventoryHelper.GetItemFromTile(x, y);
        var status = player.GetData<PlayerStatus>(DataKey);

        // Filter out tiles with ignored blocks above them
        var mineableVein = vein
            .Where(p => p.Y <= 0 || !Config.IgnoreAboveTiles.Contains(Main.tile[p.X, p.Y - 1].type))
            .ToList();

        int mineableCount = mineableVein.Count;

        if (mineableCount == 0)
        {
            if (vein.Count > 0)
            {
                player.SendWarningMessage("[VeinMiner] Cannot mine vein - blocked by protected tiles above.");
            }
            return;
        }

        // Check for rewards
        var matchingRewards = Config.Rewards
            .Where(r => r.TileType == tileType && mineableCount >= r.MinVeinSize)
            .ToList();

        if (matchingRewards.Count > 0)
        {
            foreach (var reward in matchingRewards)
            {
                if (reward.Items.Count > player.GetEmptySlots())
                {
                    player.SendErrorMessage($"[VeinMiner] Inventory full. Need {reward.Items.Count} empty slot(s) for rewards.");
                    player.SendTileSquareCentered(x, y, 1);
                    return;
                }

                foreach (var (itemId, amount) in reward.Items)
                {
                    player.GiveItem(itemId, amount);
                }

                if (reward.OnlyGiveReward)
                {
                    int killed = KillTiles(mineableVein, true);
                    if (status?.ShowMessages == true)
                        player.SendSuccessMessage($"[VeinMiner] Mined {killed} tile(s) and received bonus rewards.");
                    return;
                }
            }
        }

        // Normal mining
        GiveItems(player, item, mineableVein, status);
    }

    private static void GiveItems(TSPlayer player, Item item, List<Point> vein, PlayerStatus? status)
    {
        int count = vein.Count;
        string itemName = item.type == 0 ? "Unknown" : item.Name;

        if (status?.ShowMessages == true && count > 1)
        {
            player.SendInfoMessage($"[VeinMiner] Mining {count} {itemName}...");
        }

        if (Config.PutIntoInventory)
        {
            if (player.HasSpaceFor(item.type, count))
            {
                int mined = KillTiles(vein, true);
                player.GiveItem(item.type, mined);

                if (status?.ShowMessages == true)
                    player.SendSuccessMessage($"[VeinMiner] Mined {mined} {itemName}.");

                TShock.Log.Info($"[VeinMiner] {player.Name} mined {mined} {itemName}.");
            }
            else
            {
                WorldGen.KillTile(vein[0].X, vein[0].Y);
                player.SendErrorMessage($"[VeinMiner] Inventory full. Need space for {count} {itemName}.");
            }
        }
        else
        {
            int mined = KillTiles(vein, false);
            if (status?.ShowMessages == true)
                player.SendSuccessMessage($"[VeinMiner] Mined {mined} {itemName}.");
        }
    }

    private static int KillTiles(List<Point> tiles, bool noItemDrop)
    {
        if (tiles.Count == 0) return 0;

        int killed = 0;

        foreach (var point in tiles)
        {
            WorldGen.KillTile(point.X, point.Y, false, false, noItemDrop);
            NetMessage.SendData((int)PacketTypes.Tile, -1, -1, null, 4, point.X, point.Y, 0);
        }

        foreach (var point in tiles)
        {
            var tile = Main.tile[point.X, point.Y];
            if (tile == null || !tile.active())
            {
                killed++;
            }
        }

        return killed;
    }

    private static List<Point> GetVein(List<Point> vein, int x, int y, int tileType)
    {
        if (vein.Count >= Config.MaxVeinSize) return vein;

        if (vein.Any(p => p.X == x && p.Y == y)) return vein;

        var tile = Main.tile[x, y];
        if (tile == null || !tile.active() || tile.type != tileType) return vein;

        vein.Add(new Point(x, y));

        // Check all 8 directions
        vein = GetVein(vein, x + 1, y, tileType);
        vein = GetVein(vein, x - 1, y, tileType);
        vein = GetVein(vein, x, y + 1, tileType);
        vein = GetVein(vein, x, y - 1, tileType);
        vein = GetVein(vein, x + 1, y + 1, tileType);
        vein = GetVein(vein, x + 1, y - 1, tileType);
        vein = GetVein(vein, x - 1, y + 1, tileType);
        vein = GetVein(vein, x - 1, y - 1, tileType);

        return vein;
    }
}