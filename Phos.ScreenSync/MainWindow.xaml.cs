using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Phos.Connections;
using SocketIOClient;
using SocketIOClient.Transport;

namespace Phos.ScreenSync;
using Phos.Connections;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
using System;
using System.Drawing;
using System.Windows;

public partial class MainWindow : Window
{
    private SocketIOClient connection;
    private Rectangle screenArea;

    public MainWindow()
    {
        InitializeComponent();
        LoadConfiguration();
        ConnectWebSocket();
    }

    private void ConnectWebSocket()
    {
        var deviceName= "Phos Screensync - " + Environment.MachineName;
        var url = "ws://api.phos.steinjonker.nl";
        connection = new SocketIOClient(url, new SocketIOOptions
        {
            Transport = TransportProtocol.WebSocket,
            Query = new List<KeyValuePair<string, string>>
            {
                new("deviceName", deviceName),
            }
        });
    }

    private void LoadConfiguration()
    {
        // Load your configuration here
        // For example:
        screenArea = new Rectangle(0, 0, 100, 100);
    }

    /*private void AnalyzeScreenArea()
    {
        using (Bitmap bitmap = new Bitmap(screenArea.Width, screenArea.Height))
        {
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(screenArea.Location, Point.Empty, screenArea.Size);
            }

            long totalR = 0, totalG = 0, totalB = 0;
            int totalPixels = screenArea.Width * screenArea.Height;

            for (int y = 0; y < screenArea.Height; y++)
            {
                for (int x = 0; x < screenArea.Width; x++)
                {
                    Color pixel = bitmap.GetPixel(x, y);
                    totalR += pixel.R;
                    totalG += pixel.G;
                    totalB += pixel.B;
                }
            }

            Color averageColor = Color.FromArgb((int)(totalR / totalPixels), (int)(totalG / totalPixels), (int)(totalB / totalPixels));
            // Do something with averageColor
        }
    }*/
}