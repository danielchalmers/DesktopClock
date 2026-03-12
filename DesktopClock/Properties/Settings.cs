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
    /// This is re-evaluated each second in <see cref="MainWindow"/> and whenever <see cref="TimeZone"/> changes.
    /// Text inside braces is processed by the custom tokenizer before falling back to standard .NET formatting.
    /// See: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings">Custom date and time format strings</see>.
    /// </remarks>
    public string Format { get; set; } = "{ddd}, {MMM dd}, {h:mm:ss tt}";

    /// <summary>
    /// Format string used while <see cref="CountdownTo"/> is enabled.
    /// </summary>
    /// <remarks>
    /// This is ignored unless countdown mode is active. When left blank, the clock shows Humanizer text such as
    /// "2 hours" instead of formatting a <see cref="TimeSpan"/>.
    /// See: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings">Custom TimeSpan format strings</see>.
    /// </remarks>
    public string CountdownFormat { get; set; } = "";

    /// <summary>
    /// Target date and time for countdown mode.
    /// </summary>
    /// <remarks>
    /// Any non-default value switches the main display from clock mode to countdown mode.
    /// The same value is also used by the sound trigger logic to decide when countdown-related audio should play.
    /// </remarks>
    public DateTime CountdownTo { get; set; } = default;

    /// <summary>
    /// Time zone identifier used for the normal clock display.
    /// </summary>
    /// <remarks>
    /// The stored value is resolved through <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>.
    /// If the ID does not exist on the current machine, the app falls back to the local time zone instead of failing.
    /// </remarks>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Font family used by the main clock window.
    /// </summary>
    /// <remarks>
    /// This is bound to the window's <c>FontFamily</c>, so it affects the rendered clock text directly and can change
    /// the auto-sized width of the window.
    /// </remarks>
    public string FontFamily { get; set; } = "Consolas";

    /// <summary>
    /// Font style used by the main clock window.
    /// </summary>
    /// <remarks>
    /// This is bound to the window's <c>FontStyle</c>, which means italic or oblique text changes the live clock
    /// rendering immediately.
    /// </remarks>
    public string FontStyle { get; set; } = "Normal";

    /// <summary>
    /// Font weight used by the main clock window.
    /// </summary>
    /// <remarks>
    /// This is bound to the window's <c>FontWeight</c>.
    /// Heavier or lighter weights often change the measured width, so the window can resize as this changes.
    /// </remarks>
    public string FontWeight { get; set; } = "Normal";

    /// <summary>
    /// Fill color of the clock text.
    /// </summary>
    /// <remarks>
    /// This drives the <see cref="OutlinedTextBlock.Fill"/> brush in the main window.
    /// On first run, it may be replaced with a value inferred from the current Windows theme.
    /// </remarks>
    public Color TextColor { get; set; } = Color.FromRgb(33, 33, 33);

    /// <summary>
    /// Opacity applied to the text fill.
    /// </summary>
    /// <remarks>
    /// This affects only the text itself.
    /// The background and outline use <see cref="BackgroundOpacity"/> instead.
    /// </remarks>
    public double TextOpacity { get; set; } = 1;

    /// <summary>
    /// Color used for the background fill or the text outline, depending on the current appearance mode.
    /// </summary>
    /// <remarks>
    /// When <see cref="BackgroundEnabled"/> is <see langword="true"/>, this becomes the solid background color if no
    /// image is selected. When <see cref="BackgroundEnabled"/> is <see langword="false"/>, it becomes the outline
    /// stroke color around the text.
    /// </remarks>
    public Color OuterColor { get; set; } = Color.FromRgb(247, 247, 247);

    /// <summary>
    /// Chooses between a filled background and outlined text.
    /// </summary>
    /// <remarks>
    /// When enabled, the outer border receives either a solid color or background image and the text stroke is turned
    /// off. When disabled, the border becomes transparent and the outer styling is applied as a text outline instead.
    /// </remarks>
    public bool BackgroundEnabled { get; set; } = true;

    /// <summary>
    /// Opacity used for the outer visual treatment.
    /// </summary>
    /// <remarks>
    /// This is shared by the solid background, the background image brush, and the outline stroke.
    /// It does not affect the text fill.
    /// </remarks>
    public double BackgroundOpacity { get; set; } = 0.90;

    /// <summary>
    /// Corner radius of the background border.
    /// </summary>
    /// <remarks>
    /// This is applied to the main <see cref="System.Windows.Controls.Border"/> around the clock.
    /// It is most noticeable when <see cref="BackgroundEnabled"/> is enabled.
    /// </remarks>
    public double BackgroundCornerRadius { get; set; } = 1;

    /// <summary>
    /// File path for an optional image drawn behind the text.
    /// </summary>
    /// <remarks>
    /// This is only used when <see cref="BackgroundEnabled"/> is enabled.
    /// An empty value falls back to a solid <see cref="OuterColor"/> background; a non-empty value uses an
    /// <see cref="ImageBrush"/> instead.
    /// </remarks>
    public string BackgroundImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Stretch mode for the optional background image.
    /// </summary>
    /// <remarks>
    /// This is passed directly to the background <see cref="ImageBrush"/> when <see cref="BackgroundImagePath"/> is
    /// non-empty.
    /// </remarks>
    public Stretch BackgroundImageStretch { get; set; } = Stretch.Fill;

    /// <summary>
    /// Thickness of the text outline.
    /// </summary>
    /// <remarks>
    /// This is only visible when <see cref="BackgroundEnabled"/> is disabled.
    /// The value is also reused as margin so the outline has room to render without clipping.
    /// </remarks>
    public double OutlineThickness { get; set; } = 0.2;

    /// <summary>
    /// Whether the clock window should stay above normal windows.
    /// </summary>
    /// <remarks>
    /// This is bound directly to <see cref="Window.Topmost"/> on the main window.
    /// The same setting is exposed in the tray and window context menus.
    /// </remarks>
    public bool Topmost { get; set; } = true;

    /// <summary>
    /// Hides the clock when another fullscreen window occupies the same monitor.
    /// </summary>
    /// <remarks>
    /// <see cref="FullscreenHideManager"/> checks this on setting changes and once per second.
    /// It only hides the clock for visible fullscreen windows on the same monitor, not just any foreground app.
    /// </remarks>
    public bool HideWhenFullscreen { get; set; } = false;

    /// <summary>
    /// Whether the clock should appear in the Windows taskbar.
    /// </summary>
    /// <remarks>
    /// This is applied through <see cref="WindowUtil.ApplyWindowVisibility(Window, bool, bool)"/>.
    /// It affects how the minimized clock can be brought back when the app is hidden temporarily or started hidden.
    /// </remarks>
    public bool ShowInTaskbar { get; set; } = true;

    /// <summary>
    /// Whether the clock should be removed from the Alt+Tab switcher.
    /// </summary>
    /// <remarks>
    /// This is implemented with window extended styles rather than by closing or recreating the window.
    /// It can be combined with <see cref="ShowInTaskbar"/> to tune how discoverable the clock is.
    /// </remarks>
    public bool HideFromAltTab { get; set; } = false;

    /// <summary>
    /// Height of the clock display area in device-independent units.
    /// </summary>
    /// <remarks>
    /// This is bound to the outer <see cref="System.Windows.Controls.Viewbox"/>, while width remains content-driven.
    /// Ctrl+mouse wheel and Ctrl+plus/minus change it through <see cref="ScaleHeight(double)"/>.
    /// </remarks>
    public int Height { get; set; } = 48;

    /// <summary>
    /// Whether Windows should launch this executable when the current user signs in.
    /// </summary>
    /// <remarks>
    /// The registry entry is written or removed in <see cref="App.SetRunOnStartup(bool)"/> when the main window closes.
    /// The value is stored per-user under the standard <c>Run</c> key.
    /// </remarks>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Starts the clock minimized and hidden from the desktop.
    /// </summary>
    /// <remarks>
    /// This is checked once during window initialization.
    /// The app still loads normally, then immediately hides itself and shows a tray notification explaining how to
    /// restore it.
    /// </remarks>
    public bool StartHidden { get; set; } = false;

    /// <summary>
    /// Allows the main window to be repositioned by dragging with the left mouse button.
    /// </summary>
    /// <remarks>
    /// When enabled, <see cref="MainWindow"/> pauses timer updates and clears any pixel shift before calling
    /// <c>DragMove</c>, then saves the new base position afterward.
    /// </remarks>
    public bool DragToMove { get; set; } = true;

    /// <summary>
    /// Makes the clock ignore mouse input so clicks reach windows underneath it.
    /// </summary>
    /// <remarks>
    /// This toggles the native <c>WS_EX_TRANSPARENT</c> extended style at startup and whenever the setting changes.
    /// While enabled, normal interactions with the clock itself stop working.
    /// </remarks>
    public bool ClickThrough { get; set; } = false;

    /// <summary>
    /// Experimental option that keeps the right edge fixed while the window width changes.
    /// </summary>
    /// <remarks>
    /// When the content width changes, <see cref="MainWindow"/> moves the window left by the same amount so the right
    /// edge stays in place. Small glitches can still happen because the rest of the windowing behavior is naturally
    /// left-anchored.
    /// </remarks>
    public bool RightAligned { get; set; } = false;

    /// <summary>
    /// Experimental option that periodically nudges the clock to reduce burn-in risk.
    /// </summary>
    /// <remarks>
    /// When enabled, the app lazily creates a <see cref="PixelShifter"/> and applies a small movement once per minute
    /// while the window is visible. The unshifted base position is restored before saving placement so the drift is not
    /// persisted between launches.
    /// </remarks>
    public bool BurnInMitigation { get; set; } = false;

    /// <summary>
    /// Path to the WAV file used for clock and countdown alerts.
    /// </summary>
    /// <remarks>
    /// Sound playback is only armed when this points to an existing file and either
    /// <see cref="WavFileInterval"/> is non-zero or <see cref="PlaySoundOnCountdown"/> is enabled.
    /// </remarks>
    public string WavFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Interval used to decide when the alert sound should play.
    /// </summary>
    /// <remarks>
    /// With no countdown active, the interval is checked against the current time of day.
    /// With countdown mode active, it is checked against the remaining countdown duration instead.
    /// </remarks>
    public TimeSpan WavFileInterval { get; set; }

    /// <summary>
    /// Enables a sound at the exact moment the countdown target is reached.
    /// </summary>
    /// <remarks>
    /// This also keeps the sound subsystem active even if <see cref="WavFileInterval"/> is zero.
    /// If both settings are used, the exact countdown completion still counts as a match and plays the same WAV file.
    /// </remarks>
    public bool PlaySoundOnCountdown { get; set; } = true;

    /// <summary>
    /// Persisted width of the settings window.
    /// </summary>
    /// <remarks>
    /// The settings window binds its <c>Width</c> directly to this property so user resizing is saved automatically.
    /// </remarks>
    public double SettingsWindowWidth { get; set; } = 720;

    /// <summary>
    /// Persisted height of the settings window.
    /// </summary>
    /// <remarks>
    /// The settings window binds its <c>Height</c> directly to this property so the next session reopens at the same
    /// size.
    /// </remarks>
    public double SettingsWindowHeight { get; set; } = 600;

    /// <summary>
    /// Persisted vertical scroll offset of the settings window.
    /// </summary>
    /// <remarks>
    /// <see cref="SettingsWindow"/> restores this after loading so the user returns to the same section they were last
    /// editing.
    /// </remarks>
    public double SettingsScrollPosition { get; set; } = 0;

    /// <summary>
    /// Bit flags describing which one-time teaching tips have already been shown.
    /// </summary>
    /// <remarks>
    /// This currently suppresses repeated helper dialogs such as the advanced settings explanation and the "Hide for
    /// now" tip.
    /// </remarks>
    public TeachingTips TipsShown { get; set; }

    /// <summary>
    /// Last rendered clock text, saved so the next launch starts near the previous width.
    /// </summary>
    /// <remarks>
    /// The main window restores this before the first timer tick because the clock auto-sizes to its content.
    /// That avoids an obvious width jump during startup.
    /// </remarks>
    public string LastDisplay { get; set; }

    /// <summary>
    /// Persisted native window placement for the main clock window.
    /// </summary>
    /// <remarks>
    /// This is restored during source initialization and rewritten on close through the WpfWindowPlacement helpers.
    /// If burn-in mitigation is active, the base position is restored first so the saved placement is the intentional
    /// location rather than a temporary shifted offset.
    /// </remarks>
    public WindowPlacement Placement { get; set; }

    /// <summary>
    /// UI-facing wrapper around <see cref="TimeZone"/> for the settings window.
    /// </summary>
    /// <remarks>
    /// The settings UI binds to <see cref="TimeZoneInfo"/> objects, while the JSON file persists only the time zone ID
    /// string. Unknown IDs resolve to the local zone here instead of throwing.
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
