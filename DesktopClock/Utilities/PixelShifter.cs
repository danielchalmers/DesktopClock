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

    /// <summary>
    /// Returns an amount to shift horizontally by while staying within the specified bounds.
    /// </summary>
    public double ShiftX()
    {
        double pixelsToMoveBy = GetRandomShift();
        pixelsToMoveBy = GetFinalShiftAmount(_totalShiftX, pixelsToMoveBy, MaxPixelOffset);
        _totalShiftX += pixelsToMoveBy;
        return pixelsToMoveBy;
    }

    /// <summary>
    /// Returns an amount to shift vertically by while staying within the specified bounds.
    /// </summary>
    public double ShiftY()
    {
        double pixelsToMoveBy = GetRandomShift();
        pixelsToMoveBy = GetFinalShiftAmount(_totalShiftY, pixelsToMoveBy, MaxPixelOffset);
        _totalShiftY += pixelsToMoveBy;
        return pixelsToMoveBy;
    }

    /// <summary>
    /// Returns a random amount to shift by within the specified amount.
    /// </summary>
    private int GetRandomShift() => _random.Next(-PixelsPerShift, PixelsPerShift + 1);

    /// <summary>
    /// Returns a capped amount to shift by.
    /// </summary>
    /// <param name="current">The current total amount of shift that has occurred.</param>
    /// <param name="offset">The proposed amount to shift by this time.</param>
    /// <param name="max">The bounds to stay within in respect to the total shift.</param>
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
