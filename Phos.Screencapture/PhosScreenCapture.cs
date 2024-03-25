using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCapture.NET;

namespace Phos.Screencapture;

public class PhosScreenCapture
{
    private DX11ScreenCaptureService _screenCaptureService = new DX11ScreenCaptureService();
    private IEnumerable<GraphicsCard> _graphicsCards;
    private DX11ScreenCapture? _screenCapture;
    private CaptureZone<ColorBGRA>? _captureZone;

    public PhosScreenCapture()
    {
        _graphicsCards = _screenCaptureService.GetGraphicsCards();
    }

    /// <summary>
    /// Get connected displays to a graphics card
    /// Todo support multiple graphics cards???
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Display> GetDisplays()
    {
        return _screenCaptureService.GetDisplays(_graphicsCards.First());
    }

    public void SelectDisplay(Display display)
    {
        _screenCapture = _screenCaptureService.GetScreenCapture(display);
    }

    /// <summary>
    /// Capture part of a display, a rectangle area. Start from (fromX, fromY) with width and height
    /// </summary>
    /// <param name="fromX"></param>
    /// <param name="fromY"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void CreateCaptureZone(int fromX, int fromY, int width, int height)
    {
        if (_screenCapture == null) throw new Exception("No display selected");

        _captureZone = _screenCapture.RegisterCaptureZone(fromX, fromY, width, height);
    }


    public RefImage<ColorBGRA> GetImage()
    {
        if (_captureZone == null) throw new Exception("No capture zone created");
        if (_screenCapture == null) throw new Exception("No display selected");
        
        _screenCapture.CaptureScreen();
        
        using (_captureZone.Lock())
        {
            RefImage<ColorBGRA> image = _captureZone.Image;
            
            return image;
        }
    }

    public ColorBGRA GetAverageColorInArea()
    {
        var image = GetImage();
        long totalR = 0, totalG = 0, totalB = 0, totalA = 0;
        int pixelCount = image.Width * image.Height;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                ColorBGRA pixel = image[x, y];
                totalR += pixel.R;
                totalG += pixel.G;
                totalB += pixel.B;
            }
        }

        byte avgR = (byte)(totalR / pixelCount);
        byte avgG = (byte)(totalG / pixelCount);
        byte avgB = (byte)(totalB / pixelCount);
        byte avgA = (byte)(totalA / pixelCount);

        return new ColorBGRA(avgB, avgG, avgR, avgA);
    }

    public BitmapSource GetImageAsBitmap()
    {
        var rawData = this.GetRawData();
        return BitmapSource.Create(
            _captureZone.Width,
            _captureZone.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            rawData.ToArray(),
            _captureZone.Stride
        );
    }

    private ReadOnlySpan<byte> GetRawData()
    {
        if (_captureZone == null) throw new Exception("No capture zone created");
        if (_screenCapture == null) throw new Exception("No display selected");

        _screenCapture.CaptureScreen();

        using (_captureZone.Lock())
        {
            ReadOnlySpan<byte> rawData = _captureZone.RawBuffer;
            return rawData;
        }
    }
}