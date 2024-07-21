using System.Windows;

namespace DesktopClock;

public static class WindowUtil
{
    /// <summary>
    /// Hides the window until the user opens it again through the taskbar, tray, alt-tab, etc.
    /// </summary>
    public static void HideFromScreen(this Window window)
    {
        // Minimize the window and update the ShowInTaskbar property to keep it hidden if needed.
        // https://stackoverflow.com/a/28239057.
        var wasShownInTaskbar = window.ShowInTaskbar;
        window.ShowInTaskbar = true;
        window.WindowState = WindowState.Minimized;
        window.ShowInTaskbar = wasShownInTaskbar;
    }
}
