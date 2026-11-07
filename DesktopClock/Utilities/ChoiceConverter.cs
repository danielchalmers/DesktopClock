using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace DesktopClock;

/// <summary>
/// Checks a menu item when the bound value is the item's choice, given as the converter parameter, and picks that choice when the item is clicked.
/// </summary>
public class ChoiceConverter : MarkupExtension, IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => Equals(value, parameter);

    // Clicking the choice that's already picked unchecks the item, so pick it either way and the binding checks it again.
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => parameter;

    public override object ProvideValue(IServiceProvider serviceProvider) => this;
}
