namespace DesktopClock.Tests;

public class WindowUtilTests
{
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
