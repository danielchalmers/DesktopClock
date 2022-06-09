using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using DesktopClock.Properties;

namespace DesktopClock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();
    public static string Title { get; } = Assembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;

    // https://www.materialui.co/colors
    public static IReadOnlyList<Theme> Themes { get; } = new Theme[]
    {
        new Theme("White", "#FFFFFF", "#000000"),
        new Theme("Black", "#000000", "#9E9E9E"),
        new Theme("Grey", "#9E9E9E", "#000000"),
        new Theme("Red", "#ff5252", "#212121"),
        new Theme("Pink", "#FF4081", "#212121"),
        new Theme("Purple", "#E040FB", "#212121"),
        new Theme("Deep Purple", "#7C4DFF", "#212121"),
        new Theme("Indigo", "#536DFE", "#212121"),
        new Theme("Blue", "#448AFF", "#212121"),
        new Theme("Light Blue", "#40C4FF", "#212121"),
        new Theme("Cyan", "#18FFFF", "#212121"),
        new Theme("Teal", "#64FFDA", "#212121"),
        new Theme("Green", "#69F0AE", "#212121"),
        new Theme("Light Green", "#B2FF59", "#212121"),
        new Theme("Lime", "#EEFF41", "#212121"),
        new Theme("Yellow", "#FFFF00", "#212121"),
        new Theme("Amber", "#FFD740", "#212121"),
        new Theme("Orange", "#FFAB40", "#212121"),
        new Theme("Deep Orange", "#FF6E40", "#212121"),
    };

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        if (!Settings.Default.CheckIfModifiedExternally())
            Settings.Default.Save();
    }
}