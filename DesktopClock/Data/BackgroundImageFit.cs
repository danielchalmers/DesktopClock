using System.ComponentModel;
using System.Windows.Media;

namespace DesktopClock;

public enum BackgroundImageFit
{
    [Description("Original size")]
    OriginalSize,

    [Description("Stretch to fill")]
    StretchToFill,

    [Description("Fit inside")]
    FitInside,

    [Description("Fill and crop")]
    FillAndCrop,
}

public static class BackgroundImageFitExtensions
{
    public static Stretch ToStretch(this BackgroundImageFit fit) => fit switch
    {
        BackgroundImageFit.OriginalSize => Stretch.None,
        BackgroundImageFit.StretchToFill => Stretch.Fill,
        BackgroundImageFit.FitInside => Stretch.Uniform,
        BackgroundImageFit.FillAndCrop => Stretch.UniformToFill,
        _ => Stretch.Fill,
    };

    public static BackgroundImageFit ToBackgroundImageFit(this Stretch stretch) => stretch switch
    {
        Stretch.None => BackgroundImageFit.OriginalSize,
        Stretch.Fill => BackgroundImageFit.StretchToFill,
        Stretch.Uniform => BackgroundImageFit.FitInside,
        Stretch.UniformToFill => BackgroundImageFit.FillAndCrop,
        _ => BackgroundImageFit.StretchToFill,
    };
}
