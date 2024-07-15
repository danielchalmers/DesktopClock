using System;
using System.Collections.Generic;

namespace DesktopClock;

/// <summary>
/// A defined color set.
/// </summary>
public readonly record struct Theme
{
    /// <summary>
    /// Friendly name for the theme.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The primary color, used in the text.
    /// </summary>
    public string PrimaryColor { get; }

    /// <summary>
    /// The secondary color, used in the background or outline.
    /// </summary>
    public string SecondaryColor { get; }

    public Theme(string name, string primaryColor, string secondaryColor)
    {
        Name = name;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
    }

    /// <summary>
    /// Built-in themes that the user can use without specifying their own palettes.
    /// </summary>
    /// <remarks>
    /// https://www.materialui.co/colors - A100, A700.
    /// </remarks>
    public static IReadOnlyList<Theme> DefaultThemes { get; } = new Theme[]
    {
        new("Light Text", "#F5F5F5", "#212121"),
        new("Dark Text", "#212121", "#F5F5F5"),
        new("Red", "#D50000", "#FF8A80"),
        new("Pink", "#C51162", "#FF80AB"),
        new("Purple", "#AA00FF", "#EA80FC"),
        new("Blue", "#2962FF", "#82B1FF"),
        new("Cyan", "#00B8D4", "#84FFFF"),
        new("Green", "#00C853", "#B9F6CA"),
        new("Orange", "#FF6D00", "#FFD180"),
    };

    /// <summary>
    /// Returns a random theme from <see cref="DefaultThemes"/>.
    /// </summary>
    public static Theme GetRandomDefaultTheme()
    {
        var random = new Random();
        return DefaultThemes[random.Next(0, DefaultThemes.Count)];
    }
}