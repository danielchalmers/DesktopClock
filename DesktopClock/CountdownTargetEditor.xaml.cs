using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using DesktopClock.Properties;

namespace DesktopClock;

/// <summary>
/// Inline editor for the countdown target, built around one-click presets for
/// common scenarios and a plain-words preview of what the target means.
/// </summary>
public partial class CountdownTargetEditor : UserControl
{
    private static readonly (string Name, Func<DateTime> GetTarget)[] Presets =
    {
        (Loc.Get("TargetMidnight"), () => DateTime.Today.AddDays(1)),
        (Loc.Get("TargetTomorrowMorning"), () => DateTime.Today.AddDays(1).AddHours(9)),
        (Loc.Get("TargetFridayAfternoon"), () => NextOccurrence(DayOfWeek.Friday, 17)),
        (Loc.Get("TargetNewYear"), () => new DateTime(DateTime.Today.Year + 1, 1, 1)),
    };

    private bool _built;

    public CountdownTargetEditor()
    {
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
            var example = getTarget().ToString(FormatPresets.ShortDateTimePattern(CultureInfo.CurrentCulture));

            // Saved as a wall-clock time like a typed date, since a local one is saved with its UTC offset and would move after a time zone change.
            PresetButton.Add(PresetsPanel, name, example, () => Settings.Default.CountdownTo = DateTime.SpecifyKind(getTarget(), DateTimeKind.Unspecified));
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
            ? Loc.Get("NoCountdownTarget")
            : Loc.Format("CountdownPreview", CountdownTargetConverter.Format(target, "f", CultureInfo.CurrentCulture), RelativeTimeFormatter.Format(target, DateTime.Now));
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
