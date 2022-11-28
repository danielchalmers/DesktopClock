using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using DesktopClock.Properties;
using WpfWindowPlacement;

namespace DesktopClock;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private void CopyToClipboard() =>
        Clipboard.SetText(TimeTextBlock.Text);

    private void Window_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        CopyToClipboard();
    }

    private void Window_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // Scale size based on scroll amount, with one notch on a default PC mouse being a change of 15%.
            var steps = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
            var change = Settings.Default.Height * steps * 0.15;
            Settings.Default.Height = (int)Math.Min(Math.Max(Settings.Default.Height + change, 16), 160);
        }
    }

    private void MenuItemCopy_OnClick(object sender, RoutedEventArgs e)
    {
        CopyToClipboard();
    }

    private void MenuItemNew_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this,
            $"This will make a copy of the executable and start it with new settings.\n\n" +
            $"Continue?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.OK);

        if (result != MessageBoxResult.OK)
            return;

        var exeFileInfo = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
        var newExePath = Path.Combine(exeFileInfo.DirectoryName, Guid.NewGuid().ToString() + exeFileInfo.Name);
        File.Copy(exeFileInfo.FullName, newExePath);
        Process.Start(newExePath);
    }

    private void MenuItemCountdown_OnClick(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(this,
            $"In advanced settings: change {nameof(Settings.Default.CountdownTo)}, then restart.\n" +
            "Go back by replacing it with \"0001-01-01T00:00:00+00:00\".\n\n" +
            "Open advanced settings now?",
            Title, MessageBoxButton.OKCancel, MessageBoxImage.Information, MessageBoxResult.OK);

        if (result == MessageBoxResult.OK)
            MenuItemSettings_OnClick(this, new RoutedEventArgs());
    }

    private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Default.Save();

        // Re-create the settings file if it got deleted.
        if (!File.Exists(Settings.Path))
            Settings.Default.Save();

        // Open settings file in notepad.
        try
        {
            Process.Start("notepad", Settings.Path);
        }
        catch (Exception ex)
        {
            // Lazy scammers on the Microsoft Store may reupload without realizing it's sandboxed, which makes it unable to start the Notepad process.
            MessageBox.Show(this,
                "Couldn't open settings file.\n\n" +
                "This app may have be reuploaded without permission. If you paid for it, ask for a refund and download it for free from the original source: https://github.com/danielchalmers/DesktopClock.\n\n" +
                $"If it still doesn't work, report it as an issue at that link with details on what happened and include this error: \"{ex.Message}\"");
        }
    }

    private void MenuItemCheckForUpdates_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start("https://github.com/danielchalmers/DesktopClock/releases");
    }

    private void MenuItemExit_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_SourceInitialized(object sender, EventArgs e)
    {
        WindowPlacementFunctions.SetPlacement(this, Settings.Default.Placement);
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Settings.Default.Placement = WindowPlacementFunctions.GetPlacement(this);

        Settings.Default.SaveIfNotModifiedExternally();

        SettingsHelper.SetRunOnStartup(Settings.Default.RunOnStartup);
    }
}