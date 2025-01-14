using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScreenCapture.NET;
using Color = System.Windows.Media.Color;

namespace Phos.Screencapture;

public class PhosScreenCapture
{
    private readonly DX11ScreenCaptureService _screenCaptureService = new DX11ScreenCaptureService();
    private readonly IEnumerable<GraphicsCard> _graphicsCards;
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

    public void SelectDisplay(Display? display)
    {
        if (!display.HasValue)
        {
            _screenCapture = null;
            return;
        }
        
        
        _screenCapture = _screenCaptureService.GetScreenCapture(display.Value);
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
        if (_screenCapture == null) throw new InvalidOperationException("No display selected");

        // Remove old capture zone
        if (_captureZone != null)
        {
            _screenCapture.UnregisterCaptureZone(_captureZone);
        }
        
        _captureZone = _screenCapture.RegisterCaptureZone(fromX, fromY, width, height);
    }


    public RefImage<ColorBGRA> GetImage()
    {
        if (_captureZone == null) throw new InvalidOperationException("No capture zone created");
        if (_screenCapture == null) throw new InvalidOperationException("No display selected");
        
        _screenCapture.CaptureScreen();
        
        using (_captureZone.Lock())
        {
            RefImage<ColorBGRA> image = _captureZone.Image;
            
            return image;
        }
    }

    public static ColorRGB GetAverageColorInArea(RefImage<ColorBGRA> image)
    {
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

        return new ColorRGB(avgR, avgG, avgB);
    }

    public Dictionary<string, ColorRGB> GetColorForEachAlgorithm()
    {
        Dictionary<string, ColorRGB> colors = new Dictionary<string, ColorRGB>();
        var image = GetImage();
        // Add colors computed by different algorithms
        colors.Add("Average Color", GetAverageColorInArea(image));
        // colors.Add("Weighted Average", GetWeightedAverageColor(image));
        colors.Add("Dominant Color", GetDominantColor(image)); // looks like this one is the best, tested with: https://www.youtube.com/watch?v=AFxSdaxO2Lw
        colors.Add("Spatial Average (Grid 10x10)", GetSpatialAverageColor(image, gridSize: 10));
        colors.Add("Histogram-Based Color", GetHistogramBasedColor(image));
        colors.Add("Brightness-Weighted Average", GetBrightnessWeightedAverageColor(image));
        colors.Add("Region-Specific Color (Top Half)", GetRegionSpecificColors(image, regions: 2).First());
        colors.Add("Edge-Weighted Average", GetEdgeWeightedAverageColor(image));

        return colors;
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
    
    #region Algorithms

    public static ColorRGB GetWeightedAverageColor(RefImage<ColorBGRA> image)
    {
        long totalR = 0, totalG = 0, totalB = 0;
        int pixelCount = image.Width * image.Height;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                ref readonly var pixel = ref image[x, y];
                totalR += (long)(pixel.R * 0.299);
                totalG += (long)(pixel.G * 0.587);
                totalB += (long)(pixel.B * 0.114);
            }
        }

        byte avgR = (byte)(totalR / pixelCount);
        byte avgG = (byte)(totalG / pixelCount);
        byte avgB = (byte)(totalB / pixelCount);

        return new ColorRGB(avgR, avgG, avgB);
    }

    public static ColorRGB GetSpatialAverageColor(RefImage<ColorBGRA> image, int gridSize)
    {
        long totalR = 0, totalG = 0, totalB = 0;
        int sampledPixels = 0;

        for (int y = 0; y < image.Height; y += gridSize)
        {
            for (int x = 0; x < image.Width; x += gridSize)
            {
                ref readonly var pixel = ref image[x, y];
                totalR += pixel.R;
                totalG += pixel.G;
                totalB += pixel.B;
                sampledPixels++;
            }
        }

        if (sampledPixels == 0)
        {
            Console.WriteLine("GetSpatialAverageColor defaulting to AverageColor");
            return GetAverageColorInArea(image);
        }
        byte avgR = (byte)(totalR / sampledPixels);
        byte avgG = (byte)(totalG / sampledPixels);
        byte avgB = (byte)(totalB / sampledPixels);

        return new ColorRGB(avgR, avgG, avgB);
    }

    public static ColorRGB GetHistogramBasedColor(RefImage<ColorBGRA> image)
    {
        var colorCounts = new Dictionary<ColorBGRA, int>();

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                ref readonly var pixel = ref image[x, y];
                if (colorCounts.ContainsKey(pixel))
                    colorCounts[pixel]++;
                else
                    colorCounts[pixel] = 1;
            }
        }

        var mostFrequentColor = colorCounts.OrderByDescending(c => c.Value).First().Key;
        return new ColorRGB(mostFrequentColor.R, mostFrequentColor.G, mostFrequentColor.B);
    }
    public static ColorRGB GetBrightnessWeightedAverageColor(RefImage<ColorBGRA> image)
    {
        long totalR = 0, totalG = 0, totalB = 0, totalWeight = 0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                ref readonly var pixel = ref image[x, y];
                int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                totalR += pixel.R * brightness;
                totalG += pixel.G * brightness;
                totalB += pixel.B * brightness;
                totalWeight += brightness;
            }
        }

        if (totalWeight == 0)
        {
            Console.WriteLine("BrightnessWeightedAverageColor defaulting to AverageColor");
            return GetAverageColorInArea(image);
        }
        
        byte avgR = (byte)(totalR / totalWeight);
        byte avgG = (byte)(totalG / totalWeight);
        byte avgB = (byte)(totalB / totalWeight);

        return new ColorRGB(avgR, avgG, avgB);
    }

    public static List<ColorRGB> GetRegionSpecificColors(RefImage<ColorBGRA> image, int regions)
    {
        int regionHeight = image.Height / regions;
        List<ColorRGB> colors = new List<ColorRGB>();

        for (int r = 0; r < regions; r++)
        {
            int startY = r * regionHeight;
            int endY = Math.Min(startY + regionHeight, image.Height);

            long totalR = 0, totalG = 0, totalB = 0;
            int pixelCount = 0;

            for (int y = startY; y < endY; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    ref readonly var pixel = ref image[x, y];
                    totalR += pixel.R;
                    totalG += pixel.G;
                    totalB += pixel.B;
                    pixelCount++;
                }
            }

            colors.Add(new ColorRGB((byte)(totalR / pixelCount), (byte)(totalG / pixelCount), (byte)(totalB / pixelCount)));
        }

        return colors;
    }

    
    public static ColorRGB GetEdgeWeightedAverageColor(RefImage<ColorBGRA> image)
    {
        long totalR = 0, totalG = 0, totalB = 0;
        var edgePixels = 0;

        for (var y = 1; y < image.Height - 1; y++)
        {
            for (var x = 1; x < image.Width - 1; x++)
            {
                ref readonly var pixel = ref image[x, y];

                var dx = Math.Abs(pixel.R - image[x + 1, y].R) +
                         Math.Abs(pixel.G - image[x + 1, y].G) +
                         Math.Abs(pixel.B - image[x + 1, y].B);

                var dy = Math.Abs(pixel.R - image[x, y + 1].R) +
                         Math.Abs(pixel.G - image[x, y + 1].G) +
                         Math.Abs(pixel.B - image[x, y + 1].B);

                if (dx + dy > 128) // Edge threshold
                {
                    totalR += pixel.R;
                    totalG += pixel.G;
                    totalB += pixel.B;
                    edgePixels++;
                }
            }
        }

        if (edgePixels == 0) // Avoid divide-by-zero
        {
            Console.WriteLine("GetEdgeWeightedAverageColor defaulting to AverageColor");
            return GetAverageColorInArea(image); // Fallback to average color
        }

        
        byte avgR = (byte)(totalR / edgePixels);
        byte avgG = (byte)(totalG / edgePixels);
        byte avgB = (byte)(totalB / edgePixels);

        return new ColorRGB(avgR, avgG, avgB);
    }

    public static ColorRGB GetDominantColor(RefImage<ColorBGRA> image)
    {
        var colorCounts = new Dictionary<ColorBGRA, int>(new ColorBgraEqualityComparer());

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                ref readonly var pixel = ref image[x, y];
                if (colorCounts.TryGetValue(pixel, out var count))
                {
                    colorCounts[pixel] = count + 1;
                }
                else
                {
                    colorCounts[pixel] = 1;
                }
            }
        }

        ColorBGRA mostFrequentColor = default;
        int maxCount = 0;

        foreach (var kvp in colorCounts)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                mostFrequentColor = kvp.Key;
            }
        }

        return new ColorRGB(mostFrequentColor.R, mostFrequentColor.G, mostFrequentColor.B);
    }

    public class ColorBgraEqualityComparer : IEqualityComparer<ColorBGRA>
    {
        public bool Equals(ColorBGRA x, ColorBGRA y)
        {
            return x.R == y.R && x.G == y.G && x.B == y.B && x.A == y.A;
        }

        public int GetHashCode(ColorBGRA obj)
        {
            return HashCode.Combine(obj.R, obj.G, obj.B, obj.A);
        }
    }
    
    #endregion
}