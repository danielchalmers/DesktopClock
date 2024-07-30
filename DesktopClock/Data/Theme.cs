using System;
using System.Collections.Generic;

namespace DesktopClock;

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
    /// https://www.materialui.co/colors
    /// https://webaim.org/resources/contrastchecker
    /// </remarks>
    public static IReadOnlyList<Theme> DefaultThemes { get; } =
    [
        new("Light Text", "#F7F7F7", "#212121"),
        new("Dark Text", "#212121", "#F7F7F7"),
        new("Red", "#D50000", "#FFC2BD"),
        new("Pink", "#C51162", "#FFBDD2"),
        new("Purple", "#AA00FF", "#F6CBFB"),
        new("Blue", "#2962FF", "#C7DBFF"),
        new("Cyan", "#00A8C2", "#A8FFFF"),
        new("Green", "#00AD48", "#C9F8D4"),
        new("Orange", "#FF6D00", "#FFEFD6"),
    ];

    /// <summary>
    /// Returns a random theme from <see cref="DefaultThemes"/>.
    /// </summary>
    public static Theme GetRandomDefaultTheme()
    {
        var random = new Random();
        var index = random.Next(0, DefaultThemes.Count);
        return DefaultThemes[index];
    }
}
