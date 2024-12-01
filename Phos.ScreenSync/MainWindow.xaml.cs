using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Phos.Connections;
using Phos.Connections.AssettoCorsa.SharedMemory;
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
    private bool HasCustomAreaSelected { get; set; } = false;
    private bool _isScreenSelected = false;
    private bool _isCapturing = false;
    private AcSharedMemory _assettoCorsaSharedMemory;
    private bool _isAssettoCorsaIntegrationRunning = false;
    private Display _selectedDisplay;
    private PhosSocketIOClient? _connection;
    private readonly PhosScreenCapture _screenCapture;
    private Task? screenCaptureThread;
    private readonly SettingsManager<UserSettings> _settingsManager;

    private List<Room> ScreenSyncSelectedRooms { get; set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;
    public string CaptureButtonText => _isCapturing ? "Stop Capture" : "Start Capture";

    public bool CanStartCapture
    {
        get => _selectedDisplay != null && ScreenSyncSelectedRooms?.Count > 0;
    }


    public MainWindow(PhosScreenCapture screenCapture)
    {
        InitializeComponent();
        _screenCapture = screenCapture;

        _settingsManager = new SettingsManager<UserSettings>("userSettings.json");

        // Load the WebSocket URL from the settings
        var settings = _settingsManager.LoadSettings();
        WebSocketInput.Text = settings?.WebSocketUrl ?? string.Empty;

        LoadDisplays();

        DataContext = this;
        
        _assettoCorsaSharedMemory = new AcSharedMemory();
        _assettoCorsaSharedMemory.GraphicsInterval = 100; // 100ms
        _assettoCorsaSharedMemory.GraphicsUpdated += OnGraphicsUpdated;
        
        _assettoCorsaSharedMemory.PhysicsInterval = 1; // 50ms
        _assettoCorsaSharedMemory.PhysicsUpdated += OnPhysicsUpdated;
        
        _assettoCorsaSharedMemory.StaticInfoInterval = 2500; // 2.5s
        _assettoCorsaSharedMemory.StaticInfoUpdated += OnStaticInfoUpdated;
    }

    private void ConnectWebSocket(object o, RoutedEventArgs routedEventArgs)
    {
        var deviceName = "Phos Screensync - " + Environment.MachineName;
        var url = WebSocketInput.Text;
        // Save the WebSocket URL to the settings
        var settings = new UserSettings { WebSocketUrl = url };
        _settingsManager.SaveSettings(settings);

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
        _connection.OnDisconnect += (sender, args) =>
        {
            Console.WriteLine("Disconnected from server");
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
        Dispatcher.Invoke(() => { AcAvailableRoomsListBox.ItemsSource = networkState.Rooms; });
    }

    private void LoadDisplays()
    {
        AvailableDisplayListBox.ItemsSource = _screenCapture.GetDisplays();
    }

    public void ScreenSyncDisplayListBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedDisplay = (Display)AvailableDisplayListBox.SelectedItem;
        _screenCapture.SelectDisplay(_selectedDisplay);
    }

    public void ToggleScreenCapture(object sender, RoutedEventArgs e)
    {
        if (_isCapturing)
        {
            // Stop capturing
            _isCapturing = false;
            // Stop capture thread
            screenCaptureThread?.Wait();
            screenCaptureThread = null;
            Console.WriteLine("Stopped capturing.");
        }
        else
        {
            Console.WriteLine("Starting capture");
            // Start capturing
            _isCapturing = true;

            if (!HasCustomAreaSelected)
            {
                _screenCapture.CreateCaptureZone(0, 0, _selectedDisplay.Width, _selectedDisplay.Height);
            }

            // Start the screen capture on a new thread
            screenCaptureThread = Task.Run(StartScreenCapture);
        }

        OnPropertyChanged(nameof(CaptureButtonText));
    }


    private async void StartScreenCapture()
    {
        var newState = await PrepareSelectedRoomsForScreenSync();


        while (_isCapturing && _connection != null && _connection.IsConnected)
        {
            Console.WriteLine("Capturing...");
            var averageColor = _screenCapture.GetAverageColorInArea();
            var colors = newState.Colors;
            colors[0] = ColorUtils.ColorRGBToHex(averageColor);
            newState.Colors = colors;
            await _connection.SendEvent(PhosSocketMessage.SetNetworkState, ScreenSyncSelectedRooms.Select(r => r.Id).ToList(), newState);

            // Update the Image control on the UI thread
            Dispatcher.Invoke(new Action(() => { ScreenImage.Source = _screenCapture.GetImageAsBitmap(); }));
        }
    }

    private async Task<State> PrepareSelectedRoomsForScreenSync()
    {
        var colors = new List<string> { "#000000", "#000000", "#000000" };
        // Set the mode for all rooms to the visualizer mode with FFT 255 (max)
        var newState = new State
        {
            Mode = 72,
            Colors = colors,
            Brightness = 255,
            Speed = 2000
        };
        await _connection.SendEvent(PhosSocketMessage.SetFFTValue, ScreenSyncSelectedRooms.Select(r => r.Id).ToList(), 255);
        await _connection.SendEvent(PhosSocketMessage.SetNetworkState, ScreenSyncSelectedRooms.Select(r => r.Id).ToList(), newState);
        return newState;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void ScreenSyncRoomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedRooms = AvailableRoomsListBox.SelectedItems.Cast<Room>().ToList();
        ScreenSyncSelectedRooms = selectedRooms.ToList();
        OnPropertyChanged(nameof(CanStartCapture));
    }

    public void SelectArea(object sender, RoutedEventArgs e)
    {
        var overlayWindow = new OverlayWindow();
        overlayWindow.AreaSelected += (x, y, w, h) =>
        {
            _screenCapture.CreateCaptureZone(x, y, w, h);
            overlayWindow.Close();
            HasCustomAreaSelected = true;
        };
        overlayWindow.Show();
    }

    #region Assetto Corsa Integration
    private AC_FLAG_TYPE _lastFlag = AC_FLAG_TYPE.AC_NO_FLAG;
    private int _maxRpm = 0;
    private int _lastMappedRpm = 0;
    private float _lastActualRpm = 0;
    public IEnumerable<Room> AcSelectedRooms { get; set; } = [];

    private void ToggleAssettoCorsaIntegration(object sender, RoutedEventArgs e)
    {
        if (_isAssettoCorsaIntegrationRunning)
        {
            // Stop the integration
            _assettoCorsaSharedMemory.Stop();
            StartStopIntegrationButton.Content = "Start Integration";
            _isAssettoCorsaIntegrationRunning = false;
        }
        else
        {
            // Start the integration
            _assettoCorsaSharedMemory.Start();
            _connection?.SendEvent(PhosSocketMessage.SetNetworkState, AcSelectedRooms.Select(r => r.Id).ToList(), new State
            {
                Mode = 72,
                Colors = new List<string> { "#0000FF", "#000000", "#000000" },
                Brightness = 255,
                Speed = 2000
            });
            StartStopIntegrationButton.Content = "Stop Integration";
            _isAssettoCorsaIntegrationRunning = true;
        }
    }

    /**
     * When a flag is being shown in Assetto Corsa, then reflect that by displaying it on the ledstrip in the selected rooms.
     * When the flag changes back to no flag, then set the ledstrip to the visualizer mode to prepare for RPM display.
     */
    private void OnGraphicsUpdated(object sender, GraphicsEventArgs e)
    {
        if (_connection is not { IsConnected: true })
        {
            return;
        }
        
        
        // Set the color of the flag to the selected room
        if (e.Graphics.Flag != AC_FLAG_TYPE.AC_NO_FLAG && e.Graphics.Flag != _lastFlag)
        {
            var newState = new State
            {
                Mode = 1,
                Colors = new List<string> { "#000000", "#000000", "#000000" },
                Brightness = 255,
                Speed = 1000
            };
            
            switch (e.Graphics.Flag)
            {
                case AC_FLAG_TYPE.AC_BLUE_FLAG:
                    newState.Colors[0] = "#0000FF";
                    break;
                case AC_FLAG_TYPE.AC_WHITE_FLAG:
                    newState.Colors[0] = "#FFFFFF";
                    break;
                case AC_FLAG_TYPE.AC_YELLOW_FLAG:
                    newState.Colors[0] = "#FFFF00";
                    break;
                case AC_FLAG_TYPE.AC_PENALTY_FLAG:
                    newState.Colors[0] = "#FF0000";
                    break;
            }

            _connection.SendEvent(PhosSocketMessage.SetNetworkState, AcSelectedRooms.Select(r => r.Id).ToList(), newState);
        } 
        // When we change from a flag to no flag, then set the mode to the visualizer mode
        else if (_lastFlag != AC_FLAG_TYPE.AC_NO_FLAG && e.Graphics.Flag == AC_FLAG_TYPE.AC_NO_FLAG)
        {
            var newState = new State
            {
                Mode = 72,
                Colors = new List<string> { "#0000FF", "#000000", "#000000" },
                Brightness = 255,
                Speed = 2000
            };
            _connection.SendEvent(PhosSocketMessage.SetNetworkState, AcSelectedRooms.Select(r => r.Id).ToList(), newState);
        }

        _lastFlag = e.Graphics.Flag;
    }
    
    /**
     * Displays the RPM of the car on the ledstrip in the selected rooms.
     * Maps the RPM (range: > 90 % max RPM (actual) - maxRPM received from StaticInfo), to 0 - 255 for the ledstrip.
     */
    private void OnPhysicsUpdated(object sender, PhysicsEventArgs e)
    {
        // Check if the connection is valid and connected
        if (_connection is not { IsConnected: true })
        {
            return;
        }

        // Get the actual RPM value from the physics event arguments
        var actualRpm = e.Physics.Rpms;

        // Ensure max RPM is defined and not zero to avoid division by zero
        if (_maxRpm <= 0)
        {
            return;
        }

        // Calculate the minimum RPM threshold (90% of the max RPM)
        var minRpmThreshold = 0.75f * _maxRpm;

        // If the RPM is below 90% of max RPM and it was below the threshold last time, exit early
        if (actualRpm < minRpmThreshold && _lastActualRpm < minRpmThreshold)
        {
            return;
        }

        // If the RPM is outside the threshold of max RPM, set the LED strip value to 0 (off)
        if (actualRpm < minRpmThreshold)
        {
            _connection.SendEvent(PhosSocketMessage.SetFFTValue, AcSelectedRooms.Select(r => r.Id).ToList(), 0);
            _lastActualRpm = actualRpm;
            return;
        }

        // Map the RPM value in the range of 90% max RPM to max RPM to a value between 0 and 255 for the LED strip
        var fftValue = (int)Math.Round(((actualRpm - minRpmThreshold) / (float)(_maxRpm - minRpmThreshold)) * 255);

        // Check if the mapped RPM value is the same as the last one
        if (fftValue == _lastMappedRpm)
        {
            _lastActualRpm = actualRpm;
            return;
        }

        // Update the last mapped RPM value and actual RPM value
        _lastMappedRpm = fftValue;
        _lastActualRpm = actualRpm;

        // Send the calculated value to the LED strips in the selected rooms
        _connection.SendEvent(PhosSocketMessage.SetFFTValue, AcSelectedRooms.Select(r => r.Id).ToList(), fftValue);
    }


    /**
     * Stores the max rpm of this car for later use in the physics update.
     */
    private void OnStaticInfoUpdated(object sender, StaticInfoEventArgs e)
    {
        _maxRpm = e.StaticInfo.MaxRpm;
    }
    
    private void ACRoomListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        AcSelectedRooms = AcAvailableRoomsListBox.SelectedItems.Cast<Room>();
    }


    #endregion
}