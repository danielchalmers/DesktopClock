using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopClock.Properties;

namespace DesktopClock;

public enum FormatEditorMode
{
    Clock,
    Countdown,
}

/// <summary>
/// Inline editor for the clock and countdown format strings, built around
/// one-click presets for common scenarios and insertable building blocks.
/// Changes bind straight to settings, so the clock updates as you edit.
/// </summary>
public partial class FormatEditor : UserControl
{
    // Presets cover the scenarios users most commonly ask for; the raw box stays the escape hatch.
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
        ("ISO week", "{weekYear}-W{week}"),
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
        ("Week number", "{week}"),
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

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(FormatEditorMode), typeof(FormatEditor), new PropertyMetadata(FormatEditorMode.Clock));

    private readonly DispatcherTimer _timer;
    private bool _built;

    public FormatEditor()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdatePreview();

        Loaded += FormatEditor_Loaded;
        Unloaded += (_, _) => _timer.Stop();
    }

    public FormatEditorMode Mode
    {
        get => (FormatEditorMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    private void FormatEditor_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_built)
        {
            _built = true;

            var settingsPath = Mode == FormatEditorMode.Clock ? nameof(Settings.Format) : nameof(Settings.CountdownFormat);
            FormatTextBox.SetBinding(TextBox.TextProperty, new Binding(settingsPath)
            {
                Source = Settings.Default,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });

            BuildPresetButtons();
            BuildTokenButtons();
        }

        UpdatePreview();
        _timer.Start();
    }

    private void BuildPresetButtons()
    {
        var presets = Mode == FormatEditorMode.Clock ? ClockPresets : CountdownPresets;

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
                MaxWidth = 280,
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 8, 4),
                ToolTip = string.IsNullOrEmpty(format) ? "(automatic)" : format,
            };
            button.Click += (_, _) => SetFormat(format);

            PresetsPanel.Children.Add(button);
        }
    }

    private void BuildTokenButtons()
    {
        var tokens = Mode == FormatEditorMode.Clock ? ClockTokens : CountdownTokens;

        foreach (var (name, token) in tokens)
        {
            var button = new Button
            {
                Content = name,
                FontSize = 12,
                Padding = new Thickness(7, 3, 7, 3),
                MinHeight = 24,
                Margin = new Thickness(0, 0, 6, 4),
                ToolTip = $"Insert {token} at the cursor",
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

        if (Mode == FormatEditorMode.Clock)
        {
            return TimeStringFormatter.Format(DateTimeOffset.Now, DateTime.Now, timeZone, default, format, string.Empty, culture);
        }

        // Preview against the real countdown target, or a sample one when none is set.
        var countdownTo = Settings.Default.CountdownTo != default ?
            Settings.Default.CountdownTo :
            DateTime.Now.AddDays(3).AddHours(4).AddMinutes(30).AddSeconds(10);

        return TimeStringFormatter.Format(DateTimeOffset.Now, DateTime.Now, timeZone, countdownTo, string.Empty, format, culture);
    }
}
