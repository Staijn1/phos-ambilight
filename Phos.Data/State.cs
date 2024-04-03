using System.Text.Json.Serialization;

namespace Phos.Data;

public class State
{
    [JsonPropertyName("brightness")] public int Brightness { get; set; }

    [JsonPropertyName("colors")] public List<string> Colors { get; set; } = new List<string>();

    [JsonPropertyName("fftValue")] public int FftValue { get; set; }

    [JsonPropertyName("mode")] public int Mode { get; set; }

    [JsonPropertyName("speed")] public int Speed { get; set; }
}