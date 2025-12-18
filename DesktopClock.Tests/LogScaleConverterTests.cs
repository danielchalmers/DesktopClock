using System;

namespace DesktopClock.Tests;

/// <summary>
/// Tests for the logarithmic scale conversion logic used by the height slider.
/// Since LogScaleConverter depends on WPF's MarkupExtension, we test the underlying math directly.
/// </summary>
public class LogScaleConverterTests
{
    // Replicate the Convert logic: Math.Log(value)
    private static double Convert(int value) => Math.Log(value);

    // Replicate the ConvertBack logic: Math.Exp(value)
    private static double ConvertBack(double value) => Math.Exp(value);

    [Theory]
    [InlineData(1, 0)] // ln(1) = 0
    [InlineData(10, 2.302585)] // ln(10) ≈ 2.302585
    [InlineData(100, 4.60517)] // ln(100) ≈ 4.60517
    [InlineData(48, 3.871201)] // ln(48) ≈ 3.871201 (default height)
    public void Convert_ShouldReturnLogOfValue(int input, double expectedApprox)
    {
        // Act
        var result = Convert(input);

        // Assert
        Assert.Equal(expectedApprox, result, 4);
    }

    [Theory]
    [InlineData(0, 1)] // e^0 = 1
    [InlineData(2.302585, 10)] // e^2.302585 ≈ 10
    [InlineData(4.60517, 100)] // e^4.60517 ≈ 100
    [InlineData(3.871201, 48)] // e^3.871201 ≈ 48
    public void ConvertBack_ShouldReturnExpOfValue(double input, int expectedApprox)
    {
        // Act
        var result = ConvertBack(input);

        // Assert
        Assert.Equal(expectedApprox, result, 0);
    }

    [Fact]
    public void Convert_And_ConvertBack_ShouldBeInverse()
    {
        // Arrange
        var originalValue = 48;

        // Act
        var logValue = Convert(originalValue);
        var backValue = ConvertBack(logValue);

        // Assert
        Assert.Equal(originalValue, (int)backValue);
    }

    [Theory]
    [InlineData(15)]   // Small height
    [InlineData(48)]   // Default height
    [InlineData(100)]  // Medium height
    [InlineData(200)]  // Large height
    [InlineData(665)]  // Max height
    public void RoundTrip_ShouldPreserveValue(int original)
    {
        // Act
        var logValue = Convert(original);
        var backValue = ConvertBack(logValue);

        // Assert
        Assert.Equal(original, (int)Math.Round(backValue));
    }

    [Fact]
    public void LogScale_ShouldCompressLargeValues()
    {
        // The logarithmic scale should make differences at high values smaller
        var diff1 = Convert(20) - Convert(10);   // Difference between 10-20
        var diff2 = Convert(200) - Convert(100); // Difference between 100-200

        // Both should be similar because log scale compresses large values
        Assert.Equal(diff1, diff2, 4);
    }
}
