using System.ComponentModel;

namespace DesktopClock;

public enum TextTransform
{
    [Description("No change")]
    None,
    [Description("UPPERCASE")]
    Uppercase,
    [Description("lowercase")]
    Lowercase,
}
