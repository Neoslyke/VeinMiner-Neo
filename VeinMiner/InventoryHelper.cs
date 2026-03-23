using Terraria;
using TShockAPI;

namespace VeinMiner;

public static class InventoryHelper
{
    public static int GetEmptySlots(this TSPlayer player)
    {
        int count = 0;
        foreach (var slot in player.TPlayer.inventory)
        {
            if (slot.type == 0)
                count++;
        }
        return count;
    }

    public static bool HasSpaceFor(this TSPlayer player, int itemId, int stack)
    {
        int available = 0;
        var item = new Item();
        item.SetDefaults(itemId);

        for (int i = 0; i < 50; i++)
        {
            var slot = player.TPlayer.inventory[i];

            if (slot.type == itemId)
            {
                available += slot.maxStack - slot.stack;
            }
            else if (slot.type == 0)
            {
                available += item.maxStack;
            }

            if (available >= stack)
                return true;
        }

        return available >= stack;
    }

    public static Item GetItemFromTile(int x, int y)
    {
        WorldGen.KillTile_GetItemDrops(x, y, Main.tile[x, y],
            out int id, out int stack, out _, out _, out _);

        var item = new Item();
        item.SetDefaults(id);
        item.stack = stack;
        return item;
    }
}