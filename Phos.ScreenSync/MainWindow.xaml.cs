using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Phos.Connections;
using Phos.Screencapture;
using ScreenCapture.NET;
using SocketIOClient;
using SocketIOClient.Transport;


namespace Phos.ScreenSync;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>

public partial class MainWindow : Window
{
    private bool _isScreenSelected = false;
    private bool _isCapturing = false;
    private Display _selectedDisplay;
    private PhosSocketIOClient _connection;
    private readonly PhosScreenCapture _screenCapture;
    
    public event PropertyChangedEventHandler PropertyChanged;
    public string CaptureButtonText => _isCapturing ? "Stop Capture" : "Start Capture";

    
    public bool IsScreenSelected
    {
        get => _isScreenSelected;
        set
        {
            _isScreenSelected = value;
            OnPropertyChanged();
        }
    }

    
    public MainWindow(PhosScreenCapture screenCapture)
    {
        InitializeComponent();
        _screenCapture = screenCapture;

        ConnectWebSocket();
        LoadDisplays();
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
    }
    
    private void LoadDisplays()
    {
        DisplayListBox.ItemsSource = _screenCapture.GetDisplays();
    }

    public void DisplayListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedDisplay = (Display)DisplayListBox.SelectedItem;
        IsScreenSelected = _selectedDisplay != null;
        DisplayDetailsTextBlock.Text = $"Name: {_selectedDisplay.DeviceName}, Resolution: {_selectedDisplay.Width}x{_selectedDisplay.Height}";
    }

    public void StartStopCapture(object sender, RoutedEventArgs e)
    {
        if (_isCapturing)
        {
            // Stop capturing
            _isCapturing = false;
        }
        else
        {
            // Start capturing
            _isCapturing = true;
        }

        OnPropertyChanged(nameof(CaptureButtonText));
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}