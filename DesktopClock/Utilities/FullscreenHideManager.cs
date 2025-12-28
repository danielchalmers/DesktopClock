using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using DesktopClock.Properties;

namespace DesktopClock;

public sealed class FullscreenHideManager
{
    private const int FullscreenEdgeTolerance = 10;
    private const int MaxClassNameLength = 256;
    private const uint MonitorDefaultToNearest = 0x00000002;

    private readonly Window _window;
    private bool _hiddenForFullscreen;

    public FullscreenHideManager(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public void Update()
    {
        if (!Settings.Default.HideWhenFullscreen)
        {
            Restore();
            return;
        }

        if (!_window.IsLoaded || _window.WindowState == WindowState.Minimized)
            return;

        if (IsActiveWindowFullscreenOnSameMonitor())
            Hide();
        else
            Restore();
    }

    public void TryUpdate()
    {
        if (!Settings.Default.HideWhenFullscreen)
            return;

        if (_window.Dispatcher.CheckAccess())
            Update();
        else
            _window.Dispatcher.Invoke(Update);
    }

    private void Hide()
    {
        if (_hiddenForFullscreen)
            return;

        _hiddenForFullscreen = true;
        _window.Hide();
    }

    private void Restore()
    {
        if (!_hiddenForFullscreen || _window.WindowState == WindowState.Minimized)
            return;

        _hiddenForFullscreen = false;

        var wasShowActivated = _window.ShowActivated;
        _window.ShowActivated = false;
        _window.Show();
        _window.ShowActivated = wasShowActivated;
    }

    private bool IsActiveWindowFullscreenOnSameMonitor()
    {
        var clockHwnd = new WindowInteropHelper(_window).Handle;
        if (clockHwnd == IntPtr.Zero)
            return false;

        var foregroundHwnd = GetForegroundWindow();
        if (foregroundHwnd == IntPtr.Zero || foregroundHwnd == clockHwnd)
            return false;

        if (!IsWindowVisible(foregroundHwnd) || IsShellWindow(foregroundHwnd))
            return false;

        var clockMonitor = MonitorFromWindow(clockHwnd, MonitorDefaultToNearest);
        var foregroundMonitor = MonitorFromWindow(foregroundHwnd, MonitorDefaultToNearest);
        if (clockMonitor == IntPtr.Zero || foregroundMonitor == IntPtr.Zero || clockMonitor != foregroundMonitor)
            return false;

        if (!GetWindowRect(foregroundHwnd, out var rect))
            return false;

        if (rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            return false;

        if (!TryGetMonitorRect(foregroundMonitor, out var monitorRect))
            return false;

        return RectCoversMonitor(rect, monitorRect);
    }

    private static bool RectCoversMonitor(NativeRect rect, NativeRect monitorRect) =>
        Math.Abs(rect.Left - monitorRect.Left) <= FullscreenEdgeTolerance &&
        Math.Abs(rect.Top - monitorRect.Top) <= FullscreenEdgeTolerance &&
        Math.Abs(rect.Right - monitorRect.Right) <= FullscreenEdgeTolerance &&
        Math.Abs(rect.Bottom - monitorRect.Bottom) <= FullscreenEdgeTolerance;

    private static bool TryGetMonitorRect(IntPtr monitor, out NativeRect rect)
    {
        var info = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info))
        {
            rect = default;
            return false;
        }

        rect = info.rcMonitor;
        return true;
    }

    private static bool IsShellWindow(IntPtr hwnd)
    {
        var className = GetWindowClassName(hwnd);

        return string.Equals(className, "Progman", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "WorkerW", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Shell_TrayWnd", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(className, "Shell_SecondaryTrayWnd", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetWindowClassName(IntPtr hwnd)
    {
        var className = new StringBuilder(MaxClassNameLength);
        return GetClassName(hwnd, className, className.Capacity) > 0
            ? className.ToString()
            : string.Empty;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hwnd, StringBuilder className, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo info);

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
    }
}
