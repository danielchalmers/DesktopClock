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
    public int PixelsPerShift { get; set; } = 1;

    /// <summary>
    /// The maximum amount of drift that can occur in each direction.
    /// </summary>
    public int MaxPixelOffset { get; set; } = 4;

    public double ShiftX()
    {
        double pixelsToMoveBy = GetRandomShift();
        pixelsToMoveBy = GetFinalShiftAmount(_totalShiftX, pixelsToMoveBy, MaxPixelOffset);
        _totalShiftX += pixelsToMoveBy;
        return pixelsToMoveBy;
    }

    public double ShiftY()
    {
        double pixelsToMoveBy = GetRandomShift();
        pixelsToMoveBy = GetFinalShiftAmount(_totalShiftY, pixelsToMoveBy, MaxPixelOffset);
        _totalShiftY += pixelsToMoveBy;
        return pixelsToMoveBy;
    }

    private int GetRandomShift() => _random.Next(-PixelsPerShift, PixelsPerShift + 1);

    private double GetFinalShiftAmount(double current, double offset, double max)
    {
        var newTotal = current + offset;

        if (newTotal > max)
        {
            return max - current;
        }
        else if (newTotal < -max)
        {
            return -max - current;
        }
        else
        {
            return offset;
        }
    }
}
