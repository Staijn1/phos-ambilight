namespace Phos.Data;

public class ConnectedDevice
{
    public string Id { get; set; }
    public string SocketSessionId { get; set; }
    public string Name { get; set; }
    public bool IsLedstrip { get; set; }
    public bool IsConnected { get; set; }
    public int LedCount { get; set; }
}