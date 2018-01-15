using System.Reflection;
using System.Windows;

namespace Clock_Widget
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
        public static string Title { get; } = Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
    }
}