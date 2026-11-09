using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace DesktopClock;

/// <summary>
/// Shows a color as text, keeping the text as it was typed while it still means the same color.
/// </summary>
/// <remarks>
/// A box that sets the color as you type shows the color again after each key, and WPF writes it out in full, so typing #FF0000 would turn into #FFFFFF00 (yellow) at #FF0.
/// Each use in XAML gets its own instance, so it only remembers what was typed in its own box.
/// </remarks>
public class ColorTextConverter : MarkupExtension, IValueConverter
{
    private string _typedText;
    private Color _typedColor;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var color = (Color)value;
        return _typedText != null && color == _typedColor ? _typedText : color.ToString(culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value as string;

        try
        {
            _typedColor = (Color)ColorConverter.ConvertFromString(text);
            _typedText = text;
            return _typedColor;
        }
        catch (FormatException)
        {
            // Unreadable text, like a half-typed color, leaves the color as it was and marks the box invalid.
            return DependencyProperty.UnsetValue;
        }
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
