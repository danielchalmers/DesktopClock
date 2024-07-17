using System;

namespace DesktopClock.Utilities;

public class PixelShifter
{
    private readonly Random _random = new();
    private double _totalShiftX;
    private double _totalShiftY;

    /// <summary>
    /// The number of pixels that will be shifted each time.
    /// </summary>
    public int ShiftAmount { get; set; } = 2;

    /// <summary>
    /// The maximum amount of drift that can occur in each direction.
    /// </summary>
    public int MaxTotalShift { get; set; } = 4;

    public double ShiftX()
    {
        var shift = _random.Next(-ShiftAmount, ShiftAmount + 1);
        var newTotalShiftX = _totalShiftX + shift;

        if (Math.Abs(newTotalShiftX) <= MaxTotalShift)
        {
            _totalShiftX = newTotalShiftX;
            return shift;
        }

        return 0;
    }

    public double ShiftY()
    {
        var shift = _random.Next(-ShiftAmount, ShiftAmount + 1);
        var newTotalShiftY = _totalShiftY + shift;

        if (Math.Abs(newTotalShiftY) <= MaxTotalShift)
        {
            _totalShiftY = newTotalShiftY;
            return shift;
        }

        return 0;
    }
}
