using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace DesktopClock;

public static class WindowUtil
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_FRAMECHANGED = 0x0020;

    private static readonly IntPtr HWND_TOPMOST = new(-1);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hwnd,
        IntPtr insertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

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

    /// <summary>
    /// Makes the window click-through (transparent to mouse clicks).
    /// </summary>
    public static void SetClickThrough(this Window window, bool clickThrough)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        if (clickThrough)
        {
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
        }
        else
        {
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
        }
    }

    /// <summary>
    /// Applies the window's taskbar and Alt+Tab visibility.
    /// </summary>
    public static void ApplyWindowVisibility(this Window window, bool showInTaskbar, bool hideFromAltTab)
    {
        window.ShowInTaskbar = showInTaskbar;

        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        extendedStyle = GetWindowVisibilityExtendedStyle(extendedStyle, showInTaskbar, hideFromAltTab);

        SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
    }

    /// <summary>
    /// Moves the window back to the top of the topmost band so it covers the taskbar again.
    /// </summary>
    /// <remarks>
    /// Topmost windows all share one band, so the shell raising itself leaves the clock buried inside it.
    /// Assigning <see cref="Window.Topmost"/> again wouldn't help because WPF only reapplies the z-order when the value changes.
    /// </remarks>
    public static void ReassertTopmost(this Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero)
            return;

        // SWP_NOZORDER is deliberately left out because moving within the band is the whole point, and SWP_NOACTIVATE keeps focus wherever the user left it.
        SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
    }

    /// <summary>
    /// Returns the extended style that matches the requested taskbar and Alt+Tab behavior.
    /// </summary>
    public static int GetWindowVisibilityExtendedStyle(int extendedStyle, bool showInTaskbar, bool hideFromAltTab)
    {
        extendedStyle &= ~(WS_EX_TOOLWINDOW | WS_EX_APPWINDOW);

        if (hideFromAltTab)
            return extendedStyle | WS_EX_TOOLWINDOW;

        return showInTaskbar
            ? extendedStyle | WS_EX_APPWINDOW
            : extendedStyle;
    }
}
