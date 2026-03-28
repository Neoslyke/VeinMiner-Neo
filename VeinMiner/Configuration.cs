using Newtonsoft.Json;
using Terraria.ID;
using TShockAPI;

namespace VeinMiner;

public class Configuration
{
    private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "VeinMiner.json");

    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("PutIntoInventory")]
    public bool PutIntoInventory { get; set; } = true;

    [JsonProperty("MaxVeinSize")]
    public int MaxVeinSize { get; set; } = 5000;

    [JsonProperty("TargetTiles")]
    public List<int> TargetTiles { get; set; } = new();

    [JsonProperty("IgnoreAboveTiles")]
    public List<int> IgnoreAboveTiles { get; set; } = new();

    [JsonProperty("Rewards")]
    public List<VeinReward> Rewards { get; set; } = new();

    public static Configuration Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var config = new Configuration
                {
                    TargetTiles = new List<int>
                    {
                        // Basic Ores
                        TileID.Copper,
                        TileID.Tin,
                        TileID.Iron,
                        TileID.Lead,
                        TileID.Silver,
                        TileID.Tungsten,
                        TileID.Gold,
                        TileID.Platinum,

                        // Pre-Hardmode Ores
                        TileID.Meteorite,
                        TileID.Demonite,
                        TileID.Crimtane,
                        TileID.Obsidian,
                        TileID.Hellstone,

                        // Hardmode Ores
                        TileID.Cobalt,
                        TileID.Palladium,
                        TileID.Mythril,
                        TileID.Orichalcum,
                        TileID.Adamantite,
                        TileID.Titanium,
                        TileID.Chlorophyte,

                        // Gems
                        TileID.ExposedGems,
                        TileID.Amethyst,
                        TileID.Topaz,
                        TileID.Sapphire,
                        TileID.Emerald,
                        TileID.Ruby,
                        TileID.Diamond,

                        // Other
                        TileID.DesertFossil
                    },
                    IgnoreAboveTiles = new List<int>(),
                    Rewards = new List<VeinReward>()
                };
                config.Save();
                return config;
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<Configuration>(json) ?? new Configuration();
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[VeinMiner] Error loading config: {ex.Message}");
            return new Configuration();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[VeinMiner] Error saving config: {ex.Message}");
        }
    }
}