using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using static DesktopClock.NativeMethods;

namespace DesktopClock;

/// <summary>
/// Keeps the clock behind other windows like part of the desktop, and keeps it in view when Windows shows the desktop, such as after Win+D (#46).
/// </summary>
/// <remarks>
/// Showing the desktop raises the window that holds the desktop icons above every other window except topmost ones, so the clock joins the topmost windows until the desktop goes back down.
/// A hidden window kept at the bottom tells when that is: the desktop is shown while the icons' window is above it. Rainmeter detects it the same way.
/// </remarks>
public sealed class DesktopPinner : IDisposable
{
    private readonly Window _window;
    private readonly IntPtr _hwnd;
    private readonly HwndSource _source;
    private readonly HwndSource _marker;
    private readonly DispatcherTimer _timer;
    private bool _overDesktop;
    private bool _moving;

    public DesktopPinner(Window window)
    {
        _window = window;
        _hwnd = new WindowInteropHelper(window).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source.AddHook(WndProc);

        _marker = new HwndSource(new HwndSourceParameters("DesktopClock desktop marker", 0, 0) { WindowStyle = WS_POPUP, ExtendedWindowStyle = WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE });
        SetWindowPos(_marker.Handle, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        MoveClock(HWND_BOTTOM);

        // Four times a second, so the clock is back moments after the desktop is shown.
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => Update(), window.Dispatcher);
    }

    public void Dispose()
    {
        _timer.Stop();
        if (!_source.IsDisposed)
            _source.RemoveHook(WndProc);
        _marker.Dispose();

        // Leave the topmost windows unless Always on top is what turned pinning off.
        if (_overDesktop && !_window.Topmost)
            MoveClock(HWND_NOTOPMOST);
    }

    private void Update()
    {
        if (!_window.IsVisible || _window.WindowState == WindowState.Minimized)
            return;

        if (IsAbove(GetDesktopIconsWindow(), _marker.Handle))
        {
            if (_overDesktop)
                return;

            // Join the topmost windows behind all the others, so the taskbar and anything else that stays on top still covers the clock.
            // Windows ignores the first request to become topmost after the clock has been clicked, unless it's asked to leave them first.
            _overDesktop = true;
            MoveClock(HWND_NOTOPMOST);
            MoveClock(HWND_TOPMOST);
            var backmostTopmost = GetBackmostTopmostWindow();
            if (backmostTopmost != IntPtr.Zero)
                MoveClock(backmostTopmost);
        }
        else if (_overDesktop)
        {
            // Moving a topmost window to the bottom also takes it out of the topmost windows, in one step so it never shows above the others.
            _overDesktop = false;
            MoveClock(HWND_BOTTOM);
        }
    }

    private void MoveClock(IntPtr insertAfter)
    {
        _moving = true;
        SetWindowPos(_hwnd, insertAfter, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
        _moving = false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Stay put when the clock is clicked or anything else tries to bring it forward.
        if (msg == WM_WINDOWPOSCHANGING && !_moving && !_window.Topmost)
        {
            var position = Marshal.PtrToStructure<WindowPos>(lParam);
            position.Flags |= SWP_NOZORDER;
            Marshal.StructureToPtr(position, lParam, false);
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// Returns the window that holds the desktop icons: Program Manager since Windows 11 24H2, or a WorkerW window before it while the desktop is shown.
    /// </summary>
    private static IntPtr GetDesktopIconsWindow()
    {
        var icons = FindWindowEx(GetShellWindow(), IntPtr.Zero, "SHELLDLL_DefView", null);
        for (var worker = IntPtr.Zero; icons == IntPtr.Zero && (worker = FindWindowEx(IntPtr.Zero, worker, "WorkerW", null)) != IntPtr.Zero;)
            icons = FindWindowEx(worker, IntPtr.Zero, "SHELLDLL_DefView", null);

        return icons == IntPtr.Zero ? IntPtr.Zero : GetAncestor(icons, GA_ROOT);
    }

    private static bool IsAbove(IntPtr upper, IntPtr lower)
    {
        if (upper == IntPtr.Zero)
            return false;

        for (var window = GetWindow(upper, GW_HWNDNEXT); window != IntPtr.Zero; window = GetWindow(window, GW_HWNDNEXT))
        {
            if (window == lower)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the bottom window of the topmost ones above the desktop icons.
    /// </summary>
    private IntPtr GetBackmostTopmostWindow()
    {
        for (var window = GetWindow(GetDesktopIconsWindow(), GW_HWNDPREV); window != IntPtr.Zero; window = GetWindow(window, GW_HWNDPREV))
        {
            if (window != _hwnd && (GetWindowLong(window, GWL_EXSTYLE) & WS_EX_TOPMOST) != 0)
                return window;
        }

        return IntPtr.Zero;
    }

    private const int WM_WINDOWPOSCHANGING = 0x0046;
    private const int GWL_EXSTYLE = -20;
    private const int WS_POPUP = unchecked((int)0x80000000);
    private const int WS_EX_TOPMOST = 0x00000008;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_NOACTIVATE = 0x08000000;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint GW_HWNDNEXT = 2;
    private const uint GW_HWNDPREV = 3;
    private const uint GA_ROOT = 2;
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);
    private static readonly IntPtr HWND_BOTTOM = new(1);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPos
    {
        public IntPtr Hwnd;
        public IntPtr InsertAfter;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);
}
