using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using DesktopClock.Properties;
using Humanizer;

namespace DesktopClock;

/// <summary>
/// Inline editor for the countdown target, built around one-click presets for
/// common scenarios and a plain-words preview of what the target means.
/// </summary>
public partial class CountdownTargetEditor : UserControl
{
    private static readonly (string Name, Func<DateTime> GetTarget)[] Presets =
    {
        ("Midnight tonight", () => DateTime.Today.AddDays(1)),
        ("Tomorrow morning", () => DateTime.Today.AddDays(1).AddHours(9)),
        ("Friday 5 PM", () => NextOccurrence(DayOfWeek.Friday, 17)),
        ("New Year's", () => new DateTime(DateTime.Today.Year + 1, 1, 1)),
    };

    private bool _built;

    public CountdownTargetEditor()
    {
        // WPF defaults every element's Language to en-US, so the target text box would parse and show dates only in US format. Follow the OS locale instead, matching the preset captions and preview below it. Set before InitializeComponent so the culture is already in place when the bindings are created.
        Language = XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag);

        InitializeComponent();

        Loaded += CountdownTargetEditor_Loaded;
        Unloaded += (_, _) => Settings.Default.PropertyChanged -= Settings_PropertyChanged;
    }

    private void CountdownTargetEditor_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_built)
        {
            _built = true;
            BuildPresetButtons();
        }

        Settings.Default.PropertyChanged -= Settings_PropertyChanged;
        Settings.Default.PropertyChanged += Settings_PropertyChanged;
        UpdatePreview();
    }

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Settings.CountdownTo))
        {
            Dispatcher.BeginInvoke(new Action(UpdatePreview));
        }
    }

    private void BuildPresetButtons()
    {
        foreach (var (name, getTarget) in Presets)
        {
            var title = new TextBlock
            {
                Text = name,
                FontWeight = FontWeights.SemiBold,
            };

            var example = new TextBlock
            {
                Text = getTarget().ToString("ddd, MMM d, h:mm tt"),
                FontSize = 11.5,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            example.SetResourceReference(ForegroundProperty, "TextSecondaryBrush");

            var content = new StackPanel();
            content.Children.Add(title);
            content.Children.Add(example);

            var button = new Button
            {
                Content = content,
                MaxWidth = 280,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 8, 4),
            };
            button.Click += (_, _) => Settings.Default.CountdownTo = getTarget();

            PresetsPanel.Children.Add(button);
        }
    }

    private void ResetTarget(object sender, RoutedEventArgs e)
    {
        Settings.Default.CountdownTo = default;
    }

    private void UpdatePreview()
    {
        var target = Settings.Default.CountdownTo;

        PreviewText.Text = target == default
            ? "No target — countdown mode is off and the clock shows the time."
            : $"{target:dddd, MMMM d, yyyy h:mm tt} — {target.Humanize(utcDate: false)}";
    }

    /// <summary>
    /// Returns the next future occurrence of the given weekday at the given hour.
    /// </summary>
    private static DateTime NextOccurrence(DayOfWeek day, int hour)
    {
        var candidate = DateTime.Today.AddHours(hour);

        while (candidate.DayOfWeek != day || candidate <= DateTime.Now)
        {
            candidate = candidate.AddDays(1);
        }

        return candidate;
    }
}
