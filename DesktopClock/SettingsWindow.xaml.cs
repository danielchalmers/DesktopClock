using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
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

        ((SettingsWindowViewModel)DataContext).Settings.BackgroundImagePath = openFileDialog.FileName;
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

        ((SettingsWindowViewModel)DataContext).Settings.WavFilePath = openFileDialog.FileName;
    }

    public static void ShowSingletonSettingsWindow(Window owner)
    {
        var settingsWindow = Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault() ?? new SettingsWindow();

        if (settingsWindow.IsVisible)
        {
            settingsWindow.Activate();
            return;
        }

        settingsWindow.Show();
    }
}

public class SettingsWindowViewModel
{
    public Settings Settings { get; }

    public SettingsWindowViewModel(Settings settings)
    {
        Settings = settings;
        FontFamilies = Fonts.SystemFontFamilies.Select(ff => ff.Source).ToList();
        TimeZones = TimeZoneInfo.GetSystemTimeZones().Select(tz => tz.Id).ToList();
    }

    public IList<string> FontFamilies { get; }
    public IList<string> TimeZones { get; }
}
