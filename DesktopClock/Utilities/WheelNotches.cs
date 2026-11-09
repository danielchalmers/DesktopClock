using System.Windows.Input;

namespace DesktopClock;

/// <summary>
/// Adds up mouse wheel movement into whole notches, so touchpads and smooth-scrolling mice, which send a fraction of a notch at a time, count like a wheel.
/// </summary>
public sealed class WheelNotches
{
    private int _delta;

    /// <summary>
    /// Adds a wheel delta and returns the whole notches it completes, keeping the rest for next time.
    /// </summary>
    public int Add(int delta)
    {
        _delta += delta;
        var notches = _delta / Mouse.MouseWheelDeltaForOneLine;
        _delta %= Mouse.MouseWheelDeltaForOneLine;
        return notches;
    }
}
