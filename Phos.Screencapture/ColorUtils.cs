using ScreenCapture.NET;

namespace Phos.Screencapture;

public static class ColorUtils
{
    public static string ColorRGBToHex(ColorRGB color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}