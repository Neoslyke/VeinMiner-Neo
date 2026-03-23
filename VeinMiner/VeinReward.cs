using Newtonsoft.Json;

namespace VeinMiner;

public class VeinReward
{
    [JsonProperty("OnlyGiveReward")]
    public bool OnlyGiveReward { get; set; } = false;

    [JsonProperty("MinVeinSize")]
    public int MinVeinSize { get; set; } = 1;

    [JsonProperty("TileType")]
    public int TileType { get; set; } = 0;

    [JsonProperty("Items")]
    public Dictionary<int, int> Items { get; set; } = new();
}