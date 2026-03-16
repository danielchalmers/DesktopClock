using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace DesktopClock;

public class HeightScaleConverter : MarkupExtension, IValueConverter
{
    private const double StepSize = 0.15;

    public static readonly double MaxSizeLog = 6.5;

    public static readonly double MinSizeLog = 2.7;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var height = System.Convert.ToInt32(value, culture);
        return ToLogHeight(height);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var logHeight = System.Convert.ToDouble(value, culture);
        return FromLogHeight(logHeight);
    }

    public static int ScaleHeight(int height, double steps)
    {
        var newHeightLog = ToLogHeight(height) + (steps * StepSize);
        var clampedHeightLog = Math.Min(Math.Max(newHeightLog, MinSizeLog), MaxSizeLog);
        return FromLogHeight(clampedHeightLog);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    private static double ToLogHeight(int height) => Math.Log(height);

    private static int FromLogHeight(double logHeight) => (int)Math.Round(Math.Exp(logHeight));
}
