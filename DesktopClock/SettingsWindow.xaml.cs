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
    private bool _restoringScrollPosition = true;

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
                "Settings are stored in JSON and will open in Notepad. Save the file for changes to take effect. To start fresh, delete your '.settings' file.",
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
                "Settings file doesn't exist and couldn't be created.",
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
                "Couldn't open settings file in Notepad.\n\n" +
                "This app may have been stolen. If you paid for it, ask for a refund and download it for free from https://github.com/danielchalmers/DesktopClock.\n\n" +
                $"If it still doesn't work, create a new issue at that link with details on what happened and include this error: \"{ex.Message}\"",
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
            "This will copy the executable and start it with new settings.\n\n" +
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

    private void SettingsScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_restoringScrollPosition)
        {
            return;
        }

        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            SettingsScrollViewer.ScrollToVerticalOffset(ViewModel.Settings.SettingsScrollPosition);
            ViewModel.Settings.SettingsScrollPosition = SettingsScrollViewer.VerticalOffset;
            _restoringScrollPosition = false;
        }));
    }

    private void SettingsScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (_restoringScrollPosition)
        {
            return;
        }

        ViewModel.Settings.SettingsScrollPosition = e.VerticalOffset;
    }

    private void SettingsWindow_Closing(object sender, CancelEventArgs e)
    {
        ViewModel.Settings.SettingsScrollPosition = SettingsScrollViewer.VerticalOffset;
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
}

public partial class SettingsWindowViewModel : ObservableObject, IDisposable
{
    public Settings Settings { get; }

    public SettingsWindowViewModel(Settings settings)
    {
        Settings = settings;
        FontFamilies = GetAllSystemFonts().Distinct().OrderBy(f => f).ToList();
        FontStyles = ["Normal", "Italic", "Oblique"];
        FontWeights = ["Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold", "ExtraBold", "Black", "ExtraBlack"];
        TextTransforms = Enum.GetValues(typeof(TextTransform)).Cast<TextTransform>().ToArray();
        ImageStretches = Enum.GetValues(typeof(Stretch)).Cast<Stretch>().ToArray();
        TimeZones = TimeZoneInfo.GetSystemTimeZones();

        Settings.PropertyChanged += Settings_PropertyChanged;
    }

    public IList<string> FontFamilies { get; }

    public IList<string> FontStyles { get; }

    public IList<string> FontWeights { get; }

    public IList<TextTransform> TextTransforms { get; }

    public IList<Stretch> ImageStretches { get; }

    public IList<TimeZoneInfo> TimeZones { get; }

    public string PreviewTimeText => FormatPreviewText(default);

    public string PreviewCountdownText => FormatPreviewText(GetPreviewCountdownTarget());

    [RelayCommand]
    public void SetFormat(DateFormatExample value)
    {
        Settings.Default.Format = value.Format;
    }

    [RelayCommand]
    public void ResetCountdown()
    {
        Settings.CountdownTo = default;
        RaisePreviewChanged();
    }

    [RelayCommand]
    public void ResetCountdownFormat()
    {
        Settings.CountdownFormat = string.Empty;
        RaisePreviewChanged();
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
        RaisePreviewChanged();
    }

    public void Dispose()
    {
        Settings.PropertyChanged -= Settings_PropertyChanged;
    }

    private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        RaisePreviewChanged();
    }

    private void RaisePreviewChanged()
    {
        OnPropertyChanged(nameof(PreviewTimeText));
        OnPropertyChanged(nameof(PreviewCountdownText));
    }

    private string FormatPreviewText(DateTime countdownTo)
    {
        try
        {
            return TimeStringFormatter.Format(
                DateTimeOffset.Now,
                DateTime.Now,
                Settings.TimeZoneInfo,
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

    private DateTime GetPreviewCountdownTarget()
    {
        if (Settings.CountdownTo != default)
        {
            return Settings.CountdownTo;
        }

        return DateTime.Now.AddHours(2).AddMinutes(17);
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
