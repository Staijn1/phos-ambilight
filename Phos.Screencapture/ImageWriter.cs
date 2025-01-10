using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Image = SixLabors.ImageSharp.Image;

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
    private readonly string imagesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "images");
    private readonly string gifsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "gifs");

    public ImageWriter()
    {
        if (!Directory.Exists(imagesDirectory))
        {
            Directory.CreateDirectory(imagesDirectory);
        }
        else
        {
            Console.WriteLine("Purging images directory");
            // Purge the directory
            foreach (var file in Directory.GetFiles(imagesDirectory))
            {
                File.Delete(file);
            }
            Console.WriteLine("Done!");
        }

        if (!Directory.Exists(gifsDirectory))
        {
            Directory.CreateDirectory(gifsDirectory);
        }
    }

    public async Task CreateImage(Dictionary<string, ScreenCapture.NET.ColorRGB> algorithmColorDict)
    {
        const int width = 800; // Default image width
        const int height = 800; // Fixed image height
        var rowHeight = height / algorithmColorDict.Count; // Calculate row height based on the number of algorithms

        using (var image = new Image<Rgba32>(width, height))
        {
            image.Mutate(ctx => ctx.Fill(Color.White));

            var yOffset = 0;

            foreach (var entry in algorithmColorDict)
            {
                var algorithmName = entry.Key;
                var colorRgb = entry.Value;

                // Convert ColorRGBA to ImageSharp Color
                var color = SixLabors.ImageSharp.Color.FromRgba(colorRgb.R, colorRgb.G, colorRgb.B, 255);


                image.Mutate(ctx => ctx.FillPolygon(color, new PointF(0, yOffset), new PointF(width, yOffset), new PointF(width, yOffset + rowHeight), new PointF(0, yOffset + rowHeight)));
                image.Mutate(x=> x.DrawText(algorithmName, new Font(SystemFonts.Get("Arial"), 16), Color.Red, new PointF(10, yOffset + (rowHeight / 2f) - 8)));

                yOffset += rowHeight;
            }

            // Add frame count in the top right corner
            image.Mutate(ctx => ctx.DrawText($"Frame: {frameCount}", new Font(SystemFonts.Get("Arial"), 16), Color.Blue, new PointF(width - 150, 10)));

            var filePath = Path.Combine(imagesDirectory, $"frame_{frameCount}.png");
            await image.SaveAsync(filePath);
            frameCount++;
        }
    }

    public void CreateGifFromImages(int frameDelay = 200)
    {
        Console.WriteLine("Creating gif");
        var imageFiles = Directory.EnumerateFiles(imagesDirectory, "*.png")
                                  .ToList();

        if (imageFiles.Count == 0)
        {
            Console.WriteLine("No PNG images found in the directory.");
            return;
        }

        using var gif = new Image<Rgba32>(1, 1); // Temporary dummy image
        int frameIndex = 0;
        foreach (var imageFile in imageFiles)
        {
            using var image = Image.Load<Rgba32>(imageFile);

            // Resize GIF canvas to match the first image dimensions
            if (gif.Width == 1 && gif.Height == 1)
            {
                gif.Mutate(ctx => ctx.Resize(image.Width, image.Height));
            }

            gif.Frames.AddFrame(image.Frames.RootFrame); // Add frame
            Console.WriteLine($"Processed frame {++frameIndex}/{imageFiles.Count}: {Path.GetFileName(imageFile)}");
        }

        gif.Frames.RemoveFrame(0); // Remove the initial dummy frame

        var gifEncoder = new GifEncoder
        {
            ColorTableMode = GifColorTableMode.Local,
            Quantizer = new WebSafePaletteQuantizer()
        };

        foreach (var frame in gif.Frames)
        {
            frame.Metadata.GetGifMetadata().FrameDelay = frameDelay / 10; // Frame delay in centiseconds
        }

        var gifPath = Path.Combine(gifsDirectory, "output.gif");
        gif.Save(gifPath, gifEncoder);

        Console.WriteLine($"GIF created successfully: {gifPath}");
    }


}
