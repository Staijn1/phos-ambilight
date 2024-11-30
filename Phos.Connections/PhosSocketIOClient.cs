using SocketIOClient;

namespace Phos.Connections;

public class PhosSocketIOClient
{
    protected readonly SocketIOClient.SocketIO client;
    public event EventHandler OnConnect;
    public event EventHandler OnDisconnect;
    public EventHandler<SocketIOResponse> OnDatabaseChange;
    public bool IsConnected => client.Connected;

    public PhosSocketIOClient(string serverUrl, SocketIOOptions options = null, bool autoConnect = true)
    {
        client = new SocketIOClient.SocketIO(serverUrl, options);

        if (autoConnect)
        {
            Connect();
        }

        client.OnConnected += OnConnected;
        client.OnDisconnected += (sender, args) => OnDisconnect?.Invoke(this, EventArgs.Empty);
        client.On(PhosSocketMessage.DatabaseChange, response => OnDatabaseChange?.Invoke(this, response));
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
        if (!client.Connected)
        {
            Console.WriteLine("Warning: attempting to send event while not connected to server!");
            return Task.FromResult<SocketIOResponse>(null);
        }
        
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

        client.EmitAsync(eventName, response => { tcs.SetResult(response); }, data);

        return tcs.Task;
    }
}

public static class PhosSocketMessage
{
    public static readonly string GetNetworkState = "getNetworkState";
    public static readonly string DatabaseChange = "databaseChange";
    public static readonly string RegisterAsUser = "joinUserRoom";
    public static readonly string SetNetworkState = "setState";
    public static readonly string SetNetworkStateRaw = "setNetworkStateRaw";
    public static readonly string SetFFTValue = "FFT";
}