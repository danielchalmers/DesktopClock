using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DesktopClock.Properties;
using WpfAboutView;

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

        private void MenuItemAbout_OnClick(object sender, RoutedEventArgs e)
        {
            new AboutDialog
            {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                AboutView = new AboutView
                {
                    AppIconSource = new Uri("pack://application:,,,/DesktopClock.ico"),
                    CreditColumns = 3,
                    Credits = new ObservableCollection<Credit> {
                        new Credit {
                            Name = "DesktopClock",
                            Author = "Daniel Chalmers",
                            Website = new Uri("https://github.com/danielchalmers/DesktopClock"),
                            LicenseText = Properties.Resources.DesktopClock_License
                        },
                        new Credit {
                            Name = "Costura.Fody",
                            Author = "Simon Cropp",
                            Website = new Uri("https://github.com/Fody/Costura"),
                            LicenseText = Properties.Resources.Costura_Fody_License
                        },
                        new Credit {
                            Name = "MVVM Light",
                            Author = "Laurent Bugnion",
                            Website = new Uri("http://mvvmlight.net"),
                            LicenseText = Properties.Resources.MVVM_Light_License
                        },
                        new Credit {
                            Name = "Json.NET",
                            Author = "James Newton-King",
                            Website = new Uri("https://newtonsoft.com/json"),
                           LicenseText = Properties.Resources.Json_NET_License
                        },
                        new Credit {
                            Name = "PropertyChanged.Fody",
                            Author = "Simon Cropp",
                            Website = new Uri("https://github.com/Fody/PropertyChanged"),
                            LicenseText = Properties.Resources.PropertyChanged_Fody_License
                        },
                        new Credit {
                            Name = "WpfAboutView",
                            Author = "Daniel Chalmers",
                            Website = new Uri("https://github.com/danielchalmers/WpfAboutView"),
                            LicenseText = Properties.Resources.WpfAboutView_License
                        },
                        new Credit {
                            Name = "WpfWindowPlacement",
                            Author = "Daniel Chalmers",
                            Website = new Uri("https://github.com/danielchalmers/WpfWindowPlacement"),
                            LicenseText = Properties.Resources.WpfWindowPlacement_License
                        },
                    }
                }
            }.ShowDialog();
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