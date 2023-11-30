using System;

namespace DesktopClock;

[Flags]
public enum TeachingTips
{
    None = 0,
    NewClock = 1 << 0,
    AdvancedSettings = 1 << 1,
    HideForNow = 1 << 2,
    CheckForUpdates = 1 << 3,
}