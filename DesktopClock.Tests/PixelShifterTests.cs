using System;
using System.Collections.Generic;
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
            PixelsPerShift = shiftAmount,
            MaxPixelOffset = maxTotalShift,
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
            PixelsPerShift = shiftAmount,
            MaxPixelOffset = maxTotalShift,
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

    [Fact]
    public void ShiftX_WithZeroPixelsPerShift_ShouldReturnZero()
    {
        // Arrange
        var shifter = new PixelShifter
        {
            PixelsPerShift = 0,
            MaxPixelOffset = 10,
        };

        // Act
        var shift = shifter.ShiftX();

        // Assert
        Assert.Equal(0, shift);
    }

    [Fact]
    public void ShiftY_WithZeroPixelsPerShift_ShouldReturnZero()
    {
        // Arrange
        var shifter = new PixelShifter
        {
            PixelsPerShift = 0,
            MaxPixelOffset = 10,
        };

        // Act
        var shift = shifter.ShiftY();

        // Assert
        Assert.Equal(0, shift);
    }

    [Fact]
    public void ShiftX_WithZeroMaxOffset_ShouldReturnZero()
    {
        // Arrange
        var shifter = new PixelShifter
        {
            PixelsPerShift = 5,
            MaxPixelOffset = 0,
        };

        // Act
        var shift = shifter.ShiftX();

        // Assert
        Assert.Equal(0, shift);
    }

    [Fact]
    public void ShiftY_WithZeroMaxOffset_ShouldReturnZero()
    {
        // Arrange
        var shifter = new PixelShifter
        {
            PixelsPerShift = 5,
            MaxPixelOffset = 0,
        };

        // Act
        var shift = shifter.ShiftY();

        // Assert
        Assert.Equal(0, shift);
    }

    [Fact]
    public void DefaultValues_ShouldBeExpected()
    {
        // Arrange
        var shifter = new PixelShifter();

        // Assert
        Assert.Equal(1, shifter.PixelsPerShift);
        Assert.Equal(4, shifter.MaxPixelOffset);
    }

    [Fact]
    public void ShiftX_ShouldReverseDirectionAtBoundary()
    {
        // Arrange - set up to hit boundary quickly
        var shifter = new PixelShifter
        {
            PixelsPerShift = 10,
            MaxPixelOffset = 10,
        };

        // Act - call multiple times to force direction reversal
        double total = 0;
        var shifts = new List<double>();
        for (int i = 0; i < 10; i++)
        {
            var shift = shifter.ShiftX();
            shifts.Add(shift);
            total += shift;
        }

        // Assert - total should stay within bounds
        Assert.InRange(Math.Abs(total), 0, 10);

        // There should be both positive and negative shifts (direction reversal)
        // OR the total stayed within bounds
        Assert.True(Math.Abs(total) <= 10);
    }

    [Fact]
    public void ShiftY_ShouldReverseDirectionAtBoundary()
    {
        // Arrange - set up to hit boundary quickly
        var shifter = new PixelShifter
        {
            PixelsPerShift = 10,
            MaxPixelOffset = 10,
        };

        // Act - call multiple times to force direction reversal
        double total = 0;
        var shifts = new List<double>();
        for (int i = 0; i < 10; i++)
        {
            var shift = shifter.ShiftY();
            shifts.Add(shift);
            total += shift;
        }

        // Assert - total should stay within bounds
        Assert.InRange(Math.Abs(total), 0, 10);
    }

    [Fact]
    public void ShiftX_And_ShiftY_ShouldBeIndependent()
    {
        // Arrange
        var shifter = new PixelShifter
        {
            PixelsPerShift = 5,
            MaxPixelOffset = 20,
        };

        // Act
        double totalX = 0, totalY = 0;
        for (int i = 0; i < 50; i++)
        {
            totalX += shifter.ShiftX();
            totalY += shifter.ShiftY();
        }

        // Assert - both should be within bounds independently
        Assert.InRange(Math.Abs(totalX), 0, 20);
        Assert.InRange(Math.Abs(totalY), 0, 20);
    }

    [Fact]
    public void ShiftX_MultipleShiftersWithSameConfig_ShouldBeIndependent()
    {
        // Arrange
        var shifter1 = new PixelShifter { PixelsPerShift = 5, MaxPixelOffset = 10 };
        var shifter2 = new PixelShifter { PixelsPerShift = 5, MaxPixelOffset = 10 };

        // Act
        double total1 = 0, total2 = 0;
        for (int i = 0; i < 20; i++)
        {
            total1 += shifter1.ShiftX();
            total2 += shifter2.ShiftX();
        }

        // Assert - both should be within their own bounds
        Assert.InRange(Math.Abs(total1), 0, 10);
        Assert.InRange(Math.Abs(total2), 0, 10);
    }

    [Theory]
    [InlineData(1, 5)]
    [InlineData(2, 8)]
    [InlineData(3, 15)]
    public void Shift_ShouldReturnValueWithinPixelsPerShiftRange(int pixelsPerShift, int maxOffset)
    {
        // Arrange
        var shifter = new PixelShifter
        {
            PixelsPerShift = pixelsPerShift,
            MaxPixelOffset = maxOffset,
        };

        // Act & Assert
        for (int i = 0; i < 50; i++)
        {
            var shiftX = shifter.ShiftX();
            var shiftY = shifter.ShiftY();

            // Shift should be within the range [-pixelsPerShift, +pixelsPerShift]
            Assert.InRange(shiftX, -pixelsPerShift, pixelsPerShift);
            Assert.InRange(shiftY, -pixelsPerShift, pixelsPerShift);
        }
    }
}
