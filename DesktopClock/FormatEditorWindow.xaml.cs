using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopClock.Properties;
using DesktopClock.Utilities;

namespace DesktopClock;

public enum FormatEditorMode
{
    Clock,
    Countdown,
}

/// <summary>
/// Prototype editor for the clock and countdown format strings, built around
/// one-click presets for common scenarios and insertable building blocks.
/// </summary>
public partial class FormatEditorWindow : Window
{
    // Presets cover the scenarios users most commonly ask for; the raw box below stays the escape hatch.
    private static readonly (string Name, string Format)[] ClockPresets =
    {
        ("Time", "{h:mm tt}"),
        ("Time, 24-hour", "{HH:mm}"),
        ("Time with seconds", "{h:mm:ss tt}"),
        ("Day and time", "{ddd}, {h:mm tt}"),
        ("Date and time", "{ddd}, {MMM dd}, {h:mm tt}"),
        ("Full date and time", "{dddd}, {MMMM dd}, {h:mm tt}"),
        ("Date only", "{dddd}, {MMMM dd}"),
        ("Sortable", "{yyyy-MM-dd} {HH:mm}"),
    };

    private static readonly (string Name, string Format)[] CountdownPresets =
    {
        ("Automatic", ""),
        ("Days left", "{%d} days left"),
        ("Days and hours", "{%d}d {%h}h"),
        ("Full breakdown", "{%d}d {%h}h {%m}m {%s}s"),
        ("Digital", "{dd}.{hh}:{mm}:{ss}"),
    };

    // Multi-character tokens are used where possible; single characters would be
    // interpreted as standard format strings, so day/hour counts use the % prefix.
    private static readonly (string Name, string Token)[] ClockTokens =
    {
        ("Weekday", "{ddd}"),
        ("Weekday (full)", "{dddd}"),
        ("Day", "{dd}"),
        ("Month", "{MMM}"),
        ("Month (full)", "{MMMM}"),
        ("Year", "{yyyy}"),
        ("Time", "{h:mm tt}"),
        ("Time (24-hour)", "{HH:mm}"),
        ("Seconds", "{ss}"),
        ("UTC offset", "{zzz}"),
    };

    private static readonly (string Name, string Token)[] CountdownTokens =
    {
        ("Days", "{%d}"),
        ("Hours", "{%h}"),
        ("Minutes", "{%m}"),
        ("Seconds", "{%s}"),
        ("Digital clock", "{hh}:{mm}:{ss}"),
    };

    private static readonly SolidColorBrush _errorBrush = new(Color.FromRgb(0xE8, 0x54, 0x54));

    private readonly FormatEditorMode _mode;
    private readonly DispatcherTimer _timer;

    public FormatEditorWindow(FormatEditorMode mode)
    {
        InitializeComponent();

        _mode = mode;
        Title = mode == FormatEditorMode.Clock ? "Clock format editor (preview)" : "Countdown format editor (preview)";
        FormatHintText.Text = mode == FormatEditorMode.Clock ?
            "Use curly braces {} around time codes." :
            "Use curly braces {} around time codes. Leave blank for a friendly automatic countdown.";

        BuildPresetButtons();
        BuildTokenButtons();

        FormatTextBox.Text = mode == FormatEditorMode.Clock ? Settings.Default.Format : Settings.Default.CountdownFormat;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdatePreview();
        _timer.Start();
        Closed += (_, _) => _timer.Stop();

        UpdatePreview();
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        ThemeManager.ApplyTitleBarTheme(this);
    }

    private void BuildPresetButtons()
    {
        var presets = _mode == FormatEditorMode.Clock ? ClockPresets : CountdownPresets;

        foreach (var (name, format) in presets)
        {
            var title = new TextBlock
            {
                Text = name,
                FontWeight = FontWeights.SemiBold,
            };

            var example = new TextBlock
            {
                Text = FormatPreview(format),
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
                MaxWidth = 260,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = string.IsNullOrEmpty(format) ? "(automatic)" : format,
            };
            button.Click += (_, _) => SetFormat(format);

            PresetsPanel.Children.Add(button);
        }
    }

    private void BuildTokenButtons()
    {
        var tokens = _mode == FormatEditorMode.Clock ? ClockTokens : CountdownTokens;

        foreach (var (name, token) in tokens)
        {
            var button = new Button
            {
                Content = name,
                Margin = new Thickness(0, 0, 8, 8),
                ToolTip = token,
            };
            button.Click += (_, _) => InsertToken(token);

            TokensPanel.Children.Add(button);
        }
    }

    private void SetFormat(string format)
    {
        FormatTextBox.Text = format;
        FormatTextBox.CaretIndex = format.Length;
        FormatTextBox.Focus();
    }

    private void InsertToken(string token)
    {
        var index = FormatTextBox.CaretIndex;
        FormatTextBox.Text = FormatTextBox.Text.Insert(index, token);
        FormatTextBox.CaretIndex = index + token.Length;
        FormatTextBox.Focus();
    }

    private void FormatTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var result = FormatPreview(FormatTextBox.Text);
        PreviewText.Text = result;

        if (result == Tokenizer.FormatErrorMessage)
        {
            PreviewText.Foreground = _errorBrush;
        }
        else
        {
            PreviewText.SetResourceReference(ForegroundProperty, "TextPrimaryBrush");
        }
    }

    /// <summary>
    /// Renders a format string the same way the clock itself would.
    /// </summary>
    private string FormatPreview(string format)
    {
        var timeZone = Settings.Default.TimeZoneInfo;
        var culture = CultureInfo.CurrentCulture;

        if (_mode == FormatEditorMode.Clock)
        {
            return TimeStringFormatter.Format(DateTimeOffset.Now, DateTime.Now, timeZone, default, format, string.Empty, culture);
        }

        // Preview against the real countdown target, or a sample one when none is set.
        var countdownTo = Settings.Default.CountdownTo != default ?
            Settings.Default.CountdownTo :
            DateTime.Now.AddDays(3).AddHours(4).AddMinutes(30).AddSeconds(10);

        return TimeStringFormatter.Format(DateTimeOffset.Now, DateTime.Now, timeZone, countdownTo, string.Empty, format, culture);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_mode == FormatEditorMode.Clock)
        {
            Settings.Default.Format = FormatTextBox.Text;
        }
        else
        {
            Settings.Default.CountdownFormat = FormatTextBox.Text;
        }

        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
