using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopClock.Properties;
using Microsoft.Win32;

namespace DesktopClock;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new SettingsWindowViewModel(Settings.Default);
        Closing += SettingsWindow_Closing;
    }

    private SettingsWindowViewModel ViewModel => (SettingsWindowViewModel)DataContext;

    private void SelectFormat(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.Count == 0)
        {
            return;
        }

        if (e.AddedItems[0] is not DateFormatExample value)
        {
            return;
        }

        ViewModel.Settings.Format = value.Format;
    }

    private void InsertClockFormatToken_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string token)
        {
            return;
        }

        InsertTextAtCaret(ClockFormatTextBox, token);
    }

    private void InsertCountdownFormatToken_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string token)
        {
            return;
        }

        InsertTextAtCaret(CountdownFormatTextBox, token);
    }

    private void ApplyCountdownPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.Tag is not string presetKey)
        {
            return;
        }

        ViewModel.ApplyCountdownPreset(presetKey);
    }

    private void BrowseBackgroundImagePath(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|All files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        ViewModel.Settings.BackgroundImagePath = openFileDialog.FileName;
    }

    private void BrowseWavFilePath(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }

        ViewModel.Settings.WavFilePath = openFileDialog.FileName;
    }

    private void PickTextColor(object sender, RoutedEventArgs e)
    {
        PickColor(color => ViewModel.Settings.TextColor = color, ViewModel.Settings.TextColor);
    }

    private void PickOuterColor(object sender, RoutedEventArgs e)
    {
        PickColor(color => ViewModel.Settings.OuterColor = color, ViewModel.Settings.OuterColor);
    }

    private void PickColor(Action<Color> applyColor, Color currentColor)
    {
        using var colorDialog = new System.Windows.Forms.ColorDialog
        {
            AllowFullOpen = true,
            FullOpen = true,
            Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B)
        };

        if (colorDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        applyColor(Color.FromArgb(
            colorDialog.Color.A,
            colorDialog.Color.R,
            colorDialog.Color.G,
            colorDialog.Color.B));
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OpenSettingsFile(object sender, RoutedEventArgs e)
    {
        if (!Settings.Default.TipsShown.HasFlag(TeachingTips.AdvancedSettings))
        {
            MessageBox.Show(this,
                "Your settings are stored in a JSON file and will open in Notepad. Save the file after editing for changes to take effect. To reset everything, delete the '.settings' file.",
                Title, MessageBoxButton.OK, MessageBoxImage.Information);

            Settings.Default.TipsShown |= TeachingTips.AdvancedSettings;
        }

        if (Settings.CanBeSaved)
        {
            Settings.Default.Save();
        }

        if (!Settings.Exists)
        {
            MessageBox.Show(this,
                "The settings file doesn't exist and couldn't be created.",
                Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            Process.Start("notepad", Settings.FilePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Couldn't open the settings file in Notepad.\n\n" +
                "DesktopClock is free and open source. If you paid for a copy, request a refund and download it from https://github.com/danielchalmers/DesktopClock.\n\n" +
                $"If this keeps happening, create an issue at that link and include this error: \"{ex.Message}\"",
                Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenSettingsFolder(object sender, RoutedEventArgs e)
    {
        OpenSettingsPath(Settings.FilePath);
    }

    private void CreateNewClock(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this,
            "This will copy the app and launch the copy with its own settings file.\n\n" +
            "Continue?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
        {
            return;
        }

        var newExePath = Path.Combine(App.MainFileInfo.DirectoryName, App.MainFileInfo.GetFileAtNextIndex().Name);
        File.Copy(App.MainFileInfo.FullName, newExePath);
        Process.Start(newExePath);
    }

    private void CheckForUpdates(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/danielchalmers/DesktopClock/releases");
    }

    private void SettingsWindow_Closing(object sender, CancelEventArgs e)
    {
        ViewModel.Dispose();
    }

    private static void OpenUrl(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static void OpenSettingsPath(string filePath)
    {
        var folderPath = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo("explorer.exe", folderPath) { UseShellExecute = true });
    }

    private static void InsertTextAtCaret(TextBox textBox, string text)
    {
        textBox.Focus();

        var selectionStart = textBox.SelectionStart;
        var selectionLength = textBox.SelectionLength;
        var originalText = textBox.Text ?? string.Empty;

        textBox.Text = originalText.Remove(selectionStart, selectionLength).Insert(selectionStart, text);
        textBox.SelectionStart = selectionStart + text.Length;
        textBox.SelectionLength = 0;
    }
}

public partial class SettingsWindowViewModel : ObservableObject, IDisposable
{
    private readonly SystemClockTimer _systemClockTimer;
    private bool _syncingCountdownEditor;
    private bool _countdownTargetHasParseError;

    [ObservableProperty]
    private string _previewTimeText;

    [ObservableProperty]
    private string _previewCountdownText;

    private string _countdownTargetText;

    public Settings Settings { get; }

    public SettingsWindowViewModel(Settings settings)
    {
        Settings = settings;
        FontFamilies = GetAllSystemFonts().Distinct().OrderBy(f => f).ToList();
        FontStyles = ["Normal", "Italic", "Oblique"];
        FontWeights = ["Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold", "ExtraBold", "Black", "ExtraBlack"];
        TextTransforms =
        [
            new NamedOption<TextTransform>(TextTransform.None, "No change"),
            new NamedOption<TextTransform>(TextTransform.Uppercase, "UPPERCASE"),
            new NamedOption<TextTransform>(TextTransform.Lowercase, "lowercase"),
        ];
        ImageStretches =
        [
            new NamedOption<Stretch>(Stretch.None, "Original size"),
            new NamedOption<Stretch>(Stretch.Fill, "Stretch to fill"),
            new NamedOption<Stretch>(Stretch.Uniform, "Fit inside"),
            new NamedOption<Stretch>(Stretch.UniformToFill, "Fill and crop"),
        ];
        TimeZones = TimeZoneInfo.GetSystemTimeZones();

        Settings.PropertyChanged += Settings_PropertyChanged;
        _systemClockTimer = new();
        _systemClockTimer.SecondChanged += SystemClockTimer_SecondChanged;
        _systemClockTimer.Start();
        SyncCountdownEditorFromSettings();
        RefreshPreviewText();
    }

    public IList<string> FontFamilies { get; }

    public IList<string> FontStyles { get; }

    public IList<string> FontWeights { get; }

    public IList<NamedOption<TextTransform>> TextTransforms { get; }

    public IList<NamedOption<Stretch>> ImageStretches { get; }

    public IList<TimeZoneInfo> TimeZones { get; }

    public string CountdownTargetText
    {
        get => _countdownTargetText;
        set
        {
            if (!SetProperty(ref _countdownTargetText, value))
            {
                return;
            }

            ApplyCountdownTargetText(value);
        }
    }

    public string CountdownTargetSummary => _countdownTargetHasParseError
        ? "Couldn't read that date and time. Try something like 3/14/2026 9:30 AM."
        : Settings.CountdownTo == default
        ? "Countdown is turned off."
        : Settings.CountdownTo.ToString("f", CultureInfo.CurrentCulture);

    public bool IsCountdownEnabled => Settings.CountdownTo != default;

    [RelayCommand]
    public void SetFormat(DateFormatExample value)
    {
        Settings.Default.Format = value.Format;
    }

    [RelayCommand]
    public void ResetCountdown()
    {
        Settings.CountdownTo = default;
        SyncCountdownEditorFromSettings();
    }

    [RelayCommand]
    public void ResetCountdownFormat()
    {
        Settings.CountdownFormat = string.Empty;
    }

    [RelayCommand]
    public void ResetWavFilePath()
    {
        Settings.WavFilePath = string.Empty;
    }

    [RelayCommand]
    public void ResetWavFileInterval()
    {
        Settings.WavFileInterval = default;
    }

    [RelayCommand]
    public void ResetBackgroundImagePath()
    {
        Settings.BackgroundImagePath = string.Empty;
    }

    public void ApplyCountdownPreset(string presetKey)
    {
        var now = DateTime.Now;
        var target = presetKey switch
        {
            "plus_hour" => RoundUpToNextMinute(now.AddHours(1)),
            "tonight" => GetFutureTodayOrTomorrow(18, 0),
            "tomorrow_morning" => now.Date.AddDays(1).AddHours(9),
            "next_week" => now.Date.AddDays(7).AddHours(9),
            _ => now.AddHours(1),
        };

        Settings.CountdownTo = new DateTime(target.Year, target.Month, target.Day, target.Hour, target.Minute, 0);
        SyncCountdownEditorFromSettings();
        RefreshPreviewText();
    }

    public void Dispose()
    {
        Settings.PropertyChanged -= Settings_PropertyChanged;
        _systemClockTimer.SecondChanged -= SystemClockTimer_SecondChanged;
        _systemClockTimer.Dispose();
    }

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(Settings.Format):
            case nameof(Settings.TimeZone):
            case nameof(Settings.TextTransform):
            case nameof(Settings.CountdownTo):
            case nameof(Settings.CountdownFormat):
                if (e.PropertyName == nameof(Settings.CountdownTo))
                {
                    SyncCountdownEditorFromSettings();
                    OnPropertyChanged(nameof(CountdownTargetSummary));
                    OnPropertyChanged(nameof(IsCountdownEnabled));
                }

                RefreshPreviewText();
                break;
        }
    }

    private void SystemClockTimer_SecondChanged(object sender, EventArgs e)
    {
        Application.Current?.Dispatcher.Invoke(RefreshPreviewText);
    }

    private string FormatPreviewText(DateTime countdownTo)
    {
        try
        {
            return TimeStringFormatter.Format(
                DateTimeOffset.Now,
                DateTime.Now,
                TimeZoneInfo.Local,
                countdownTo,
                Settings.Format,
                Settings.CountdownFormat,
                Settings.TextTransform,
                CultureInfo.CurrentCulture);
        }
        catch
        {
            return countdownTo == default ? "Preview unavailable" : "Countdown preview unavailable";
        }
    }

    private void RefreshPreviewText()
    {
        PreviewTimeText = FormatPreviewText(default);
        PreviewCountdownText = FormatPreviewText(GetPreviewCountdownTarget());
    }

    private void SyncCountdownEditorFromSettings()
    {
        _syncingCountdownEditor = true;
        _countdownTargetHasParseError = false;

        CountdownTargetText = Settings.CountdownTo == default
            ? string.Empty
            : Settings.CountdownTo.ToString("g", CultureInfo.CurrentCulture);

        _syncingCountdownEditor = false;
        OnPropertyChanged(nameof(CountdownTargetSummary));
        OnPropertyChanged(nameof(IsCountdownEnabled));
    }

    private void ApplyCountdownTargetText(string value)
    {
        if (_syncingCountdownEditor)
        {
            return;
        }

        var trimmedValue = value?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(trimmedValue))
        {
            _countdownTargetHasParseError = false;
            Settings.CountdownTo = default;
            OnPropertyChanged(nameof(CountdownTargetSummary));
            OnPropertyChanged(nameof(IsCountdownEnabled));
            return;
        }

        if (!DateTime.TryParse(trimmedValue, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var countdownTo))
        {
            _countdownTargetHasParseError = true;
            OnPropertyChanged(nameof(CountdownTargetSummary));
            return;
        }

        _countdownTargetHasParseError = false;
        Settings.CountdownTo = new DateTime(
            countdownTo.Year,
            countdownTo.Month,
            countdownTo.Day,
            countdownTo.Hour,
            countdownTo.Minute,
            0);
        OnPropertyChanged(nameof(CountdownTargetSummary));
        OnPropertyChanged(nameof(IsCountdownEnabled));
    }

    private DateTime GetPreviewCountdownTarget()
    {
        if (Settings.CountdownTo != default)
        {
            return Settings.CountdownTo;
        }

        return DateTime.Now.AddHours(2).AddMinutes(17);
    }

    private static DateTime GetFutureTodayOrTomorrow(int hour, int minute)
    {
        var now = DateTime.Now;
        var today = now.Date.AddHours(hour).AddMinutes(minute);
        return today > now ? today : today.AddDays(1);
    }

    private static DateTime RoundUpToNextMinute(DateTime dateTime)
    {
        var rounded = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0);
        return dateTime.Second == 0 && dateTime.Millisecond == 0 ? rounded : rounded.AddMinutes(1);
    }

    private IEnumerable<string> GetAllSystemFonts()
    {
        foreach (var fontFamily in Fonts.SystemFontFamilies)
        {
            yield return fontFamily.Source;
        }

        using var installedFontCollection = new InstalledFontCollection();
        foreach (var fontFamily in installedFontCollection.Families)
        {
            yield return fontFamily.Name;
        }
    }
}

public sealed class NamedOption<T>
{
    public NamedOption(T value, string label)
    {
        Value = value;
        Label = label;
    }

    public T Value { get; }

    public string Label { get; }
}
