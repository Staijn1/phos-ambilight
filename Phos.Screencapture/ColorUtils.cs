using ScreenCapture.NET;

namespace Phos.Screencapture;

public static class ColorUtils
{
    public static string ColorRGBAToHex(ColorBGRA color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}{color.A:X2}";
    }
}