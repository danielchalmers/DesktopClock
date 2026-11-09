using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace DesktopClock;

/// <summary>
/// Builds the one-click preset buttons in the editors: a bold name over a smaller example of what it gives.
/// </summary>
public static class PresetButton
{
    /// <summary>
    /// Adds a preset button to the panel and returns its example text, so it can be updated later.
    /// </summary>
    public static TextBlock Add(Panel panel, string name, string example, Action onClick, string toolTip = null)
    {
        var title = new TextBlock
        {
            Text = name,
            FontWeight = FontWeights.SemiBold,
        };

        var exampleText = new TextBlock
        {
            Text = example,
            FontSize = 12,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };
        exampleText.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");

        var content = new StackPanel();
        content.Children.Add(title);
        content.Children.Add(exampleText);

        var button = new Button
        {
            Content = content,
            MaxWidth = 280,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 8, 4),
            ToolTip = toolTip,
        };
        AutomationProperties.SetName(button, name);
        button.Click += (_, _) => onClick();

        panel.Children.Add(button);
        return exampleText;
    }
}
