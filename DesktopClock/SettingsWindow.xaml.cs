using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        FontFamilies = Fonts.SystemFontFamilies.Select(ff => ff.Source).ToList();
        FontStyles = ["Normal", "Italic", "Oblique"];
        TimeZones = TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id).ToList();
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
    /// All available time zones reported by the system.
    /// </summary>
    public IList<string> TimeZones { get; }

    /// <summary>
    /// Sets the format string in settings.
    /// </summary>
    [RelayCommand]
    public void SetFormat(DateFormatExample value)
    {
        Settings.Default.Format = value.Format;
    }

    /// <summary>
    /// Disables countdown mode by resetting the value to default.
    /// </summary>
    [RelayCommand]
    public void ResetCountdown()
    {
        Settings.CountdownTo = default;
    }
}
