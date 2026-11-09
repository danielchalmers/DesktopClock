using System;
using System.Globalization;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopClock.Tests;

public class BackgroundBrushConverterTests
{
    [Theory]
    [InlineData("{0}")]
    [InlineData("\"{0}\"")] // Copy as path in Explorer adds quotes.
    [InlineData(" \"{0}\" ")]
    public void Convert_ShowsTheImage(string pathFormat)
    {
        var image = Path.Combine(Path.GetTempPath(), $"DesktopClock background {Guid.NewGuid():N}.png");
        try
        {
            using (var file = File.Create(image))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2, 2, 96, 96, PixelFormats.Bgra32, null, new byte[16], 8)));
                encoder.Save(file);
            }

            var brush = Convert(string.Format(pathFormat, image));

            Assert.IsType<ImageBrush>(brush);
        }
        finally
        {
            File.Delete(image);
        }
    }

    [Fact]
    public void Convert_FallsBackToTheColorWhenTheImageIsMissing()
    {
        var brush = Convert(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png"));

        Assert.Equal(Colors.Red, Assert.IsType<SolidColorBrush>(brush).Color);
    }

    private static object Convert(string path) =>
        new BackgroundBrushConverter().Convert([true, path, Colors.Red, 1d, Stretch.Fill], typeof(Brush), null, CultureInfo.InvariantCulture);
}
