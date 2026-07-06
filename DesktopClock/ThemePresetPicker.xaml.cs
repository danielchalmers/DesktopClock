using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DesktopClock.Properties;

namespace DesktopClock;

/// <summary>
/// A row of one-click clock looks, each chip rendering a true miniature of the
/// clock — same fonts, colors, and shape the real window would use.
/// </summary>
public partial class ThemePresetPicker : UserControl
{
    private static readonly FontWeightConverter _fontWeightConverter = new();
    private static readonly FontStyleConverter _fontStyleConverter = new();

    public ThemePresetPicker()
    {
        InitializeComponent();

        foreach (var theme in ClockTheme.GetBuiltInThemes())
        {
            PresetsPanel.Children.Add(CreatePresetButton(theme));
        }
    }

    private Button CreatePresetButton(ClockTheme theme)
    {
        var name = new TextBlock
        {
            Text = theme.Name,
            FontSize = 11.5,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
        };
        name.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");

        var content = new StackPanel();
        content.Children.Add(CreateMiniClock(theme));
        content.Children.Add(name);

        var button = new Button
        {
            Content = content,
            Padding = new Thickness(5),
            Margin = new Thickness(0, 0, 8, 8),
            ToolTip = "Sets the font, colors, opacity, and corners. Fine-tune in Typography and Appearance.",
        };
        button.Click += (_, _) => theme.Apply(Settings.Default);

        return button;
    }

    /// <summary>
    /// Renders the theme the same way the clock window would, at chip scale.
    /// </summary>
    private static FrameworkElement CreateMiniClock(ClockTheme theme)
    {
        var text = new OutlinedTextBlock
        {
            Text = "10:08",
            FontSize = 17,
            FontFamily = new FontFamily(theme.FontFamily),
            FontWeight = (FontWeight)_fontWeightConverter.ConvertFromString(theme.FontWeight),
            FontStyle = (FontStyle)_fontStyleConverter.ConvertFromString(theme.FontStyle),
            Fill = new SolidColorBrush(theme.TextColor),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var clock = new Border
        {
            Child = text,
            Padding = new Thickness(10, 4, 10, 4),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        if (theme.BackgroundEnabled)
        {
            // Solid background, scaled down like the clock itself would be in a small window.
            clock.Background = new SolidColorBrush(theme.OuterColor) { Opacity = theme.BackgroundOpacity };
            clock.CornerRadius = new CornerRadius(theme.BackgroundCornerRadius * 0.75);
        }
        else if (theme.OutlineThickness > 0)
        {
            text.Stroke = new SolidColorBrush(theme.OuterColor) { Opacity = theme.BackgroundOpacity };
            text.StrokeThickness = theme.OutlineThickness;
        }

        // Neutral backdrop standing in for the desktop behind the clock.
        var backdrop = new Border
        {
            Child = clock,
            Width = 128,
            Height = 58,
            CornerRadius = new CornerRadius(5),
            SnapsToDevicePixels = true,
        };
        backdrop.SetResourceReference(Border.BackgroundProperty, "SubtleBrush");

        return backdrop;
    }
}
