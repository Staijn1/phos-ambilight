namespace Phos.Connections;

using global::SocketIOClient;

public class PhosSocketIOClient
{
    protected readonly SocketIO client;
    public event EventHandler OnConnect;
    public EventHandler<SocketIOResponse> OnDatabaseChange;

    public PhosSocketIOClient(string serverUrl, SocketIOOptions options = null, bool autoConnect = true)
    {
        client = new SocketIO(serverUrl, options);

        if (autoConnect)
        {
            Connect();
        }

        client.OnConnected += this.OnConnected;
        client.On(PhosSocketMessage.DatabaseChange, async response => this.OnDatabaseChange.Invoke(this, response));
    }



    public virtual void OnConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("Connected to server!");
        OnConnect.Invoke(this, EventArgs.Empty);
    }

    private void Connect()
    {
        client.ConnectAsync();
    }

    /// <summary>
    /// Send a message to the server by emitting an event, and wrapping the payload in an object that targets the specified room id's
    /// </summary>
    /// <param name="eventName">Should be a type of PhosSocketMessage</param>
    /// <param name="rooms"></param>
    /// <param name="payload"></param>
    public Task<SocketIOResponse> SendEvent(string eventName, List<string> rooms = null, object payload = null)
    {
        object data = null;
        if (rooms != null || payload != null)
        {
            data = new
            {
                rooms,
                payload
            };
        }

        var tcs = new TaskCompletionSource<SocketIOResponse>();

        client.EmitAsync(eventName, response =>
        {
            tcs.SetResult(response);
        }, data);

        return tcs.Task;
    }
}

public static class PhosSocketMessage
{
    public static readonly string GetNetworkState = "getNetworkState";
    public static readonly string DatabaseChange = "databaseChange";
    public static readonly string RegisterAsUser = "joinUserRoom";
}