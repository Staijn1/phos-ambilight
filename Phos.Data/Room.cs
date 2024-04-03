namespace Phos.Data;

public class Room
{
    public string Id { get; set; }
    public string Name { get; set; }
    public State State { get; set; }
    public List<ConnectedDevice> ConnectedDevices { get; set; }
}