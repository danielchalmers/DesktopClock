using System.Windows;

namespace DesktopClock;

/// <summary>
/// Marks a checkable menu item as one choice of several, so the menu shows a dot instead of a checkmark next to the picked one.
/// </summary>
public static class RadioMenuItem
{
    public static readonly DependencyProperty IsRadioProperty =
        DependencyProperty.RegisterAttached("IsRadio", typeof(bool), typeof(RadioMenuItem), new PropertyMetadata(false));

    public static bool GetIsRadio(DependencyObject element) => (bool)element.GetValue(IsRadioProperty);

    public static void SetIsRadio(DependencyObject element, bool value) => element.SetValue(IsRadioProperty, value);
}
