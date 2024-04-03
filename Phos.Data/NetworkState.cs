using System.Text.Json.Serialization;

namespace Phos.Data;

public class NetworkState
{
    [JsonPropertyName("rooms")] public List<Room> Rooms { get; set; }

    [JsonPropertyName("devices")] public List<Device> Devices { get; set; }
}