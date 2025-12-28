using System;
using System.Windows;

namespace DesktopClock.Utilities;

public class PixelShifter
{
    private double _totalShiftX;
    private double _totalShiftY;
    private int _directionX = 1;
    private int _directionY = 1;
    private double _baseLeft;
    private double _baseTop;
    private bool _hasBasePosition;

    /// <summary>
    /// The number of pixels that will be shifted each time.
    /// </summary>
    public int PixelsPerShift { get; set; } = 1;

    /// <summary>
    /// The maximum amount of drift that can occur in each direction.
    /// </summary>
    public int MaxPixelOffset { get; set; } = 4;

    /// <summary>
    /// The ratio of the window size that determines the effective max drift.
    /// </summary>
    public double MaxPixelOffsetRatio { get; set; } = 0.1;

    /// <summary>
    /// The total amount shifted horizontally.
    /// </summary>
    public double TotalShiftX => _totalShiftX;

    /// <summary>
    /// The total amount shifted vertically.
    /// </summary>
    public double TotalShiftY => _totalShiftY;

    /// <summary>
    /// Returns an amount to shift horizontally by while staying within the specified bounds.
    /// </summary>
    public double ShiftX(double windowWidth)
    {
        var maxOffset = GetEffectiveMaxOffset(windowWidth);
        return ShiftAxis(ref _totalShiftX, ref _directionX, maxOffset);
    }

    /// <summary>
    /// Returns an amount to shift vertically by while staying within the specified bounds.
    /// </summary>
    public double ShiftY(double windowHeight)
    {
        var maxOffset = GetEffectiveMaxOffset(windowHeight);
        return ShiftAxis(ref _totalShiftY, ref _directionY, maxOffset);
    }

    /// <summary>
    /// Applies the current shift to the window's position.
    /// </summary>
    public void ApplyShift(Window window)
    {
        EnsureBasePosition(window.Left, window.Top);
        ShiftX(window.ActualWidth);
        ShiftY(window.ActualHeight);
        window.Left = _baseLeft + _totalShiftX;
        window.Top = _baseTop + _totalShiftY;
    }

    /// <summary>
    /// Clears any shift and restores the base position.
    /// </summary>
    public void ClearShift(Window window)
    {
        EnsureBasePosition(window.Left, window.Top);
        Reset();
        window.Left = _baseLeft;
        window.Top = _baseTop;
    }

    /// <summary>
    /// Restores the base position to persist the unshifted placement.
    /// </summary>
    public void RestoreBasePosition(Window window)
    {
        if (!_hasBasePosition)
        {
            UpdateBasePosition(window.Left, window.Top);
            return;
        }

        window.Left = _baseLeft;
        window.Top = _baseTop;
    }

    /// <summary>
    /// Updates the base position after a user move.
    /// </summary>
    public void UpdateBasePosition(Window window) => UpdateBasePosition(window.Left, window.Top);

    private void UpdateBasePosition(double currentLeft, double currentTop)
    {
        _baseLeft = currentLeft;
        _baseTop = currentTop;
        _hasBasePosition = true;
    }

    /// <summary>
    /// Adjusts the base position when right alignment nudges the left edge.
    /// </summary>
    public void AdjustForRightAlignedWidthChange(double widthChange)
    {
        if (!_hasBasePosition)
            return;

        _baseLeft -= widthChange;
    }

    /// <summary>
    /// Returns the effective max offset based on the window size ratio.
    /// </summary>
    public double GetEffectiveMaxOffset(double windowSize)
    {
        if (windowSize <= 0 || MaxPixelOffset <= 0)
            return 0;

        if (MaxPixelOffsetRatio <= 0)
            return MaxPixelOffset;

        var ratioOffset = (int)Math.Floor(windowSize * MaxPixelOffsetRatio);
        return Math.Min(MaxPixelOffset, Math.Max(0, ratioOffset));
    }

    /// <summary>
    /// Resets the shifter state back to zero.
    /// </summary>
    public void Reset()
    {
        _totalShiftX = 0;
        _totalShiftY = 0;
        _directionX = 1;
        _directionY = 1;
    }

    /// <summary>
    /// Ensures the base position is captured once.
    /// </summary>
    private void EnsureBasePosition(double currentLeft, double currentTop)
    {
        if (_hasBasePosition)
            return;

        _baseLeft = currentLeft;
        _baseTop = currentTop;
        _hasBasePosition = true;
    }

    /// <summary>
    /// Returns a capped amount to shift by.
    /// </summary>
    /// <param name="total">The current total amount of shift that has occurred.</param>
    /// <param name="direction">The direction of the next shift.</param>
    /// <param name="max">The bounds to stay within in respect to the total shift.</param>
    private double ShiftAxis(ref double total, ref int direction, double max)
    {
        if (PixelsPerShift <= 0 || max <= 0)
        {
            total = 0;
            direction = 1;
            return 0;
        }

        if (total >= max)
        {
            total = max;
            direction = -1;
        }
        else if (total <= -max)
        {
            total = -max;
            direction = 1;
        }

        var step = direction * PixelsPerShift;
        var proposed = total + step;

        if (proposed > max)
        {
            step = (int)(max - total);
            direction = -1;
        }
        else if (proposed < -max)
        {
            step = (int)(-max - total);
            direction = 1;
        }

        total += step;
        return step;
    }
}
