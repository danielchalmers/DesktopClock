using System;
using System.Globalization;

namespace DesktopClock.Tests;

public class HeightScaleConverterTests
{
    private static readonly CultureInfo TestCulture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(15)]
    [InlineData(48)]
    [InlineData(64)]
    [InlineData(665)]
    public void ConvertBack_AfterConvert_ShouldRoundTripHeight(int height)
    {
        var converter = new HeightScaleConverter();

        var logHeight = converter.Convert(height, typeof(double), null, TestCulture);
        var roundTrippedHeight = converter.ConvertBack(logHeight, typeof(int), null, TestCulture);

        Assert.Equal(height, roundTrippedHeight);
    }

    [Fact]
    public void Convert_ShouldReturnNaturalLogOfHeight()
    {
        var converter = new HeightScaleConverter();

        var result = converter.Convert(48, typeof(double), null, TestCulture);

        Assert.Equal(Math.Log(48), (double)result, 10);
    }

    [Fact]
    public void ConvertBack_ShouldReturnExponentiatedHeightAsInteger()
    {
        var converter = new HeightScaleConverter();

        var result = converter.ConvertBack(Math.Log(48), typeof(int), null, TestCulture);

        Assert.Equal(48, result);
    }

    [Fact]
    public void ScaleHeight_OneStepUp_ShouldMatchSliderConversion()
    {
        var converter = new HeightScaleConverter();
        const int initialHeight = 48;

        var logHeight = (double)converter.Convert(initialHeight, typeof(double), null, TestCulture);
        var scaledHeight = HeightScaleConverter.ScaleHeight(initialHeight, 1);
        var sliderHeight = (int)converter.ConvertBack(logHeight + 0.15, typeof(int), null, TestCulture);

        Assert.Equal(sliderHeight, scaledHeight);
    }

    [Fact]
    public void ScaleHeight_ShouldAdjustAndClamp()
    {
        var minHeight = (int)Math.Round(Math.Exp(HeightScaleConverter.MinSizeLog));
        var maxHeight = (int)Math.Round(Math.Exp(HeightScaleConverter.MaxSizeLog));

        Assert.Equal(65, HeightScaleConverter.ScaleHeight(48, 2));
        Assert.Equal(47, HeightScaleConverter.ScaleHeight(64, -2));
        Assert.Equal(maxHeight, HeightScaleConverter.ScaleHeight(48, 10_000));
        Assert.Equal(minHeight, HeightScaleConverter.ScaleHeight(48, -10_000));
    }

    [Fact]
    public void ProvideValue_ShouldReturnSameInstance()
    {
        var converter = new HeightScaleConverter();

        var providedValue = converter.ProvideValue(serviceProvider: null);

        Assert.Same(converter, providedValue);
    }
}
