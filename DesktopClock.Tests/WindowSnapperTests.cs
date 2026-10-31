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
        var offset = WindowSnapper.GetSnapOffset(new Rect(left, top, 500, 48), Screen, WorkArea, 10);

        Assert.Equal(expectedLeft, left + offset.X);
        Assert.Equal(expectedTop, top + offset.Y);
    }

    [Fact]
    public void GetSnapOffset_FarFromEdges_DoesNotMove()
    {
        var offset = WindowSnapper.GetSnapOffset(new Rect(300, 200, 500, 48), Screen, WorkArea, 10);

        Assert.Equal(default, offset);
    }

    [Fact]
    public void GetSnapOffset_JustOutsideDistance_DoesNotMove()
    {
        var offset = WindowSnapper.GetSnapOffset(new Rect(11, 200, 500, 48), Screen, WorkArea, 10);

        Assert.Equal(default, offset);
    }

    [Fact]
    public void GetSnapOffset_ScreenLeftOfTheMainOne_SnapsToItsEdges()
    {
        var screen = new Rect(-1920, 0, 1920, 1080);

        var offset = WindowSnapper.GetSnapOffset(new Rect(-1914, 5, 500, 48), screen, screen, 10);

        Assert.Equal(new Vector(-6, -5), offset);
    }

    [Theory]
    [InlineData(1914, 500)] // Its left edge is near the screen's right edge.
    [InlineData(300, 1075)] // Its top edge is near the screen's bottom edge.
    [InlineData(-494, -43)] // Its bottom-right corner is near the screen's top-left corner.
    public void GetSnapOffset_FarEdgeNearScreenEdge_DoesNotSnapOffScreen(double left, double top)
    {
        var offset = WindowSnapper.GetSnapOffset(new Rect(left, top, 500, 48), Screen, WorkArea, 10);

        Assert.Equal(default, offset);
    }
}
