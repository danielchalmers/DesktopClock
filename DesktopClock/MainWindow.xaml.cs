using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DesktopClock.Properties;

namespace DesktopClock
{
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

        private void MenuItemCopy_OnClick(object sender, RoutedEventArgs e)
        {
            CopyToClipboard();
        }

        private void MenuItemSettings_OnClick(object sender, RoutedEventArgs e)
        {
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
}