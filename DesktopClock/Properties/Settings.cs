using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using DesktopClock.Utilities;
using Newtonsoft.Json;
using WpfWindowPlacement;

namespace DesktopClock.Properties;

public sealed class Settings : INotifyPropertyChanged, IDisposable
{
    private readonly FileSystemWatcher _watcher;

    private static readonly Lazy<Settings> _default = new(LoadAndAttemptSave);

    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        // Make it easier to read by a human.
        Formatting = Formatting.Indented,

        // Prevent a single error from taking down the whole file.
        Error = (_, e) => e.ErrorContext.Handled = true,
    };

    public static readonly double MaxSizeLog = 6.5;

    public static readonly double MinSizeLog = 2.7;
    static Settings()
    {
        // Settings file path from the same directory as the executable.
        var settingsFileName = Path.GetFileNameWithoutExtension(App.MainFileInfo.FullName) + ".settings";
        FilePath = Path.Combine(App.MainFileInfo.DirectoryName, settingsFileName);
    }

    // Private constructor to enforce singleton pattern.
    private Settings()
    {
        // Watch for changes in the settings file.
        _watcher = new(App.MainFileInfo.DirectoryName, Path.GetFileName(FilePath))
        {
            EnableRaisingEvents = true,
        };
        _watcher.Changed += FileChanged;
    }

#pragma warning disable CS0067 // The event 'Settings.PropertyChanged' is never used. Handled by Fody.
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067

    /// <summary>
    /// The singleton instance of the local settings file.
    /// </summary>
    public static Settings Default => _default.Value;

    /// <summary>
    /// The full path to the settings file.
    /// </summary>
    public static string FilePath { get; private set; }

    /// <summary>
    /// Indicates if the settings file can be saved to.
    /// </summary>
    /// <remarks>
    /// <c>false</c> could indicate the file is in a folder that requires administrator permissions among other constraints.
    /// </remarks>
    public static bool CanBeSaved { get; private set; }

    /// <summary>
    /// Checks if the settings file exists on the disk.
    /// </summary>
    public static bool Exists => File.Exists(FilePath);

    #region "Properties"

    /// <summary>
    /// Format string used while the window is showing the current date and time instead of a countdown.
    /// </summary>
    /// <remarks>
    /// Use this to decide exactly which date and time parts appear on the clock during normal use.
    /// Change it when you want a different arrangement, such as adding the weekday, seconds, or a shorter date.
    /// See: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings">Custom date and time format strings</see>.
    /// </remarks>
    public string Format { get; set; } = "{ddd}, {MMM dd}, {h:mm:ss tt}";

    /// <summary>
    /// Format string used while <see cref="CountdownTo"/> is enabled.
    /// </summary>
    /// <remarks>
    /// Use this when you want countdowns to follow a specific layout instead of a simple human-readable phrase.
    /// Leave it blank if you prefer a more natural countdown such as "2 hours".
    /// See: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings">Custom TimeSpan format strings</see>.
    /// </remarks>
    public string CountdownFormat { get; set; } = "";

    /// <summary>
    /// Target date and time for countdown mode.
    /// </summary>
    /// <remarks>
    /// Set this when you want the clock to count down to a deadline, event, or appointment instead of showing the
    /// current time. Clear it to return to the normal clock display.
    /// </remarks>
    public DateTime CountdownTo { get; set; } = default;

    /// <summary>
    /// Time zone identifier used for the normal clock display.
    /// </summary>
    /// <remarks>
    /// Use this when you want the clock to follow a different city or region instead of your local system time.
    /// This is useful for remote work, travel, or tracking another time zone at a glance.
    /// </remarks>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Font family used by the main clock window.
    /// </summary>
    /// <remarks>
    /// Choose this to give the clock its overall typographic personality.
    /// Different families can make the clock feel more compact, more readable, or more decorative.
    /// </remarks>
    public string FontFamily { get; set; } = "Consolas";

    /// <summary>
    /// Font style used by the main clock window.
    /// </summary>
    /// <remarks>
    /// Use this to switch between normal, italic, or oblique text.
    /// It is mainly a visual choice for matching the tone you want.
    /// </remarks>
    public string FontStyle { get; set; } = "Normal";

    /// <summary>
    /// Font weight used by the main clock window.
    /// </summary>
    /// <remarks>
    /// Use this to make the clock look lighter or bolder.
    /// Heavier weights usually stand out more from a distance, while lighter ones can feel less intrusive.
    /// </remarks>
    public string FontWeight { get; set; } = "Normal";

    /// <summary>
    /// Fill color of the clock text.
    /// </summary>
    /// <remarks>
    /// Choose this for the main text color you want to read at a glance.
    /// It usually works together with <see cref="OuterColor"/> to control contrast.
    /// </remarks>
    public Color TextColor { get; set; } = Color.FromRgb(33, 33, 33);

    /// <summary>
    /// Opacity applied to the text fill.
    /// </summary>
    /// <remarks>
    /// Lower this if you want the clock text to feel more subtle on the desktop.
    /// Keep it high if readability is more important than blending in.
    /// </remarks>
    public double TextOpacity { get; set; } = 1;

    /// <summary>
    /// Color used for the background fill or the text outline, depending on the current appearance mode.
    /// </summary>
    /// <remarks>
    /// Think of this as the secondary appearance color around the text.
    /// It is useful for adding contrast, making the clock easier to read, or matching the rest of the desktop theme.
    /// </remarks>
    public Color OuterColor { get; set; } = Color.FromRgb(247, 247, 247);

    /// <summary>
    /// Chooses between a filled background and outlined text.
    /// </summary>
    /// <remarks>
    /// Turn this on for a label-like clock with a solid backing, or turn it off for a lighter look where only the text
    /// is outlined.
    /// </remarks>
    public bool BackgroundEnabled { get; set; } = true;

    /// <summary>
    /// Opacity used for the outer visual treatment.
    /// </summary>
    /// <remarks>
    /// Use this to control how strong the background, image, or outline feels.
    /// Lower values make the clock blend in more with the desktop behind it.
    /// </remarks>
    public double BackgroundOpacity { get; set; } = 0.90;

    /// <summary>
    /// Corner radius of the background border.
    /// </summary>
    /// <remarks>
    /// Increase this for softer, pill-like corners, or reduce it for a squarer look.
    /// It matters most when the clock is using a visible background.
    /// </remarks>
    public double BackgroundCornerRadius { get; set; } = 1;

    /// <summary>
    /// File path for an optional image drawn behind the text.
    /// </summary>
    /// <remarks>
    /// Use this when you want the clock to sit on top of a texture, badge, or custom artwork instead of a plain color.
    /// Leave it empty if you want a simpler solid background.
    /// </remarks>
    public string BackgroundImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Stretch mode for the optional background image.
    /// </summary>
    /// <remarks>
    /// Use this to decide whether the image should fill the space, keep its proportions, or fit more conservatively.
    /// It only matters when a background image is being used.
    /// </remarks>
    public Stretch BackgroundImageStretch { get; set; } = Stretch.Fill;

    /// <summary>
    /// Thickness of the text outline.
    /// </summary>
    /// <remarks>
    /// Increase this when you want outlined text to stand out more clearly against a busy desktop.
    /// It mainly matters when the clock is using an outline instead of a solid background.
    /// </remarks>
    public double OutlineThickness { get; set; } = 0.2;

    /// <summary>
    /// Whether the clock window should stay above normal windows.
    /// </summary>
    /// <remarks>
    /// Turn this on if you always want the clock visible while working.
    /// Turn it off if you prefer the clock to behave like a normal window and get covered by other apps.
    /// </remarks>
    public bool Topmost { get; set; } = true;

    /// <summary>
    /// Hides the clock when another fullscreen window occupies the same monitor.
    /// </summary>
    /// <remarks>
    /// Use this if you want games, videos, or presentations to take over the screen without the clock staying visible
    /// on top of them.
    /// </remarks>
    public bool HideWhenFullscreen { get; set; } = false;

    /// <summary>
    /// Whether the clock should appear in the Windows taskbar.
    /// </summary>
    /// <remarks>
    /// Turn this on if you want the clock to be easier to find and restore from the taskbar.
    /// Turn it off if you prefer relying on the tray or a less prominent presence.
    /// </remarks>
    public bool ShowInTaskbar { get; set; } = true;

    /// <summary>
    /// Whether the clock should be removed from the Alt+Tab switcher.
    /// </summary>
    /// <remarks>
    /// Use this when you want the clock to stay out of the normal app-switching flow.
    /// It is helpful if the clock is meant to feel more like an overlay than a regular app window.
    /// </remarks>
    public bool HideFromAltTab { get; set; } = false;

    /// <summary>
    /// Height of the clock display area in device-independent units.
    /// </summary>
    /// <remarks>
    /// Use this as the main size control for the clock.
    /// Raise it for a more visible desktop clock, or lower it if you want something smaller and less dominant.
    /// </remarks>
    public int Height { get; set; } = 48;

    /// <summary>
    /// Whether Windows should launch this executable when the current user signs in.
    /// </summary>
    /// <remarks>
    /// Turn this on if the clock is part of your normal desktop setup and you want it available automatically after
    /// signing in.
    /// </remarks>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Starts the clock minimized and hidden from the desktop.
    /// </summary>
    /// <remarks>
    /// Use this if you want the app running in the background without showing the clock immediately at sign-in or
    /// launch.
    /// </remarks>
    public bool StartHidden { get; set; } = false;

    /// <summary>
    /// Allows the main window to be repositioned by dragging with the left mouse button.
    /// </summary>
    /// <remarks>
    /// Turn this on if you want to place the clock directly with the mouse.
    /// Turn it off if you want to avoid accidental moves after getting the position just right.
    /// </remarks>
    public bool DragToMove { get; set; } = true;

    /// <summary>
    /// Makes the clock ignore mouse input so clicks reach windows underneath it.
    /// </summary>
    /// <remarks>
    /// Use this when the clock is acting as a passive overlay and you do not want it blocking clicks on other windows.
    /// It is less convenient if you still want to interact with the clock directly.
    /// </remarks>
    public bool ClickThrough { get; set; } = false;

    /// <summary>
    /// Experimental option that keeps the right edge fixed while the window width changes.
    /// </summary>
    /// <remarks>
    /// This is useful if you anchor the clock to the right side of a screen and want it to grow or shrink inward.
    /// It is marked experimental because that behavior can still look imperfect in some situations.
    /// </remarks>
    public bool RightAligned { get; set; } = false;

    /// <summary>
    /// Experimental option that periodically nudges the clock to reduce burn-in risk.
    /// </summary>
    /// <remarks>
    /// Use this on displays where static content is a concern and a tiny amount of movement is acceptable.
    /// It trades perfect stillness for better long-running display hygiene.
    /// </remarks>
    public bool BurnInMitigation { get; set; } = false;

    /// <summary>
    /// Path to the WAV file used for clock and countdown alerts.
    /// </summary>
    /// <remarks>
    /// Choose a WAV file here if you want the clock to make an audible alert.
    /// This can be used for repeating chimes, countdown alerts, or both.
    /// </remarks>
    public string WavFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Interval used to decide when the alert sound should play.
    /// </summary>
    /// <remarks>
    /// Use this for a repeating reminder sound, such as every minute, every quarter hour, or every hour.
    /// When countdown mode is active, it can also act like a repeating countdown warning.
    /// </remarks>
    public TimeSpan WavFileInterval { get; set; }

    /// <summary>
    /// Enables a sound at the exact moment the countdown target is reached.
    /// </summary>
    /// <remarks>
    /// Turn this on if the countdown should end with a clear audible signal even when you are not watching the screen.
    /// </remarks>
    public bool PlaySoundOnCountdown { get; set; } = true;

    /// <summary>
    /// Persisted width of the settings window.
    /// </summary>
    /// <remarks>
    /// This remembers how wide you last made the settings window so it feels familiar the next time you open it.
    /// </remarks>
    public double SettingsWindowWidth { get; set; } = 720;

    /// <summary>
    /// Persisted height of the settings window.
    /// </summary>
    /// <remarks>
    /// This remembers how tall you last made the settings window so you do not have to resize it every time.
    /// </remarks>
    public double SettingsWindowHeight { get; set; } = 600;

    /// <summary>
    /// Persisted vertical scroll offset of the settings window.
    /// </summary>
    /// <remarks>
    /// This helps reopen the settings window near the same section you were working in before.
    /// </remarks>
    public double SettingsScrollPosition { get; set; } = 0;

    /// <summary>
    /// Bit flags describing which one-time teaching tips have already been shown.
    /// </summary>
    /// <remarks>
    /// This keeps introductory tips from being shown over and over after the user has already seen them once.
    /// </remarks>
    public TeachingTips TipsShown { get; set; }

    /// <summary>
    /// Last rendered clock text, saved so the next launch starts near the previous width.
    /// </summary>
    /// <remarks>
    /// This helps the clock reopen with a similar shape to the last session instead of visibly resizing right away.
    /// </remarks>
    public string LastDisplay { get; set; }

    /// <summary>
    /// Persisted native window placement for the main clock window.
    /// </summary>
    /// <remarks>
    /// This remembers where the clock was placed so it can return to the same spot next time.
    /// </remarks>
    public WindowPlacement Placement { get; set; }

    /// <summary>
    /// UI-facing wrapper around <see cref="TimeZone"/> for the settings window.
    /// </summary>
    /// <remarks>
    /// This exists to make time zone selection easier to present and edit in the settings UI.
    /// </remarks>
    [JsonIgnore]
    public TimeZoneInfo TimeZoneInfo
    {
        get
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.Local;
            }
        }
        set
        {
            TimeZone = value.Id;
        }
    }

    #endregion "Properties"

    /// <summary>
    /// Saves to the default path in JSON format.
    /// </summary>
    public bool Save()
    {
        var shouldResumeWatcher = _watcher.EnableRaisingEvents;

        if (shouldResumeWatcher)
            _watcher.EnableRaisingEvents = false;

        try
        {
            var json = JsonConvert.SerializeObject(this, _jsonSerializerSettings);

            // Attempt to save multiple times.
            for (var i = 0; i < 4; i++)
            {
                try
                {
                    File.WriteAllText(FilePath, json);
                    return true;
                }
                catch
                {
                    // Wait before next attempt to read.
                    System.Threading.Thread.Sleep(250);
                }
            }
        }
        catch (JsonSerializationException)
        {
        }
        finally
        {
            if (shouldResumeWatcher)
                _watcher.EnableRaisingEvents = true;
        }

        return false;
    }

    /// <summary>
    /// Populates the given settings with values from the default path.
    /// </summary>
    private static void Populate(Settings settings)
    {
        using var fileStream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var streamReader = new StreamReader(fileStream);
        using var jsonReader = new JsonTextReader(streamReader);

        JsonSerializer.Create(_jsonSerializerSettings).Populate(jsonReader, settings);
    }

    /// <summary>
    /// Loads from the default path in JSON format.
    /// </summary>
    private static Settings LoadFromFile()
    {
        try
        {
            var settings = new Settings();
            Populate(settings);
            return settings;
        }
        catch
        {
            return new();
        }
    }

    /// <summary>
    /// Loads from the default path in JSON format then attempts to save in order to check if it can be done.
    /// </summary>
    private static Settings LoadAndAttemptSave()
    {
        var settings = LoadFromFile();

        if (!File.Exists(FilePath))
        {
            settings.ApplySystemThemeDefaultsIfAvailable();
        }

        CanBeSaved = settings.Save();

        return settings;
    }

    /// <summary>
    /// Occurs after the watcher detects a change in the settings file.
    /// </summary>
    private void FileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            Populate(this);
        }
        catch
        {
        }
    }

    /// <summary>
    /// Adjusts the height by a number of steps.
    /// </summary>
    public void ScaleHeight(double steps)
    {
        // Convert the height, adjust it, then convert back in the same way as the slider.
        var newHeightLog = Math.Log(Height) + (steps * 0.15);
        var newHeightLogClamped = Math.Min(Math.Max(newHeightLog, MinSizeLog), MaxSizeLog);
        var exp = Math.Exp(newHeightLogClamped);

        // Save the new height as an integer to make it easier for the user.
        Height = (int)exp;
    }

    private void ApplySystemThemeDefaultsIfAvailable()
    {
        if (!SystemThemeService.TryGetThemeDefaults(out var textColor, out var outerColor))
            return;

        TextColor = textColor;
        OuterColor = outerColor;
    }

    public void Dispose()
    {
        // We don't dispose of the watcher anymore because it would actually hang indefinitely if you had multiple instances of the same clock open.
        //_watcher?.Dispose();
    }
}
