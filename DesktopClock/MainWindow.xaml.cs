using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DesktopClock.Properties;

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
            Settings.Default.Height = Math.Min(Math.Max(Settings.Default.Height + change, 16), 160);
        }
    }

    private void MenuItemCopy_OnClick(object sender, RoutedEventArgs e)
    {
        CopyToClipboard();
    }

    private void MenuItemCountdown_OnClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(this,
            $"Go to settings, change the {nameof(Settings.Default.CountdownTo)} option, and restart.\n\n" +
            $"Get the clock back by deleting everything between the quotes.");
    }

    private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
    {
        Settings.Default.Save();

        // Open settings file in notepad.
        Process.Start("notepad", Settings.Path);
    }

    private void MenuItemCheckForUpdates_OnClick(object sender, RoutedEventArgs e)
    {
        Process.Start("https://github.com/danielchalmers/DesktopClock/releases");
    }

    private void MenuItemExit_OnClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}