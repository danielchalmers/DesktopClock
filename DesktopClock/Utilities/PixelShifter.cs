using System;
using System.Windows;

namespace DesktopClock.Utilities;

public class PixelShifter
{
    private readonly Random _random = new();
    private readonly Window _window;
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

    public PixelShifter(Window window)
    {
        _window = window;
    }

    /// <summary>
    /// Shifts the location of the window randomly to help prevent screen burn-in.
    /// </summary>
    public void ShiftWindow()
    {
        double CalculateShift(ref double totalShift)
        {
            var shift = _random.Next(-ShiftAmount, ShiftAmount + 1);
            var newTotalShift = totalShift + shift;

            if (Math.Abs(newTotalShift) <= MaxTotalShift)
            {
                totalShift = newTotalShift;
                return shift;
            }

            return 0;
        }

        var shiftX = CalculateShift(ref _totalShiftX);
        var shiftY = CalculateShift(ref _totalShiftY);

        _window.Left += shiftX;
        _window.Top += shiftY;
    }
}
