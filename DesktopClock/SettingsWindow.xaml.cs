using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using DesktopClock.Properties;
using Microsoft.Win32;

namespace DesktopClock;

public partial class SettingsWindow : Window, INotifyPropertyChanged
{
    private static readonly string[] _fontStyles = { "Normal", "Italic", "Oblique" };
    private static readonly string[] _fontWeights = { "Thin", "Light", "Normal", "Medium", "SemiBold", "Bold", "Black" };
    private static readonly string[] _intervalFormats =
    {
        @"m\:ss",
        @"mm\:ss",
        @"h\:mm",
        @"hh\:mm",
        @"h\:mm\:ss",
        @"hh\:mm\:ss",
    };

    private readonly DispatcherTimer _previewTimer;
    private readonly PropertyChangedEventHandler _settingsPropertyChanged;

    private string _previewCaption = string.Empty;
    private string _previewSupportText = string.Empty;
    private string _previewText = string.Empty;
    private string _countdownValidationMessage = string.Empty;
    private string _soundIntervalValidationMessage = string.Empty;

    public SettingsWindow()
    {
        InitializeComponent();

        DataContext = this;

        Width = Math.Max(MinWidth, Settings.SettingsWindowWidth);
        Height = Math.Max(MinHeight, Settings.SettingsWindowHeight);

        FontFamilies = Fonts.SystemFontFamilies
            .Select(fontFamily => fontFamily.Source)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name)
            .ToArray();

        FontStyles = _fontStyles;
        FontWeights = _fontWeights;

        StretchModes = new[]
        {
            new StretchOption("Fill the panel", Stretch.Fill),
            new StretchOption("Keep aspect ratio", Stretch.Uniform),
            new StretchOption("Fill and crop", Stretch.UniformToFill),
            new StretchOption("Original size", Stretch.None),
        };

        TimeZones = TimeZoneInfo.GetSystemTimeZones()
            .Select(timeZone => new TimeZoneOption(timeZone.Id, timeZone.DisplayName))
            .ToArray();

        _settingsPropertyChanged = (_, _) => RefreshDerivedState();
        Settings.PropertyChanged += _settingsPropertyChanged;

        _previewTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1),
            DispatcherPriority.Background,
            (_, _) => UpdatePreview(),
            Dispatcher);

        _previewTimer.Start();
        RefreshDerivedState();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public Settings Settings => Settings.Default;

    public IReadOnlyList<string> FontFamilies { get; }

    public IReadOnlyList<string> FontStyles { get; }

    public IReadOnlyList<string> FontWeights { get; }

    public IReadOnlyList<StretchOption> StretchModes { get; }

    public IReadOnlyList<TimeZoneOption> TimeZones { get; }

    public string PreviewText
    {
        get => _previewText;
        private set => SetField(ref _previewText, value, nameof(PreviewText));
    }

    public string PreviewCaption
    {
        get => _previewCaption;
        private set => SetField(ref _previewCaption, value, nameof(PreviewCaption));
    }

    public string PreviewSupportText
    {
        get => _previewSupportText;
        private set => SetField(ref _previewSupportText, value, nameof(PreviewSupportText));
    }

    public bool CountdownEnabled
    {
        get => Settings.CountdownTo != default;
        set
        {
            if (value == CountdownEnabled)
                return;

            Settings.CountdownTo = value ? CreateDefaultCountdownTarget() : default;
            SetCountdownValidationMessage(string.Empty);
            RefreshDerivedState();
        }
    }

    public DateTime? CountdownDate
    {
        get => CountdownEnabled ? Settings.CountdownTo.Date : null;
        set
        {
            if (!value.HasValue)
                return;

            var timeOfDay = CountdownEnabled ? Settings.CountdownTo.TimeOfDay : CreateDefaultCountdownTarget().TimeOfDay;
            Settings.CountdownTo = value.Value.Date + timeOfDay;
            SetCountdownValidationMessage(string.Empty);
            RefreshDerivedState();
        }
    }

    public string CountdownTimeText
    {
        get => CountdownEnabled ? Settings.CountdownTo.ToString("HH:mm", CultureInfo.InvariantCulture) : string.Empty;
        set
        {
            if (!CountdownEnabled)
                return;

            if (!TryParseTimeOfDay(value, out var timeOfDay))
            {
                SetCountdownValidationMessage("Enter the countdown time as HH:mm, for example 09:30 or 18:45.");
                OnPropertyChanged(nameof(CountdownTimeText));
                return;
            }

            Settings.CountdownTo = Settings.CountdownTo.Date + timeOfDay;
            SetCountdownValidationMessage(string.Empty);
            RefreshDerivedState();
        }
    }

    public string CountdownValidationMessage => _countdownValidationMessage;

    public bool HasCountdownValidationMessage => !string.IsNullOrWhiteSpace(CountdownValidationMessage);

    public string SelectedTimeZoneId
    {
        get => string.IsNullOrWhiteSpace(Settings.TimeZone) ? TimeZoneInfo.Local.Id : Settings.TimeZone;
        set
        {
            if (string.Equals(value, SelectedTimeZoneId, StringComparison.Ordinal))
                return;

            Settings.TimeZone = value ?? string.Empty;
            RefreshDerivedState();
        }
    }

    public string TextColorHex
    {
        get => ToHex(Settings.TextColor);
        set
        {
            if (!TryParseColor(value, out var color))
            {
                OnPropertyChanged(nameof(TextColorHex));
                return;
            }

            Settings.TextColor = color;
            RefreshDerivedState();
        }
    }

    public string OuterColorHex
    {
        get => ToHex(Settings.OuterColor);
        set
        {
            if (!TryParseColor(value, out var color))
            {
                OnPropertyChanged(nameof(OuterColorHex));
                return;
            }

            Settings.OuterColor = color;
            RefreshDerivedState();
        }
    }

    public string SoundIntervalText
    {
        get => Settings.WavFileInterval == default
            ? string.Empty
            : Settings.WavFileInterval.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Settings.WavFileInterval = default;
                SetSoundIntervalValidationMessage(string.Empty);
                RefreshDerivedState();
                return;
            }

            if (!TryParseInterval(value, out var interval))
            {
                SetSoundIntervalValidationMessage("Enter a duration like 00:01:00, 00:15:00, or 01:00:00.");
                OnPropertyChanged(nameof(SoundIntervalText));
                return;
            }

            Settings.WavFileInterval = interval;
            SetSoundIntervalValidationMessage(string.Empty);
            RefreshDerivedState();
        }
    }

    public string SoundIntervalValidationMessage => _soundIntervalValidationMessage;

    public bool HasSoundIntervalValidationMessage => !string.IsNullOrWhiteSpace(SoundIntervalValidationMessage);

    public bool UsesBackgroundImage => !string.IsNullOrWhiteSpace(Settings.BackgroundImagePath);

    public string SettingsFilePath => Settings.FilePath;

    public bool SaveWarningVisible => !Settings.CanBeSaved;

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshDerivedState();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!IsLoaded || WindowState != WindowState.Normal)
            return;

        Settings.SettingsWindowWidth = ActualWidth;
        Settings.SettingsWindowHeight = ActualHeight;
    }

    private void SettingsScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        TabContentScrollViewer.ScrollToVerticalOffset(Settings.SettingsScrollPosition);
    }

    private void SettingsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (!IsLoaded)
            return;

        Settings.SettingsScrollPosition = e.VerticalOffset;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _previewTimer.Stop();
        Settings.PropertyChanged -= _settingsPropertyChanged;
        Settings.Save();
        base.OnClosed(e);
    }

    private void BrowseBackgroundImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp|All files|*.*",
            Title = "Choose a background image",
        };

        if (dialog.ShowDialog(this) != true)
            return;

        Settings.BackgroundImagePath = dialog.FileName;
        RefreshDerivedState();
    }

    private void ClearBackgroundImage_Click(object sender, RoutedEventArgs e)
    {
        Settings.BackgroundImagePath = string.Empty;
        RefreshDerivedState();
    }

    private void BrowseSoundFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            Filter = "Wave files|*.wav|All files|*.*",
            Title = "Choose a WAV file",
        };

        if (dialog.ShowDialog(this) != true)
            return;

        Settings.WavFilePath = dialog.FileName;
        RefreshDerivedState();
    }

    private void ClearSoundFile_Click(object sender, RoutedEventArgs e)
    {
        Settings.WavFilePath = string.Empty;
        RefreshDerivedState();
    }

    private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
    {
        var folderPath = Path.GetDirectoryName(Settings.FilePath);
        if (string.IsNullOrWhiteSpace(folderPath))
            return;

        if (File.Exists(Settings.FilePath))
        {
            OpenShellTarget("explorer.exe", $"/select,\"{Settings.FilePath}\"");
            return;
        }

        OpenShellTarget(folderPath);
    }

    private void OpenDateFormatDocs_Click(object sender, RoutedEventArgs e)
    {
        OpenShellTarget("https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings");
    }

    private void OpenDurationFormatDocs_Click(object sender, RoutedEventArgs e)
    {
        OpenShellTarget("https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings");
    }

    private void ClockFormatPreset_Click(object sender, RoutedEventArgs e)
    {
        Settings.Format = ((FrameworkElement)sender).Tag?.ToString() ?? Settings.Format;
        RefreshDerivedState();
    }

    private void CountdownFormatPreset_Click(object sender, RoutedEventArgs e)
    {
        Settings.CountdownFormat = ((FrameworkElement)sender).Tag?.ToString() ?? string.Empty;
        RefreshDerivedState();
    }

    private void SoundIntervalPreset_Click(object sender, RoutedEventArgs e)
    {
        SoundIntervalText = ((FrameworkElement)sender).Tag?.ToString() ?? string.Empty;
    }

    private void TextColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        TextColorHex = ((FrameworkElement)sender).Tag?.ToString() ?? TextColorHex;
    }

    private void OuterColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        OuterColorHex = ((FrameworkElement)sender).Tag?.ToString() ?? OuterColorHex;
    }

    private void ThemePreset_Click(object sender, RoutedEventArgs e)
    {
        switch (((FrameworkElement)sender).Tag?.ToString())
        {
            case "Studio":
                Settings.BackgroundEnabled = true;
                Settings.TextColor = ParseColor("#152238");
                Settings.OuterColor = ParseColor("#FFFFFF");
                Settings.TextOpacity = 1;
                Settings.BackgroundOpacity = 0.92;
                Settings.BackgroundCornerRadius = 20;
                Settings.OutlineThickness = 0.18;
                break;

            case "Contrast":
                Settings.BackgroundEnabled = false;
                Settings.TextColor = ParseColor("#FFFFFF");
                Settings.OuterColor = ParseColor("#111827");
                Settings.TextOpacity = 1;
                Settings.BackgroundOpacity = 0.9;
                Settings.BackgroundCornerRadius = 12;
                Settings.OutlineThickness = 0.38;
                break;

            case "Night":
                Settings.BackgroundEnabled = true;
                Settings.TextColor = ParseColor("#EAF2FF");
                Settings.OuterColor = ParseColor("#162033");
                Settings.TextOpacity = 1;
                Settings.BackgroundOpacity = 0.88;
                Settings.BackgroundCornerRadius = 22;
                Settings.OutlineThickness = 0.22;
                break;

            case "Warm":
                Settings.BackgroundEnabled = true;
                Settings.TextColor = ParseColor("#5C2C06");
                Settings.OuterColor = ParseColor("#FFF1E7");
                Settings.TextOpacity = 0.98;
                Settings.BackgroundOpacity = 0.94;
                Settings.BackgroundCornerRadius = 24;
                Settings.OutlineThickness = 0.18;
                break;
        }

        RefreshDerivedState();
    }

    private void RefreshDerivedState()
    {
        OnPropertyChanged(nameof(CountdownEnabled));
        OnPropertyChanged(nameof(CountdownDate));
        OnPropertyChanged(nameof(CountdownTimeText));
        OnPropertyChanged(nameof(CountdownValidationMessage));
        OnPropertyChanged(nameof(HasCountdownValidationMessage));
        OnPropertyChanged(nameof(SelectedTimeZoneId));
        OnPropertyChanged(nameof(TextColorHex));
        OnPropertyChanged(nameof(OuterColorHex));
        OnPropertyChanged(nameof(SoundIntervalText));
        OnPropertyChanged(nameof(SoundIntervalValidationMessage));
        OnPropertyChanged(nameof(HasSoundIntervalValidationMessage));
        OnPropertyChanged(nameof(UsesBackgroundImage));
        OnPropertyChanged(nameof(SaveWarningVisible));
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var now = DateTimeOffset.Now;
        var localNow = DateTime.Now;

        PreviewText = TimeStringFormatter.Format(
            now,
            localNow,
            Settings.TimeZoneInfo,
            Settings.CountdownTo,
            Settings.Format,
            Settings.CountdownFormat,
            CultureInfo.CurrentCulture);

        if (CountdownEnabled)
        {
            PreviewCaption = $"Counting down to {Settings.CountdownTo:ddd, MMM d yyyy h:mm tt}";
            PreviewSupportText = string.IsNullOrWhiteSpace(Settings.CountdownFormat)
                ? "Countdown preview uses natural language until you add a custom duration format."
                : "Countdown preview uses your custom duration tokens.";
            return;
        }

        PreviewCaption = $"Showing time in {Settings.TimeZoneInfo.DisplayName}";
        PreviewSupportText = Settings.BackgroundEnabled
            ? "Background, image, and color changes are reflected live in the preview."
            : "Outline mode is active, so the secondary color is applied to the text stroke.";
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void SetCountdownValidationMessage(string message)
    {
        if (_countdownValidationMessage == message)
            return;

        _countdownValidationMessage = message;
        OnPropertyChanged(nameof(CountdownValidationMessage));
        OnPropertyChanged(nameof(HasCountdownValidationMessage));
    }

    private void SetSoundIntervalValidationMessage(string message)
    {
        if (_soundIntervalValidationMessage == message)
            return;

        _soundIntervalValidationMessage = message;
        OnPropertyChanged(nameof(SoundIntervalValidationMessage));
        OnPropertyChanged(nameof(HasSoundIntervalValidationMessage));
    }

    private void OpenShellTarget(string target, string arguments = null)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = target,
                Arguments = arguments ?? string.Empty,
                UseShellExecute = true,
            });
        }
        catch
        {
        }
    }

    private static DateTime CreateDefaultCountdownTarget()
    {
        var now = DateTime.Now.AddHours(1);
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
    }

    private static bool TryParseTimeOfDay(string value, out TimeSpan timeOfDay)
    {
        return TimeSpan.TryParseExact(
            value?.Trim(),
            new[] { @"h\:mm", @"hh\:mm" },
            CultureInfo.InvariantCulture,
            out timeOfDay);
    }

    private static bool TryParseInterval(string value, out TimeSpan interval)
    {
        return TimeSpan.TryParseExact(
            value?.Trim(),
            _intervalFormats,
            CultureInfo.InvariantCulture,
            out interval) &&
            interval > TimeSpan.Zero;
    }

    private static bool TryParseColor(string value, out Color color)
    {
        try
        {
            var normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                color = default;
                return false;
            }

            if (!normalized.StartsWith("#", StringComparison.Ordinal))
                normalized = "#" + normalized;

            var converted = ColorConverter.ConvertFromString(normalized);
            if (converted is Color parsedColor)
            {
                color = Color.FromRgb(parsedColor.R, parsedColor.G, parsedColor.B);
                return true;
            }
        }
        catch
        {
        }

        color = default;
        return false;
    }

    private static Color ParseColor(string value)
    {
        return TryParseColor(value, out var color) ? color : Colors.White;
    }

    private static string ToHex(Color color)
    {
        return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void SetField(ref string field, string value, string propertyName)
    {
        if (string.Equals(field, value, StringComparison.Ordinal))
            return;

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class StretchOption
{
    public StretchOption(string label, Stretch value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public Stretch Value { get; }
}

public sealed class TimeZoneOption
{
    public TimeZoneOption(string id, string displayName)
    {
        Id = id;
        DisplayName = displayName;
    }

    public string Id { get; }

    public string DisplayName { get; }
}
