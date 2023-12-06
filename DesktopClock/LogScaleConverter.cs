using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace DesktopClock;

/// <summary>
/// https://stackoverflow.com/a/13850984.
/// </summary>
public class LogScaleConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double x = (int)value;
        return Math.Log(x);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var x = (double)value;
        return Math.Exp(x);
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
