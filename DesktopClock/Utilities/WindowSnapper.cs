using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopClock;

/// <summary>
/// Snaps a window being dragged to the edges of its screen, the taskbar and other docked bars, and other clocks.
/// </summary>
public static class WindowSnapper
{
    /// <summary>
    /// How close an edge has to be, in device-independent units, for the window to snap to it.
    /// </summary>
    public const double SnapDistance = 10;

    private const int WM_MOVING = 0x0216;
    private const int WM_ENTERSIZEMOVE = 0x0231;
    private const int WM_EXITSIZEMOVE = 0x0232;
    private const int DWMWA_CLOAKED = 14;
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    /// <summary>
    /// Makes the window snap while the user drags it.
    /// </summary>
    public static void SnapWhileDragging(this Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || HwndSource.FromHwnd(hwnd) is not { } source)
            return;

        List<Rect> clocks = null;
        var snap = default(Vector);
        source.AddHook(SnapMoves);

        IntPtr SnapMoves(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                // Find the other clocks once per drag, since they don't move while the mouse button is held.
                case WM_ENTERSIZEMOVE:
                    clocks = GetOtherClocks(hwnd, window.Title);
                    snap = default;
                    break;

                case WM_EXITSIZEMOVE:
                    clocks = null;
                    break;

                // Windows works out each new position from the last one it was given, so take back the last snap to follow the mouse again, or the window would stay stuck to an edge.
                case WM_MOVING when clocks != null:
                    var rect = Marshal.PtrToStructure<NativeRect>(lParam);
                    rect.Offset(-snap);

                    // The work area leaves out the taskbar and any other docked bars, so the window can rest against them as well as the screen edges behind them.
                    var monitor = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
                    GetMonitorInfo(MonitorFromRect(ref rect, MONITOR_DEFAULTTONEAREST), ref monitor);

                    snap = GetSnapOffset(rect.ToRect(), monitor.rcMonitor.ToRect(), monitor.rcWork.ToRect(), clocks, SnapDistance * VisualTreeHelper.GetDpi(window).DpiScaleX);
                    rect.Offset(snap);
                    Marshal.StructureToPtr(rect, lParam, false);
                    handled = true;
                    return new IntPtr(1);
            }

            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Returns how far to move the window so its closest edge on each axis lines up with an edge within the distance, or zero on an axis with none.
    /// </summary>
    /// <remarks>
    /// The window snaps inside the screen and its work area, and beside or in line with the clocks next to it.
    /// </remarks>
    public static Vector GetSnapOffset(Rect window, Rect screen, Rect workArea, IEnumerable<Rect> clocks, double distance)
    {
        var x = double.PositiveInfinity;
        var y = double.PositiveInfinity;

        foreach (var area in new[] { workArea, screen })
        {
            x = Closest(x, area.Left - window.Left, area.Right - window.Right);
            y = Closest(y, area.Top - window.Top, area.Bottom - window.Bottom);
        }

        foreach (var clock in clocks)
        {
            // Only clocks alongside the window count, so one far away on the same line doesn't pull it.
            if (window.Top <= clock.Bottom + distance && window.Bottom >= clock.Top - distance)
                x = Closest(x, clock.Left - window.Left, clock.Right - window.Left, clock.Left - window.Right, clock.Right - window.Right);

            if (window.Left <= clock.Right + distance && window.Right >= clock.Left - distance)
                y = Closest(y, clock.Top - window.Top, clock.Bottom - window.Top, clock.Top - window.Bottom, clock.Bottom - window.Bottom);
        }

        // Never snap the window off the screen, like beside a clock at its edge, where it couldn't be grabbed again.
        return new Vector(
            Math.Abs(x) <= distance && window.Left + x < screen.Right && window.Right + x > screen.Left ? x : 0,
            Math.Abs(y) <= distance && window.Top + y < screen.Bottom && window.Bottom + y > screen.Top ? y : 0);
    }

    private static double Closest(double current, params double[] offsets)
    {
        foreach (var offset in offsets)
        {
            if (Math.Abs(offset) < Math.Abs(current))
                current = offset;
        }

        return current;
    }

    /// <summary>
    /// Returns where every other clock that's showing is.
    /// </summary>
    private static List<Rect> GetOtherClocks(IntPtr self, string title)
    {
        var clocks = new List<Rect>();

        // Clocks made with "New clock" run as their own apps, and may be older versions, but they all share the main window's title.
        for (var hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, null, title); hwnd != IntPtr.Zero; hwnd = FindWindowEx(IntPtr.Zero, hwnd, null, title))
        {
            if (hwnd != self && IsShowingClock(hwnd) && GetWindowRect(hwnd, out var rect))
                clocks.Add(rect.ToRect());
        }

        return clocks;
    }

    private static bool IsShowingClock(IntPtr hwnd)
    {
        if (!IsWindowVisible(hwnd) || IsIconic(hwnd))
            return false;

        // Rule out other windows with the same title, like the clock's own message boxes or a File Explorer window open on a folder named after the app.
        var className = new StringBuilder(16);
        GetClassName(hwnd, className, className.Capacity);
        if (!className.ToString().StartsWith("HwndWrapper[", StringComparison.Ordinal))
            return false;

        // Windows on other virtual desktops still count as visible, but they're cloaked.
        return DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, out var cloaked, sizeof(int)) != 0 || cloaked == 0;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref NativeRect rect, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr parent, IntPtr childAfter, string className, string windowName);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out int value, int size);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public readonly Rect ToRect() => new(Left, Top, Right - Left, Bottom - Top);

        public void Offset(Vector offset)
        {
            Left += (int)offset.X;
            Top += (int)offset.Y;
            Right += (int)offset.X;
            Bottom += (int)offset.Y;
        }
    }
}
