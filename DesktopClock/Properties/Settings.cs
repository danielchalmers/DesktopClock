using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using Newtonsoft.Json;
using WpfWindowPlacement;

namespace DesktopClock.Properties;

public sealed class Settings : INotifyPropertyChanged, IDisposable
{
    private readonly FileSystemWatcher _watcher;

    private static readonly Lazy<Settings> _default = new(LoadAndAttemptSave);

    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented,
        Error = (_, e) => e.ErrorContext.Handled = true,
    };

    private Settings()
    {
        // Settings file path from same directory as the executable.
        var settingsFileName = Path.GetFileNameWithoutExtension(App.MainFileInfo.FullName) + ".settings";
        FilePath = Path.Combine(App.MainFileInfo.DirectoryName, settingsFileName);

        // Watch for changes.
        _watcher = new(App.MainFileInfo.DirectoryName, settingsFileName)
        {
            EnableRaisingEvents = true,
        };
        _watcher.Changed += FileChanged;

        // Random default theme before getting overwritten.
        Theme = Theme.GetRandomDefaultTheme();
    }

#pragma warning disable CS0067 // The event 'Settings.PropertyChanged' is never used
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067 // The event 'Settings.PropertyChanged' is never used

    public static Settings Default => _default.Value;

    /// <summary>
    /// The full path to the settings file.
    /// </summary>
    public static string FilePath { get; private set; }

    /// <summary>
    /// Can the settings file be saved to?
    /// </summary>
    public static bool CanBeSaved { get; private set; }

    /// <summary>
    /// Does the settings file exist on the disk?
    /// </summary>
    public static bool Exists => File.Exists(FilePath);

    #region "Properties"

    public string Format { get; set; } = "{ddd}, {MMM dd}, {h:mm:ss tt}";
    public string CountdownFormat { get; set; } = "";
    public DateTime? CountdownTo { get; set; } = default(DateTime);
    public string TimeZone { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Consolas";
    public Color TextColor { get; set; }
    public Color OuterColor { get; set; }
    public bool BackgroundEnabled { get; set; } = true;
    public double BackgroundOpacity { get; set; } = 0.90;
    public double BackgroundCornerRadius { get; set; } = 1;
    public double OutlineThickness { get; set; } = 0.2;
    public bool Topmost { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = true;
    public int Height { get; set; } = 48;
    public bool RunOnStartup { get; set; } = false;
    public bool DragToMove { get; set; } = true;
    public bool RightAligned { get; set; } = false;
    public TeachingTips TipsShown { get; set; }
    public WindowPlacement Placement { get; set; }

    [JsonIgnore]
    public Theme Theme
    {
        get => new("Custom", TextColor.ToString(), OuterColor.ToString());
        set
        {
            TextColor = (Color)ColorConverter.ConvertFromString(value.PrimaryColor);
            OuterColor = (Color)ColorConverter.ConvertFromString(value.SecondaryColor);
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

    public void Dispose()
    {
        _watcher?.Dispose();
    }
}