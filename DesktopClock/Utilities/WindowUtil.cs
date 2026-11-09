using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WpfWindowPlacement;
using static DesktopClock.NativeMethods;

namespace DesktopClock;

public static class WindowUtil
{
    /// <summary>
    /// How far one arrow key press moves the window.
    /// </summary>
    public const double NudgeStep = 1;

    /// <summary>
    /// How far an arrow key press moves the window while Shift is held.
    /// </summary>
    public const double LargeNudgeStep = 10;

    /// <summary>
    /// Returns how far an arrow key should move the window, or zero for other keys.
    /// </summary>
    public static Vector GetKeyboardNudge(Key key, ModifierKeys modifiers)
    {
        var step = modifiers == ModifierKeys.Shift ? LargeNudgeStep : NudgeStep;

        return key switch
        {
            Key.Left => new Vector(-step, 0),
            Key.Right => new Vector(step, 0),
            Key.Up => new Vector(0, -step),
            Key.Down => new Vector(0, step),
            _ => default,
        };
    }

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
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    private const int ASFW_ANY = -1;

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int processId);

    /// <summary>
    /// Lets another app bring its window to the front, which Windows otherwise only allows the app the user just started.
    /// </summary>
    public static void AllowAnyAppToComeToFront() => AllowSetForegroundWindow(ASFW_ANY);

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
    /// Moves the window back above a taskbar that the shell raised over it.
    /// </summary>
    /// <remarks>
    /// Topmost windows all share one band, so the shell raising itself leaves the clock buried inside it.
    /// Assigning <see cref="Window.Topmost"/> again wouldn't help because WPF only reapplies the z-order when the value changes.
    /// </remarks>
    public static void ReassertTopmost(this Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || !GetWindowRect(hwnd, out var clockRect))
            return;

        // Only the few topmost windows above the clock are walked, and only a taskbar that actually covers it counts, so menus and other always-on-top windows keep their place.
        for (var above = GetWindow(hwnd, GW_HWNDPREV); above != IntPtr.Zero; above = GetWindow(above, GW_HWNDPREV))
        {
            if (!IsTaskbar(above) || !IsWindowVisible(above) || !GetWindowRect(above, out var taskbarRect) || !Overlaps(clockRect, taskbarRect))
                continue;

            // Slot in just above the taskbar rather than at the top of the band, so its jump lists and anything else above it stay in front.
            // SWP_NOACTIVATE keeps focus wherever the user left it.
            var insertAfter = GetWindow(above, GW_HWNDPREV);
            SetWindowPos(hwnd, insertAfter == IntPtr.Zero ? HWND_TOPMOST : insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            return;
        }

        static bool IsTaskbar(IntPtr hwnd)
        {
            var className = new StringBuilder(32);
            GetClassName(hwnd, className, className.Capacity);
            return className.ToString() is "Shell_TrayWnd" or "Shell_SecondaryTrayWnd";
        }

        static bool Overlaps(NativeRect a, NativeRect b) =>
            a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
    }

    /// <summary>
    /// Restores the saved placement, then undoes the move Windows makes when it pushes a window that sits entirely inside the taskbar up into the work area (#157).
    /// </summary>
    public static void RestorePlacement(this Window window, WindowPlacement placement)
    {
        window.SetPlacement(placement);

        var hwnd = new WindowInteropHelper(window).Handle;
        if (placement.Equals(default(WindowPlacement)) || !GetWindowRect(hwnd, out var rect))
            return;

        // Both placements use the same coordinates, so their difference is how far Windows moved the window.
        var saved = placement.NormalBounds;
        var restored = window.GetPlacement().NormalBounds;
        var target = Offset(rect, saved.Left - restored.Left, saved.Top - restored.Top);

        // Keep Windows' move if the saved spot isn't on any monitor anymore, such as after unplugging one.
        if (!IsOnAnyMonitor(target))
            target = rect;

        // Windows doesn't bring a window hidden from Alt+Tab back onto a monitor, so do it here.
        if (!IsOnAnyMonitor(target))
            target = MoveIntoNearestWorkArea(target);

        if (target.Left != rect.Left || target.Top != rect.Top)
            SetWindowPos(hwnd, IntPtr.Zero, target.Left, target.Top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);

        static NativeRect Offset(NativeRect rect, int dx, int dy) =>
            new() { Left = rect.Left + dx, Top = rect.Top + dy, Right = rect.Right + dx, Bottom = rect.Bottom + dy };

        static bool IsOnAnyMonitor(NativeRect rect) =>
            MonitorFromRect(ref rect, MONITOR_DEFAULTTONULL) != IntPtr.Zero;

        static NativeRect MoveIntoNearestWorkArea(NativeRect rect)
        {
            var monitor = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(MonitorFromRect(ref rect, MONITOR_DEFAULTTONEAREST), ref monitor))
                return rect;

            var work = monitor.rcWork;
            var left = Math.Max(work.Left, Math.Min(rect.Left, work.Right - (rect.Right - rect.Left)));
            var top = Math.Max(work.Top, Math.Min(rect.Top, work.Bottom - (rect.Bottom - rect.Top)));
            return Offset(rect, left - rect.Left, top - rect.Top);
        }
    }

    private const uint MONITOR_DEFAULTTONULL = 0;
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    private const uint GW_HWNDPREV = 3;

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
