using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
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
    }

    private SettingsWindowViewModel ViewModel => (SettingsWindowViewModel)DataContext;

    private void SelectFormat(object sender, SelectionChangedEventArgs e)
    {
        var value = e.AddedItems[0] as DateFormatExample;

        if (value == null)
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
        // Teach user how it works.
        if (!Settings.Default.TipsShown.HasFlag(TeachingTips.AdvancedSettings))
        {
            MessageBox.Show(this,
                "Settings are stored in JSON format and will be opened in Notepad. Save the file for your changes to take effect. To start fresh, delete your '.settings' file.",
                Title, MessageBoxButton.OK, MessageBoxImage.Information);

            Settings.Default.TipsShown |= TeachingTips.AdvancedSettings;
        }

        // Save first if we can so it's up-to-date.
        if (Settings.CanBeSaved)
            Settings.Default.Save();

        // If it doesn't even exist then it's probably somewhere that requires special access and we shouldn't even be at this point.
        if (!Settings.Exists)
        {
            MessageBox.Show(this,
                "Settings file doesn't exist and couldn't be created.",
                Title, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Open settings file in notepad.
        try
        {
            Process.Start("notepad", Settings.FilePath);
        }
        catch (Exception ex)
        {
            // Lazy scammers on the Microsoft Store may reupload without realizing it gets sandboxed, making it unable to start the Notepad process (#1, #12).
            MessageBox.Show(this,
                "Couldn't open settings file in Notepad.\n\n" +
                "This app may have be stolen. If you paid for it, ask for a refund and download it for free from https://github.com/danielchalmers/DesktopClock.\n\n" +
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
            return;

        var newExePath = Path.Combine(App.MainFileInfo.DirectoryName, App.MainFileInfo.GetFileAtNextIndex().Name);

        // Copy and start the new clock.
        File.Copy(App.MainFileInfo.FullName, newExePath);
        Process.Start(newExePath);
    }

    private void CheckForUpdates(object sender, RoutedEventArgs e)
    {
        OpenUrl("https://github.com/danielchalmers/DesktopClock/releases");
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

public partial class SettingsWindowViewModel : ObservableObject
{
    public Settings Settings { get; }

    public SettingsWindowViewModel(Settings settings)
    {
        Settings = settings;
        FontFamilies = GetAllSystemFonts().Distinct().OrderBy(f => f).ToList();
        FontStyles = ["Normal", "Italic", "Oblique"];
        FontWeights = ["Thin", "ExtraLight", "Light", "Normal", "Medium", "SemiBold", "Bold", "ExtraBold", "Black", "ExtraBlack"];
        TextTransforms = Enum.GetValues(typeof(TextTransform)).Cast<TextTransform>().ToArray();
        TimeZones = TimeZoneInfo.GetSystemTimeZones();
    }

    /// <summary>
    /// All available font families reported by the system.
    /// </summary>
    public IList<string> FontFamilies { get; }

    /// <summary>
    /// All available font styles.
    /// </summary>
    public IList<string> FontStyles { get; }

    /// <summary>
    /// All available font weights.
    /// </summary>
    public IList<string> FontWeights { get; }

    /// <summary>
    /// All available text transformations.
    /// </summary>
    public IList<TextTransform> TextTransforms { get; }

    /// <summary>
    /// All available time zones reported by the system.
    /// </summary>
    public IList<TimeZoneInfo> TimeZones { get; }

    /// <summary>
    /// Sets the format string in settings.
    /// </summary>
    [RelayCommand]
    public void SetFormat(DateFormatExample value)
    {
        Settings.Default.Format = value.Format;
    }

    /// <summary>
    /// Disables countdown mode by resetting the date to default.
    /// </summary>
    [RelayCommand]
    public void ResetCountdown()
    {
        Settings.CountdownTo = default;
    }

    /// <summary>
    /// Resets the countdown format to the default (dynamic) format.
    /// </summary>
    [RelayCommand]
    public void ResetCountdownFormat()
    {
        Settings.CountdownFormat = string.Empty;
    }

    /// <summary>
    /// Clears the chime sound file path.
    /// </summary>
    [RelayCommand]
    public void ResetWavFilePath()
    {
        Settings.WavFilePath = string.Empty;
    }

    /// <summary>
    /// Resets the chime interval to the default value.
    /// </summary>
    [RelayCommand]
    public void ResetWavFileInterval()
    {
        Settings.WavFileInterval = default;
    }

    /// <summary>
    /// Clears the background image path.
    /// </summary>
    [RelayCommand]
    public void ResetBackgroundImagePath()
    {
        Settings.BackgroundImagePath = string.Empty;
    }

    private IEnumerable<string> GetAllSystemFonts()
    {
        // Get fonts from WPF.
        foreach (var fontFamily in Fonts.SystemFontFamilies)
        {
            yield return fontFamily.Source;
        }

        // Get fonts from System.Drawing.
        using var installedFontCollection = new InstalledFontCollection();
        foreach (var fontFamily in installedFontCollection.Families)
        {
            yield return fontFamily.Name;
        }
    }
}
