namespace Phos.Connections;

using global::SocketIOClient;

public class PhosSocketIOClient
{
    private readonly SocketIO client;

    public PhosSocketIOClient(string serverUrl, SocketIOOptions options = null, bool autoConnect = true)
    {
        client = new SocketIO(serverUrl, options);

        if (autoConnect)
        {
            Connect();
        }

        client.OnConnected += this.OnConnected;
    }

    protected virtual void OnConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("Connected to server!");
    }

    private void Connect()
    {
        client.ConnectAsync();
    }
}