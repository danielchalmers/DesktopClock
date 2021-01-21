using System.Reflection;
using System.Windows;
using DesktopClock.Properties;

namespace DesktopClock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
        public static string Title { get; } = Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (!Settings.Default.CheckIfModifiedExternally())
                Settings.Default.Save();
        }
    }
}