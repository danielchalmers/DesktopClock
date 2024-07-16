using System;

namespace DesktopClock;

[Flags]
public enum TeachingTips
{
    None = 0,

    NewClock = 1 << 0,

    [Obsolete("Settings were moved to a native window")]
    AdvancedSettings = 1 << 1,

    HideForNow = 1 << 2,

    [Obsolete("Moved to Help which and is clearly visible as a link")]
    CheckForUpdates = 1 << 3,
}
