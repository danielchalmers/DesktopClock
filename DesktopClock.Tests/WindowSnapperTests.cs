using System.Windows;

namespace DesktopClock.Tests;

public class WindowSnapperTests
{
    private static readonly Rect Screen = new(0, 0, 1920, 1080);
    private static readonly Rect WorkArea = new(0, 0, 1920, 1032);

    [Theory]
    [InlineData(1412, 979, 1420, 984)] // Bottom-right of the work area, resting on the taskbar.
    [InlineData(1414, 1027, 1420, 1032)] // Bottom-right of the screen, over the taskbar.
    [InlineData(1416, 1034, 1420, 1032)] // Bottom-right of the screen, from slightly past it.
    [InlineData(6, -4, 0, 0)] // Top-left of the screen.
    [InlineData(-8, 500, 0, 500)] // Left edge only.
    [InlineData(10, 500, 0, 500)] // Exactly the snap distance away.
    public void GetSnapOffset_NearScreenEdges_SnapsFlush(double left, double top, double expectedLeft, double expectedTop)
    {
        var offset = WindowSnapper.GetSnapOffset(new Rect(left, top, 500, 48), Screen, WorkArea, [], 10);

        Assert.Equal(expectedLeft, left + offset.X);
        Assert.Equal(expectedTop, top + offset.Y);
    }

    [Fact]
    public void GetSnapOffset_FarFromEdges_DoesNotMove()
    {
        var offset = WindowSnapper.GetSnapOffset(new Rect(300, 200, 500, 48), Screen, WorkArea, [], 10);

        Assert.Equal(default, offset);
    }

    [Fact]
    public void GetSnapOffset_JustOutsideDistance_DoesNotMove()
    {
        var offset = WindowSnapper.GetSnapOffset(new Rect(11, 200, 500, 48), Screen, WorkArea, [], 10);

        Assert.Equal(default, offset);
    }

    [Fact]
    public void GetSnapOffset_ScreenLeftOfTheMainOne_SnapsToItsEdges()
    {
        var screen = new Rect(-1920, 0, 1920, 1080);

        var offset = WindowSnapper.GetSnapOffset(new Rect(-1914, 5, 500, 48), screen, screen, [], 10);

        Assert.Equal(new Vector(-6, -5), offset);
    }

    [Theory]
    [InlineData(1914, 500)] // Its left edge is near the screen's right edge.
    [InlineData(300, 1075)] // Its top edge is near the screen's bottom edge.
    [InlineData(-494, -43)] // Its bottom-right corner is near the screen's top-left corner.
    public void GetSnapOffset_FarEdgeNearScreenEdge_DoesNotSnapOffScreen(double left, double top)
    {
        var offset = WindowSnapper.GetSnapOffset(new Rect(left, top, 500, 48), Screen, WorkArea, [], 10);

        Assert.Equal(default, offset);
    }

    [Theory]
    [InlineData(1550, 300, 1914, 303, 0, -3)] // Beside a clock at the right edge: lines up the tops but doesn't go off the right.
    [InlineData(700, 0, 704, -43, -4, 0)] // Above a clock at the top edge: lines up the left edges but doesn't go off the top.
    public void GetSnapOffset_NextToClockAtScreenEdge_DoesNotSnapOffScreen(double clockLeft, double clockTop, double left, double top, double expectedX, double expectedY)
    {
        var clock = new Rect(clockLeft, clockTop, 370, 36);

        var offset = WindowSnapper.GetSnapOffset(new Rect(left, top, 500, 48), Screen, WorkArea, [clock], 10);

        Assert.Equal(new Vector(expectedX, expectedY), offset);
    }

    [Fact]
    public void GetSnapOffset_ClockAndScreenEdgesInRange_PicksTheCloser()
    {
        // The screen's left edge is 7 away and the clock's is 6 away.
        var clock = new Rect(13, 300, 200, 36);

        var offset = WindowSnapper.GetSnapOffset(new Rect(7, 340, 500, 48), Screen, WorkArea, [clock], 10);

        Assert.Equal(new Vector(6, -4), offset);
    }

    [Fact]
    public void GetSnapOffset_UnderAnotherClock_StacksAndLinesUpLeftEdges()
    {
        var clock = new Rect(700, 500, 370, 36);

        var offset = WindowSnapper.GetSnapOffset(new Rect(704, 541, 500, 48), Screen, WorkArea, [clock], 10);

        Assert.Equal(new Vector(-4, -5), offset);
    }

    [Fact]
    public void GetSnapOffset_BesideAnotherClock_TouchesAndLinesUpTops()
    {
        var clock = new Rect(700, 500, 370, 36);

        var offset = WindowSnapper.GetSnapOffset(new Rect(1076, 503, 500, 48), Screen, WorkArea, [clock], 10);

        Assert.Equal(new Vector(-6, -3), offset);
    }

    [Fact]
    public void GetSnapOffset_FarAlongTheSameLine_IgnoresClock()
    {
        // Lined up with the clock's left edge, but far below it.
        var clock = new Rect(700, 500, 370, 36);

        var offset = WindowSnapper.GetSnapOffset(new Rect(704, 800, 500, 48), Screen, WorkArea, [clock], 10);

        Assert.Equal(default, offset);
    }
}
