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
        (Loc.Get("ClockPresetTime"), "{h:mm tt}"),
        (Loc.Get("ClockPresetTime24"), "{HH:mm}"),
        (Loc.Get("ClockPresetTimeSeconds"), "{h:mm:ss tt}"),
        (Loc.Get("ClockPresetDayTime"), "{ddd}, {h:mm tt}"),
        (Loc.Get("ClockPresetDateTime"), "{ddd}, {MMM dd}, {h:mm tt}"),
        (Loc.Get("ClockPresetFullDateTime"), "{dddd}, {MMMM dd}, {h:mm tt}"),
        (Loc.Get("ClockPresetDateOnly"), "{dddd}, {MMMM dd}"),
        (Loc.Get("ClockPresetSortable"), "{yyyy-MM-dd} {HH:mm}"),
        (Loc.Get("ClockPresetIsoWeek"), "{weekYear}-W{week}"),
    };

    private static readonly (string Name, string Format)[] CountdownPresets =
    {
        (Loc.Get("CountdownPresetAutomatic"), ""),
        (Loc.Get("CountdownPresetDaysLeft"), Loc.Get("CountdownFormatDaysLeft")),
        (Loc.Get("CountdownPresetDaysHours"), Loc.Get("CountdownFormatDaysHours")),
        (Loc.Get("CountdownPresetFull"), Loc.Get("CountdownFormatFull")),
        (Loc.Get("CountdownPresetDigital"), "{dd}.{hh}:{mm}:{ss}"),
    };

    // Multi-character tokens are used where possible; single characters would be
    // interpreted as standard format strings, so day/hour counts use the % prefix.
    private static readonly (string Name, string Token)[] ClockTokens =
    {
        (Loc.Get("TokenWeekday"), "{ddd}"),
        (Loc.Get("TokenWeekdayFull"), "{dddd}"),
        (Loc.Get("TokenDay"), "{dd}"),
        (Loc.Get("TokenMonth"), "{MMM}"),
        (Loc.Get("TokenMonthFull"), "{MMMM}"),
        (Loc.Get("TokenYear"), "{yyyy}"),
        (Loc.Get("TokenTime"), "{h:mm tt}"),
        (Loc.Get("TokenTime24"), "{HH:mm}"),
        (Loc.Get("TokenSeconds"), "{ss}"),
        (Loc.Get("TokenWeekNumber"), "{week}"),
        (Loc.Get("TokenUtcOffset"), "{zzz}"),
    };

    private static readonly (string Name, string Token)[] CountdownTokens =
    {
        (Loc.Get("TokenDays"), "{%d}"),
        (Loc.Get("TokenHours"), "{%h}"),
        (Loc.Get("TokenMinutes"), "{%m}"),
        (Loc.Get("TokenSeconds"), "{%s}"),
        (Loc.Get("TokenDigitalClock"), "{hh}:{mm}:{ss}"),
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
                FontSize = 12,
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
                ToolTip = string.IsNullOrEmpty(format) ? Loc.Get("AutomaticTooltip") : format,
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
                ToolTip = Loc.Format("InsertTokenTooltip", token),
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
