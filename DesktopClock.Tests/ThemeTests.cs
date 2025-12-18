using System.Linq;

namespace DesktopClock.Tests;

public class ThemeTests
{
    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        // Arrange & Act
        var theme = new Theme("Test Theme", "#FFFFFF", "#000000");

        // Assert
        Assert.Equal("Test Theme", theme.Name);
        Assert.Equal("#FFFFFF", theme.PrimaryColor);
        Assert.Equal("#000000", theme.SecondaryColor);
    }

    [Fact]
    public void DefaultThemes_ShouldNotBeEmpty()
    {
        // Assert
        Assert.NotEmpty(Theme.DefaultThemes);
    }

    [Fact]
    public void DefaultThemes_ShouldContainLightTextTheme()
    {
        // Assert
        var lightTheme = Theme.DefaultThemes.FirstOrDefault(t => t.Name == "Light Text");
        Assert.NotEqual(default, lightTheme);
        Assert.Equal("#F7F7F7", lightTheme.PrimaryColor);
        Assert.Equal("#212121", lightTheme.SecondaryColor);
    }

    [Fact]
    public void DefaultThemes_ShouldContainDarkTextTheme()
    {
        // Assert
        var darkTheme = Theme.DefaultThemes.FirstOrDefault(t => t.Name == "Dark Text");
        Assert.NotEqual(default, darkTheme);
        Assert.Equal("#212121", darkTheme.PrimaryColor);
        Assert.Equal("#F7F7F7", darkTheme.SecondaryColor);
    }

    [Fact]
    public void DefaultThemes_AllShouldHaveValidColors()
    {
        // Assert
        foreach (var theme in Theme.DefaultThemes)
        {
            Assert.NotNull(theme.Name);
            Assert.NotEmpty(theme.Name);
            Assert.StartsWith("#", theme.PrimaryColor);
            Assert.StartsWith("#", theme.SecondaryColor);
            Assert.Equal(7, theme.PrimaryColor.Length); // #RRGGBB format
            Assert.Equal(7, theme.SecondaryColor.Length);
        }
    }

    [Fact]
    public void GetRandomDefaultTheme_ShouldReturnThemeFromDefaultThemes()
    {
        // Act
        var randomTheme = Theme.GetRandomDefaultTheme();

        // Assert
        Assert.Contains(randomTheme, Theme.DefaultThemes);
    }

    [Fact]
    public void GetRandomDefaultTheme_ShouldReturnValidTheme()
    {
        // Run multiple times to ensure randomness doesn't break anything
        for (int i = 0; i < 20; i++)
        {
            // Act
            var randomTheme = Theme.GetRandomDefaultTheme();

            // Assert
            Assert.NotNull(randomTheme.Name);
            Assert.NotNull(randomTheme.PrimaryColor);
            Assert.NotNull(randomTheme.SecondaryColor);
        }
    }

    [Fact]
    public void Theme_RecordEquality_ShouldWork()
    {
        // Arrange
        var theme1 = new Theme("Test", "#FFF", "#000");
        var theme2 = new Theme("Test", "#FFF", "#000");
        var theme3 = new Theme("Different", "#FFF", "#000");

        // Assert
        Assert.Equal(theme1, theme2);
        Assert.NotEqual(theme1, theme3);
    }
}
