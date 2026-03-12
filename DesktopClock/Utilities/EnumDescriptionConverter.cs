using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;

namespace DesktopClock.Utilities;

public sealed class EnumDescriptionConverter : MarkupExtension, IValueConverter
{
    public override object ProvideValue(IServiceProvider serviceProvider) => this;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var type = value.GetType();
        if (!type.IsEnum)
        {
            return value.ToString() ?? string.Empty;
        }

        var field = type.GetField(value.ToString() ?? string.Empty, BindingFlags.Public | BindingFlags.Static);
        var description = field?
            .GetCustomAttributes(typeof(DescriptionAttribute), inherit: false)
            .Cast<DescriptionAttribute>()
            .FirstOrDefault()?
            .Description;

        return description ?? value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
