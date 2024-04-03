using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Phos.Data;

public class Room
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("state")] public State State { get; set; }

    [JsonPropertyName("connectedDevices")] public List<Device> ConnectedDevices { get; set; }
}