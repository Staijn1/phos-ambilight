using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Phos.Connections;
using Phos.Data;
using Phos.Screencapture;
using ScreenCapture.NET;
using SocketIOClient;
using SocketIOClient.Transport;

namespace Phos.ScreenSync;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private bool _isScreenSelected = false;
    private bool _isCapturing = false;
    private Display _selectedDisplay;
    private PhosSocketIOClient _connection;
    private readonly PhosScreenCapture _screenCapture;
    private Task? screenCaptureThread;
    private List<Room> SelectedRooms { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public string CaptureButtonText => _isCapturing ? "Stop Capture" : "Start Capture";

    public bool CanStartCapture
    {
        get => _selectedDisplay != null && SelectedRooms != null && SelectedRooms.Count > 0;
    }


    public MainWindow(PhosScreenCapture screenCapture)
    {
        InitializeComponent();
        _screenCapture = screenCapture;

        ConnectWebSocket();
        LoadDisplays();

        DataContext = this; // Set the DataContext
    }

    private void ConnectWebSocket()
    {
        var deviceName = "Phos Screensync - " + Environment.MachineName;
        var url = "ws://api.phos.steinjonker.nl";
        _connection = new PhosSocketIOClient(url, new SocketIOOptions
        {
            Transport = TransportProtocol.WebSocket,
            Query = new List<KeyValuePair<string, string>>
            {
                new("deviceName", deviceName),
            }
        });

        _connection.OnConnect += async (sender, args) =>
        {
            // First register ourselves as a user
            await _connection.SendEvent(PhosSocketMessage.RegisterAsUser);
            var response = await _connection.SendEvent(PhosSocketMessage.GetNetworkState);

            var networkState = response.GetValue<NetworkState>();
            OnNewNetworkState(networkState);
        };
        _connection.OnDatabaseChange += (sender, response) => { Console.WriteLine("Database change event received"); };
    }

    /// <summary>
    /// Display the new network state on screen
    /// </summary>
    /// <param name="networkState"></param>
    private void OnNewNetworkState(NetworkState networkState)
    {
        Dispatcher.Invoke(() => { AvailableRoomsListBox.ItemsSource = networkState.Rooms; });
    }

    private void LoadDisplays()
    {
        AvailableDisplayListBox.ItemsSource = _screenCapture.GetDisplays();
    }

    public void DisplayListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedDisplay = (Display)AvailableDisplayListBox.SelectedItem;
        DisplayDetailsTextBlock.Text =
            $"Name: {_selectedDisplay.DeviceName}, Resolution: {_selectedDisplay.Width}x{_selectedDisplay.Height}";
        _screenCapture.SelectDisplay(_selectedDisplay);
    }

    public void StartStopCapture(object sender, RoutedEventArgs e)
    {
        if (_isCapturing)
        {
            // Stop capturing
            _isCapturing = false;
            // Stop capture thread
            screenCaptureThread?.Wait();
            Console.WriteLine("Stopped capturing.");
        }
        else
        {
            Console.WriteLine("Starting capture");
            // Start capturing
            _isCapturing = true;

            _screenCapture.CreateCaptureZone(0, 0, _selectedDisplay.Width, _selectedDisplay.Height);
            // Start the screen capture on a new thread
            screenCaptureThread = Task.Run(StartScreenCapture);
        }

        OnPropertyChanged(nameof(CaptureButtonText));
    }

    private async void StartScreenCapture()
    {
        var colors = new List<string> { "#ff0000", "#000000", "#000000" };
        // Set the mode for all rooms to the visualizer mode with FFT 255 (max)
        var newState = new State
        {
            Mode = 72,
            Colors = colors,
            Brightness = 255,
            Speed = 2000
        };
        await _connection.SendEvent(PhosSocketMessage.SetFFTValue, SelectedRooms.Select(r => r.Id).ToList(), 255);
        await _connection.SendEvent(PhosSocketMessage.SetNetworkState, SelectedRooms.Select(r => r.Id).ToList(),
            newState);


        while (_isCapturing)
        {
            Console.WriteLine("Capturing...");
            var averageColor = _screenCapture.GetAverageColorInArea();

            colors[0] = ColorUtils.ColorRGBToHex(averageColor);
            newState.Colors = colors;
            await _connection.SendEvent(PhosSocketMessage.SetNetworkStateRaw, SelectedRooms.Select(r => r.Id).ToList(),
                newState);

            // Update the Image control on the UI thread
            Dispatcher.Invoke(new Action(() => { ScreenImage.Source = _screenCapture.GetImageAsBitmap(); }));
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RoomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedRooms = AvailableRoomsListBox.SelectedItems.Cast<Room>().ToList();
        SelectedRooms = selectedRooms.ToList();
        OnPropertyChanged(nameof(CanStartCapture));
    }

    public void SelectArea(object sender, RoutedEventArgs e)
    {
        var overlayWindow = new OverlayWindow();
        overlayWindow.AreaSelected += (x, y, w, h) =>
        {
            _screenCapture.CreateCaptureZone(x, y, w, h);
            overlayWindow.Close();
        };
        overlayWindow.Topmost = true; // Ensure the overlay window is always on top
        overlayWindow.Show();
    }
}