using System.Windows.Input;

namespace DesktopClock.Tests;

public class WindowUtilTests
{
    [Theory]
    [InlineData(Key.Left, ModifierKeys.None, -1, 0)]
    [InlineData(Key.Right, ModifierKeys.None, 1, 0)]
    [InlineData(Key.Up, ModifierKeys.None, 0, -1)]
    [InlineData(Key.Down, ModifierKeys.None, 0, 1)]
    [InlineData(Key.Left, ModifierKeys.Shift, -10, 0)]
    [InlineData(Key.Right, ModifierKeys.Shift, 10, 0)]
    [InlineData(Key.Up, ModifierKeys.Shift, 0, -10)]
    [InlineData(Key.Down, ModifierKeys.Shift, 0, 10)]
    public void GetKeyboardNudge_ArrowKeys_MoveByStep(Key key, ModifierKeys modifiers, double expectedX, double expectedY)
    {
        var nudge = WindowUtil.GetKeyboardNudge(key, modifiers);

        Assert.Equal(expectedX, nudge.X);
        Assert.Equal(expectedY, nudge.Y);
    }

    [Theory]
    [InlineData(Key.Enter)]
    [InlineData(Key.A)]
    [InlineData(Key.Space)]
    public void GetKeyboardNudge_OtherKeys_DoNotMove(Key key)
    {
        var nudge = WindowUtil.GetKeyboardNudge(key, ModifierKeys.None);

        Assert.Equal(default, nudge);
    }

    private const int WsExTransparent = 0x00000020;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExAppWindow = 0x00040000;

    [Fact]
    public void GetWindowVisibilityExtendedStyle_HideFromAltTab_SetsToolWindowAndClearsAppWindow()
    {
        var style = WsExTransparent | WsExAppWindow;

        var updatedStyle = WindowUtil.GetWindowVisibilityExtendedStyle(
            style,
            showInTaskbar: true,
            hideFromAltTab: true);

        Assert.Equal(WsExTransparent | WsExToolWindow, updatedStyle);
    }

    [Fact]
    public void GetWindowVisibilityExtendedStyle_ShowInTaskbar_SetsAppWindowAndClearsToolWindow()
    {
        var style = WsExTransparent | WsExToolWindow;

        var updatedStyle = WindowUtil.GetWindowVisibilityExtendedStyle(
            style,
            showInTaskbar: true,
            hideFromAltTab: false);

        Assert.Equal(WsExTransparent | WsExAppWindow, updatedStyle);
    }

    [Fact]
    public void GetWindowVisibilityExtendedStyle_HideTaskbarWithoutHidingFromAltTab_ClearsBothFlags()
    {
        var style = WsExTransparent | WsExToolWindow | WsExAppWindow;

        var updatedStyle = WindowUtil.GetWindowVisibilityExtendedStyle(
            style,
            showInTaskbar: false,
            hideFromAltTab: false);

        Assert.Equal(WsExTransparent, updatedStyle);
    }
}
