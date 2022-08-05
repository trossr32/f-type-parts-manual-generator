using System.Drawing.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using Svg;

namespace App.Core.Services;

public static class ImageConverter
{
    /// <summary>
    /// Converts an image file to a base 64 data uri. <br />
    /// If width and/or height are provided the image is resized prior to conversion.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static string ToDataUri(this string file) =>
        Image
            .Load(file, out var format)
            .ToBase64String(format);

    public static string ToPngDataUri(this string file, int? width, int? height)
    {
        using var image = Image.Load(file, out var format);

        using var ms = new MemoryStream();
        using var copy = image.Clone(i => i.Opacity(1));

        copy.Save(ms, new PngEncoder());

        using var png = Image.Load(ms, out var pngFormat);

        return png.ToBase64String(pngFormat);
    }

    public static string SvgToBase64(this string file, int resize = 1000)
    {
        var svgDoc = SvgDocument.Open<SvgDocument>(file, null);

        using var ms = new MemoryStream();
        using var bitmap = svgDoc.Draw(resize, 0);

        bitmap.Save(ms, ImageFormat.Png);
        
        ms.Seek(0, SeekOrigin.Begin);

        using var image = Image.Load(ms, out var format); 
        
        return image.ToBase64String(format);
    }
}