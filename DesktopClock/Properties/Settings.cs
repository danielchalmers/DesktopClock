using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json;
using WpfWindowPlacement;

namespace DesktopClock.Properties;

public sealed class Settings : INotifyPropertyChanged, IDisposable
{
    private readonly FileSystemWatcher _watcher;
    private bool _followSystemTheme;
    private bool _systemThemeIsLight = true;
    private Color _systemAccentColor = DefaultAccentColor;
    private Color _textColor = Color.FromRgb(33, 33, 33);
    private Color _outerColor = Color.FromRgb(247, 247, 247);

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
    private static readonly Color DefaultAccentColor = Color.FromRgb(0, 120, 215);
    private static readonly Color LightThemeOuterColor = Color.FromRgb(247, 247, 247);
    private static readonly Color DarkThemeOuterColor = Color.FromRgb(32, 32, 32);
    private const string PersonalizeKeyPath = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

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

        RefreshSystemThemeColors(notifyIfFollowing: false);
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        SystemParameters.StaticPropertyChanged += SystemParameters_StaticPropertyChanged;
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
    /// .NET format string for the time shown on the clock. Format specific parts inside { and }.
    /// </summary>
    /// <remarks>
    /// See: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings">Custom date and time format strings</see>.
    /// </remarks>
    public string Format { get; set; } = "{ddd}, {MMM dd}, {h:mm:ss tt}";

    /// <summary>
    /// .NET format string for the countdown mode. If left blank, it will be dynamic.
    /// </summary>
    /// <remarks>
    /// See: <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-timespan-format-strings">Custom TimeSpan format strings</see>.
    /// </remarks>
    public string CountdownFormat { get; set; } = "";

    /// <summary>
    /// Date and time to countdown to. If left blank, countdown mode is not enabled.
    /// </summary>
    public DateTime CountdownTo { get; set; } = default;

    /// <summary>
    /// A different time zone to be used.
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Font to use for the clock's text.
    /// </summary>
    public string FontFamily { get; set; } = "Consolas";

    /// <summary>
    /// Style of font to use for the clock's text.
    /// </summary>
    public string FontStyle { get; set; } = "Normal";

    /// <summary>
    /// Weight of the font for the clock's text.
    /// </summary>
    public string FontWeight { get; set; } = "Normal";

    /// <summary>
    /// Text transformation to apply to the clock's text.
    /// </summary>
    public TextTransform TextTransform { get; set; } = TextTransform.None;

    /// <summary>
    /// Text color for the clock's text.
    /// </summary>
    public Color TextColor
    {
        get => _textColor;
        set
        {
            if (_textColor.Equals(value))
                return;

            _textColor = value;
            NotifyPropertyChanged(nameof(TextColor));
            NotifyEffectiveThemeChanged();
        }
    }

    /// <summary>
    /// Opacity of the text.
    /// </summary>
    public double TextOpacity { get; set; } = 1;

    /// <summary>
    /// The outer color, for either the background or the outline.
    /// </summary>
    public Color OuterColor
    {
        get => _outerColor;
        set
        {
            if (_outerColor.Equals(value))
                return;

            _outerColor = value;
            NotifyPropertyChanged(nameof(OuterColor));
            NotifyEffectiveThemeChanged();
        }
    }

    /// <summary>
    /// Follow the system theme and accent color.
    /// </summary>
    public bool FollowSystemTheme
    {
        get => _followSystemTheme;
        set
        {
            if (_followSystemTheme == value)
                return;

            _followSystemTheme = value;
            NotifyPropertyChanged(nameof(FollowSystemTheme));
            RefreshSystemThemeColors(notifyIfFollowing: false);
            NotifyEffectiveThemeChanged();
        }
    }

    /// <summary>
    /// Effective text color based on theme-following preference.
    /// </summary>
    [JsonIgnore]
    public Color EffectiveTextColor => FollowSystemTheme ? _systemAccentColor : TextColor;

    /// <summary>
    /// Effective outer color based on theme-following preference.
    /// </summary>
    [JsonIgnore]
    public Color EffectiveOuterColor => FollowSystemTheme
        ? (_systemThemeIsLight ? LightThemeOuterColor : DarkThemeOuterColor)
        : OuterColor;

    /// <summary>
    /// Shows a solid background instead of an outline.
    /// </summary>
    public bool BackgroundEnabled { get; set; } = true;

    /// <summary>
    /// Opacity of the background.
    /// </summary>
    public double BackgroundOpacity { get; set; } = 0.90;

    /// <summary>
    /// Corner radius of the background.
    /// </summary>
    public double BackgroundCornerRadius { get; set; } = 1;

    /// <summary>
    /// Path to the background image. If left blank, a solid color will be used.
    /// </summary>
    public string BackgroundImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Stretch mode for the background image.
    /// </summary>
    public Stretch BackgroundImageStretch { get; set; } = Stretch.Fill;

    /// <summary>
    /// Thickness of the outline around the clock.
    /// </summary>
    public double OutlineThickness { get; set; } = 0.2;

    /// <summary>
    /// Keeps the clock on top of other windows.
    /// </summary>
    public bool Topmost { get; set; } = true;

    /// <summary>
    /// Hides the clock when a fullscreen app covers the same monitor.
    /// </summary>
    public bool HideWhenFullscreen { get; set; } = false;

    /// <summary>
    /// Shows the app icon in the taskbar instead of the tray.
    /// </summary>
    public bool ShowInTaskbar { get; set; } = false;

    /// <summary>
    /// Hides the clock window from Alt+Tab.
    /// </summary>
    public bool HideFromAltTab { get; set; } = false;

    /// <summary>
    /// Height of the clock window.
    /// </summary>
    public int Height { get; set; } = 48;

    /// <summary>
    /// Opens the app when you log in.
    /// </summary>
    public bool RunOnStartup { get; set; } = false;

    /// <summary>
    /// Starts the app hidden until the taskbar or tray icon is clicked.
    /// </summary>
    public bool StartHidden { get; set; } = false;

    /// <summary>
    /// Allows moving the clock by dragging it with the cursor.
    /// </summary>
    public bool DragToMove { get; set; } = true;

    /// <summary>
    /// Makes the clock ignore mouse clicks so you can interact with windows underneath.
    /// </summary>
    public bool ClickThrough { get; set; } = false;

    /// <summary>
    /// Experimental: Keeps the clock window aligned to the right when the size changes.
    /// </summary>
    /// <remarks>
    /// Small glitches can happen because programs are naturally meant to be left-anchored.
    /// </remarks>
    public bool RightAligned { get; set; } = false;

    /// <summary>
    /// Experimental: Shifts the clock periodically in order to reduce screen burn-in.
    /// </summary>
    public bool BurnInMitigation { get; set; } = false;

    /// <summary>
    /// Path to a WAV file to be played on a specified interval.
    /// </summary>
    public string WavFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Interval for playing the WAV file if one is specified and exists (HH:mm:ss).
    /// </summary>
    public TimeSpan WavFileInterval { get; set; }

    /// <summary>
    /// Play the WAV file when the countdown time elapses.
    /// </summary>
    public bool PlaySoundOnCountdown { get; set; } = true;

    /// <summary>
    /// The index of the selected tab in the settings window.
    /// </summary>
    public int SettingsTabIndex { get; set; }

    /// <summary>
    /// Teaching tips that have already been shown to the user.
    /// </summary>
    public TeachingTips TipsShown { get; set; }

    /// <summary>
    /// The last text shown on the clock, saved to maintain the dimensions on the next launch.
    /// </summary>
    public string LastDisplay { get; set; }

    /// <summary>
    /// Window placement settings to preserve the location of the clock on the screen.
    /// </summary>
    public WindowPlacement Placement { get; set; }

    /// <summary>
    /// Proxy for binding to the a timezone.
    /// </summary>
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

    private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (!FollowSystemTheme)
            return;

        if (e.Category == UserPreferenceCategory.General ||
            e.Category == UserPreferenceCategory.Color ||
            e.Category == UserPreferenceCategory.VisualStyle)
        {
            RefreshSystemThemeColors(notifyIfFollowing: true);
        }
    }

    private void SystemParameters_StaticPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!FollowSystemTheme)
            return;

        if (e.PropertyName == nameof(SystemParameters.WindowGlassColor))
        {
            RefreshSystemThemeColors(notifyIfFollowing: true);
        }
    }

    private void RefreshSystemThemeColors(bool notifyIfFollowing)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(new Action(() => RefreshSystemThemeColors(notifyIfFollowing)));
            return;
        }

        var isLightTheme = GetSystemThemeIsLight();
        var accentColor = GetSystemAccentColor();
        var themeChanged = isLightTheme != _systemThemeIsLight;
        var accentChanged = !_systemAccentColor.Equals(accentColor);

        _systemThemeIsLight = isLightTheme;
        _systemAccentColor = accentColor;

        if (notifyIfFollowing && (themeChanged || accentChanged) && FollowSystemTheme)
        {
            NotifyEffectiveThemeChanged();
        }
    }

    private static bool GetSystemThemeIsLight()
    {
        var value = Registry.GetValue(PersonalizeKeyPath, AppsUseLightThemeValueName, 1);
        return value switch
        {
            int intValue => intValue > 0,
            byte byteValue => byteValue > 0,
            _ => true,
        };
    }

    private static Color GetSystemAccentColor()
    {
        var accent = SystemParameters.WindowGlassColor;
        if (accent.A == 0)
            return DefaultAccentColor;

        return Color.FromArgb(255, accent.R, accent.G, accent.B);
    }

    private void NotifyEffectiveThemeChanged()
    {
        NotifyPropertyChanged(nameof(EffectiveTextColor));
        NotifyPropertyChanged(nameof(EffectiveOuterColor));
    }

    private void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        // We don't dispose of the watcher anymore because it would actually hang indefinitely if you had multiple instances of the same clock open.
        //_watcher?.Dispose();
    }
}
