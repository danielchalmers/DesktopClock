namespace DesktopClock.Tests;

public class WheelNotchesTests
{
    [Theory]
    [InlineData(120, 1)]
    [InlineData(-120, -1)]
    [InlineData(360, 3)]
    public void Add_CountsWholeNotchesFromAWheel(int delta, int expected)
    {
        Assert.Equal(expected, new WheelNotches().Add(delta));
    }

    [Fact]
    public void Add_KeepsTheRestOfANotchForNextTime()
    {
        var wheel = new WheelNotches();

        Assert.Equal(0, wheel.Add(100));
        Assert.Equal(1, wheel.Add(40));
        Assert.Equal(1, wheel.Add(100));
    }

    [Fact]
    public void Add_ScrollingBackCancelsOut()
    {
        var wheel = new WheelNotches();

        Assert.Equal(0, wheel.Add(60));
        Assert.Equal(0, wheel.Add(-60));
        Assert.Equal(0, wheel.Add(60));
    }

    [Fact]
    public void TouchpadScroll_ResizesLikeOneNotch()
    {
        // A precision touchpad sends many small deltas, here twelve of a twelfth of a notch.
        var wheel = new WheelNotches();
        var height = 48;
        for (var i = 0; i < 12; i++)
        {
            var notches = wheel.Add(10);
            if (notches != 0)
                height = HeightScaleConverter.ScaleHeight(height, notches);
        }

        Assert.Equal(HeightScaleConverter.ScaleHeight(48, 1), height);
        Assert.NotEqual(48, height);
    }
}
