using System.Reflection;
using System.Windows;
using Clock_Widget.Properties;

namespace Clock_Widget
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
        public static string Title { get; } = Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

        private void UpgradeSettingsIfRequired()
        {
            if (Settings.Default.ShouldUpgrade)
            {
                Settings.Default.Upgrade();
                Settings.Default.ShouldUpgrade = false;
            }
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            UpgradeSettingsIfRequired();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Settings.Default.Save();
        }
    }
}