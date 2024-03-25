using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Phos.Connections;
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

    public event PropertyChangedEventHandler? PropertyChanged;
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
    }

    private void LoadDisplays()
    {
        DisplayListBox.ItemsSource = _screenCapture.GetDisplays();
    }

    public void DisplayListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedDisplay = (Display)DisplayListBox.SelectedItem;
        IsScreenSelected = true;
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
            Console.WriteLine("Stopped capturing.");
        }
        else
        {
            Console.WriteLine("Starting capture");
            // Start capturing
            _isCapturing = true;

            _screenCapture.CreateCaptureZone(0, 0, _selectedDisplay.Width, _selectedDisplay.Height);
            // Start the screen capture on a new thread
            Task.Run(StartScreenCapture);
        }

        OnPropertyChanged(nameof(CaptureButtonText));
    }

    private async void StartScreenCapture()
    {
        while (_isCapturing)
        {
            Console.WriteLine("Capturing...");
            var averageColor = _screenCapture.GetAverageColorInArea();
            
            // Log the average color hex
            Console.WriteLine(ColorUtils.ColorRGBAToHex(averageColor));

            // Update the Background property of the window on the UI thread
            Dispatcher.Invoke(new Action(() =>
            {
                // Convert the average color to a SolidColorBrush
                var averageColorBrush = new SolidColorBrush(Color.FromRgb(averageColor.R, averageColor.G, averageColor.B));

                Background = averageColorBrush;
            }));

            // Introduce a delay
            await Task.Delay(100); // 100 milliseconds delay
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}