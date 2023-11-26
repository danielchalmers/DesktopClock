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
        Error = (_, e) =>
        {
            e.ErrorContext.Handled = true;
        },
    };

    private Settings()
    {
        // Settings file path from same directory as the executable.
        var settingsFileName = Path.GetFileNameWithoutExtension(App.MainFileInfo.FullName) + ".settings";
        FilePath = Path.Combine(App.MainFileInfo.DirectoryName, settingsFileName);

        // Watch for changes.
        _watcher = new(App.MainFileInfo.DirectoryName, settingsFileName)
        {
            EnableRaisingEvents = true
        };
        _watcher.Changed += FileChanged;

        // Random default theme.
        var random = new Random();
        Theme = App.Themes[random.Next(0, App.Themes.Count)];
    }

#pragma warning disable CS0067 // The event 'Settings.PropertyChanged' is never used
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067 // The event 'Settings.PropertyChanged' is never used

    public static Settings Default => _default.Value;

    public static string FilePath { get; private set; }

    public static bool CanBeSaved { get; private set; }

    #region "Properties"

    public string Format { get; set; } = "{dddd}, {MMM dd}, {HH:mm:ss}";
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
    /// Saves to the default path.
    /// </summary>
    public void Save()
    {
        using var fileStream = new FileStream(FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
        using var streamWriter = new StreamWriter(fileStream);
        using var jsonWriter = new JsonTextWriter(streamWriter);

        JsonSerializer.Create(_jsonSerializerSettings).Serialize(jsonWriter, this);
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

    private static Settings LoadAndAttemptSave()
    {
        var settings = LoadFromFile();

        try
        {
            settings.Save();

            CanBeSaved = true;
        }
        catch (UnauthorizedAccessException)
        {
            CanBeSaved = false;
        }
        catch (IOException)
        {
            CanBeSaved = false;
        }

        return settings;
    }

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