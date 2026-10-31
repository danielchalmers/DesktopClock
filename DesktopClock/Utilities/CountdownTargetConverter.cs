using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace DesktopClock;

/// <summary>
/// Shows the countdown target in the regional format, and no target as an empty box instead of 1/1/0001. Clearing the box turns the countdown off.
/// </summary>
public class CountdownTargetConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is DateTime target && target != default ? target.ToString(culture) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;
        if (string.IsNullOrWhiteSpace(text))
            return default(DateTime);

        // Unparseable text leaves the target as it was and marks the box invalid.
        return DateTime.TryParse(text, culture, DateTimeStyles.AllowWhiteSpaces, out var target) ? target : DependencyProperty.UnsetValue;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
