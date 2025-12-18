using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
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

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
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

    private IEnumerable<string> GetAllSystemFonts()
    {
        // Get fonts from WPF.
        foreach (var fontFamily in Fonts.SystemFontFamilies)
        {
            yield return fontFamily.Source;
        }

        // Get fonts from System.Drawing.
        var installedFontCollection = new InstalledFontCollection();
        foreach (var fontFamily in installedFontCollection.Families)
        {
            yield return fontFamily.Name;
        }
    }
}
