using System.Text.Json.Serialization;

namespace NTComponents.Virtualization;

internal sealed class NTVirtualizeAnchorSnapshot {
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("offset")]
    public double Offset { get; set; }

    [JsonPropertyName("atStart")]
    public bool AtStart { get; set; }

    [JsonPropertyName("atEnd")]
    public bool AtEnd { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }
}
