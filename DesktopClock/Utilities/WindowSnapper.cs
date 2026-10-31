using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace DesktopClock;

/// <summary>
/// Snaps a window being dragged to the edges of its screen, and the taskbar and other docked bars.
/// </summary>
public static class WindowSnapper
{
    /// <summary>
    /// How close an edge has to be, in device-independent units, for the window to snap to it.
    /// </summary>
    public const double SnapDistance = 10;

    private const int WM_MOVING = 0x0216;
    private const int WM_ENTERSIZEMOVE = 0x0231;
    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    /// <summary>
    /// Makes the window snap while the user drags it.
    /// </summary>
    public static void SnapWhileDragging(this Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero || HwndSource.FromHwnd(hwnd) is not { } source)
            return;

        var snap = default(Vector);
        source.AddHook(SnapMoves);

        IntPtr SnapMoves(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_ENTERSIZEMOVE:
                    snap = default;
                    break;

                // Windows works out each new position from the last one it was given, so take back the last snap to follow the mouse again, or the window would stay stuck to an edge.
                case WM_MOVING:
                    var rect = Marshal.PtrToStructure<NativeRect>(lParam);
                    rect.Offset(-snap);

                    // The work area leaves out the taskbar and any other docked bars, so the window can rest against them as well as the screen edges behind them.
                    var monitor = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
                    GetMonitorInfo(MonitorFromRect(ref rect, MONITOR_DEFAULTTONEAREST), ref monitor);

                    snap = GetSnapOffset(rect.ToRect(), monitor.rcMonitor.ToRect(), monitor.rcWork.ToRect(), SnapDistance * VisualTreeHelper.GetDpi(window).DpiScaleX);
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
    /// The window only snaps inside the screen and its work area, so it never ends up off the screen where it couldn't be grabbed again.
    /// </remarks>
    public static Vector GetSnapOffset(Rect window, Rect screen, Rect workArea, double distance)
    {
        var x = double.PositiveInfinity;
        var y = double.PositiveInfinity;

        foreach (var area in new[] { workArea, screen })
        {
            x = Closest(x, area.Left - window.Left, area.Right - window.Right);
            y = Closest(y, area.Top - window.Top, area.Bottom - window.Bottom);
        }

        return new Vector(
            Math.Abs(x) <= distance ? x : 0,
            Math.Abs(y) <= distance ? y : 0);
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

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromRect(ref NativeRect rect, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

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
