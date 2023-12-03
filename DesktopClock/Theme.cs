using System;
using System.Collections.Generic;

namespace DesktopClock;

public readonly record struct Theme
{
    public string Name { get; }
    public string PrimaryColor { get; }
    public string SecondaryColor { get; }

    public Theme(string name, string primaryColor, string secondaryColor)
    {
        Name = name;
        PrimaryColor = primaryColor;
        SecondaryColor = secondaryColor;
    }

    // https://www.materialui.co/colors - A100, A700.
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

    public static Theme GetRandomDefaultTheme()
    {
        var random = new Random();
        return DefaultThemes[random.Next(0, DefaultThemes.Count)];
    }
}