using System;
using DesktopClock.Utilities;

namespace DesktopClock.Tests;

public class PixelShifterTests
{
    [Theory]
    [InlineData(5, 10)] // Evenly divisible.
    [InlineData(3, 10)] // Not evenly divisible.
    [InlineData(10, 5)] // Amount is larger than total.
    public void ShiftX_ShouldNotExceedMaxTotalShift(int shiftAmount, int maxTotalShift)
    {
        var shifter = new PixelShifter
        {
            ShiftAmount = shiftAmount,
            MaxTotalShift = maxTotalShift,
        };

        double totalShiftX = 0;

        // Test 100 times because it's random.
        for (var i = 0; i < 100; i++)
        {
            var shift = shifter.ShiftX();
            totalShiftX += shift;

            Assert.InRange(Math.Abs(totalShiftX), 0, maxTotalShift);
        }
    }

    [Theory]
    [InlineData(5, 10)] // Evenly divisible.
    [InlineData(3, 10)] // Not evenly divisible.
    [InlineData(10, 5)] // Amount is larger than total.
    public void ShiftY_ShouldNotExceedMaxTotalShift(int shiftAmount, int maxTotalShift)
    {
        var shifter = new PixelShifter
        {
            ShiftAmount = shiftAmount,
            MaxTotalShift = maxTotalShift,
        };

        double totalShiftY = 0;

        // Test 100 times because it's random.
        for (var i = 0; i < 100; i++)
        {
            var shift = shifter.ShiftY();
            totalShiftY += shift;

            Assert.InRange(Math.Abs(totalShiftY), 0, maxTotalShift);
        }
    }
}
