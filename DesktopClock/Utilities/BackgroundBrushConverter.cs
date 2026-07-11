using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DesktopClock;

/// <summary>
/// Builds the clock's background brush from the current settings, falling back to the solid outer color when a chosen background image is missing or can't be read.
/// </summary>
/// <remarks>
/// Bound values, in order: BackgroundEnabled, BackgroundImagePath, OuterColor, BackgroundOpacity, BackgroundImageStretch.
/// </remarks>
public class BackgroundBrushConverter : MarkupExtension, IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        var enabled = values.Length > 0 && values[0] is bool b && b;

        // Background off means outlined text on a transparent window.
        if (!enabled)
            return Brushes.Transparent;

        var path = values.Length > 1 ? values[1] as string : null;
        var outerColor = values.Length > 2 && values[2] is Color color ? color : Colors.Transparent;
        var opacity = values.Length > 3 && values[3] is double o ? o : 1d;
        var stretch = values.Length > 4 && values[4] is Stretch s ? s : Stretch.Fill;

        // Use the image when it loads; otherwise fall back to the solid color so the clock never ends up with an invisible background.
        var image = TryLoadImage(path);
        if (image != null)
            return new ImageBrush(image) { Opacity = opacity, Stretch = stretch };

        return new SolidColorBrush(outerColor) { Opacity = opacity };
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    private static ImageSource TryLoadImage(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Decode now so a missing or invalid file fails here, and don't hold the file locked.
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            // Missing file, non-image content, unavailable share, or a malformed path all fall back to the solid color.
            return null;
        }
    }
}
