using System.Text.Json.Serialization;

namespace Phos.Data;

public class Device
{
    [JsonPropertyName("id")] public string Id { get; set; }
    [JsonPropertyName("socketSessionId")] public string SocketSessionId { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("isLedstrip")] public bool IsLedstrip { get; set; }
    [JsonPropertyName("isConnected")] public bool IsConnected { get; set; }
    [JsonPropertyName("ledCount")] public int LedCount { get; set; }
    [JsonPropertyName("room")] public Room Room { get; set; }
}