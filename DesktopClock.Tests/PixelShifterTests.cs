using System;
using DesktopClock.Utilities;

namespace DesktopClock.Tests;

public class PixelShifterTests
{
    [Theory]
    [InlineData(50, 0.1, 100, 5)]
    [InlineData(20, 0.5, 4, 4)]
    public void GetEffectiveMaxOffset_ShouldUseWindowSizeRatio(double windowSize, double ratio, int maxOffset, double expected)
    {
        var shifter = new PixelShifter
        {
            MaxPixelOffsetRatio = ratio,
            MaxPixelOffset = maxOffset,
        };

        var effective = shifter.GetEffectiveMaxOffset(windowSize);

        Assert.Equal(expected, effective);
    }

    [Fact]
    public void ShiftX_ShouldBounceDeterministicallyWithinBounds()
    {
        var shifter = new PixelShifter
        {
            PixelsPerShift = 2,
            MaxPixelOffset = 100,
            MaxPixelOffsetRatio = 0.1,
        };

        const double windowSize = 50;
        Assert.Equal(5d, shifter.GetEffectiveMaxOffset(windowSize));

        var expectedShifts = new double[] { 2, 2, 1, -2, -2, -2, -2, -2, 2, 2 };
        foreach (var expected in expectedShifts)
        {
            var shift = shifter.ShiftX(windowSize);
            Assert.Equal(expected, shift);
            Assert.InRange(Math.Abs(shifter.TotalShiftX), 0, 5);
        }
    }

    [Fact]
    public void ShiftY_ShouldBounceDeterministicallyWithinBounds()
    {
        var shifter = new PixelShifter
        {
            PixelsPerShift = 2,
            MaxPixelOffset = 100,
            MaxPixelOffsetRatio = 0.1,
        };

        const double windowSize = 50;
        Assert.Equal(5d, shifter.GetEffectiveMaxOffset(windowSize));

        var expectedShifts = new double[] { 2, 2, 1, -2, -2, -2, -2, -2, 2, 2 };
        foreach (var expected in expectedShifts)
        {
            var shift = shifter.ShiftY(windowSize);
            Assert.Equal(expected, shift);
            Assert.InRange(Math.Abs(shifter.TotalShiftY), 0, 5);
        }
    }

    [Theory]
    [InlineData(0, 50)]
    [InlineData(2, 0)]
    public void ShiftX_WhenDisabled_ReturnsZero(int pixelsPerShift, double windowSize)
    {
        var shifter = new PixelShifter
        {
            PixelsPerShift = pixelsPerShift,
            MaxPixelOffset = 10,
            MaxPixelOffsetRatio = 0.1,
        };

        var shift = shifter.ShiftX(windowSize);

        Assert.Equal(0, shift);
        Assert.Equal(0, shifter.TotalShiftX);
    }

    [Fact]
    public void DefaultValues_ShouldBeExpected()
    {
        var shifter = new PixelShifter();

        Assert.Equal(1, shifter.PixelsPerShift);
        Assert.Equal(4, shifter.MaxPixelOffset);
        Assert.Equal(0.1, shifter.MaxPixelOffsetRatio, 5);
    }
}
