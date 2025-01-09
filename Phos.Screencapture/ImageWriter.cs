using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Phos.Screencapture;
using ScreenCapture.NET;

/// <summary>
/// Class that generates images for each captured frame using the Screen Capture feature
///
/// It generates a new image for each frame, e.g. 0.png, 1.png, 2.png, etc.
/// The images are saved in the current working directory/images
///
/// The images are used to compare color algorithms and to test the accuracy of the color detection
/// Each algorithm will get an equal size row in the image, where the whole detected color is used. The name of the algorithm will be displayed in the image for each row.
/// </summary>
public class ImageWriter
{
    private int frameCount = 0;
    private readonly string outputDirectory;

    public ImageWriter()
    {
        outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "images");
        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }
        else
        {
            Console.WriteLine("Purging images directory");
            // Purge the directory
            foreach (var file in Directory.GetFiles(outputDirectory))
            {
                File.Delete(file);
            }
            Console.WriteLine("Done!");
        }
    }

    public async Task CreateImage(Dictionary<string, ColorRGB> algorithmColorDict)
    {
        const int width = 800; // Default image width
        const int height = 800; // Fixed image height
        var rowHeight = height / algorithmColorDict.Count; // Calculate row height based on the number of algorithms

        using (var bitmap = new Bitmap(width, height))
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.White);

            var yOffset = 0;
            using (var font = new Font("Arial", 16))
            using (var brush = new SolidBrush(Color.Black))
            {
                foreach (var entry in algorithmColorDict)
                {
                    var algorithmName = entry.Key;
                    var colorHex = ColorUtils.ColorRGBToHex(entry.Value);

                    if (!ColorTranslator.FromHtml(colorHex).IsEmpty)
                    {
                        using (var rowBrush = new SolidBrush(ColorTranslator.FromHtml(colorHex)))
                        {
                            graphics.FillRectangle(rowBrush, 0, yOffset, width, rowHeight);
                        }
                    }

                    // Draw algorithm name on the row
                    graphics.DrawString(algorithmName, font, brush, new PointF(10, yOffset + (rowHeight / 2) - 8));

                    yOffset += rowHeight;
                }
            }

            var filePath = Path.Combine(outputDirectory, $"{frameCount}.png");
            bitmap.Save(filePath, ImageFormat.Png);
            frameCount++;
        }

        await Task.CompletedTask;
    }
}
