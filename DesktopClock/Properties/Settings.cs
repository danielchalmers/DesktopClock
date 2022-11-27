using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using Newtonsoft.Json;
using WpfWindowPlacement;

namespace DesktopClock.Properties;

public sealed class Settings : INotifyPropertyChanged
{
    private DateTime _fileLastUsed = DateTime.UtcNow;

    public static readonly string Path = GetSettingsPath();
    private static readonly Lazy<Settings> _default = new(() => TryLoad() ?? new Settings());

    private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        Formatting = Formatting.Indented
    };

    private Settings()
    {
        var random = new Random();

        // Random default theme.
        Theme = App.Themes[random.Next(0, App.Themes.Count)];
    }

#pragma warning disable CS0067 // The event 'Settings.PropertyChanged' is never used
    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning restore CS0067 // The event 'Settings.PropertyChanged' is never used

    public static Settings Default => _default.Value;

    #region "Properties"

    public DateTimeOffset CountdownTo { get; set; } = DateTimeOffset.MinValue;
    public string Format { get; set; } = "dddd, MMM dd, HH:mm:ss";
    public string TimeZone { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Consolas";
    public Color TextColor { get; set; }
    public Color OuterColor { get; set; }
    public bool BackgroundEnabled { get; set; } = true;
    public double BackgroundOpacity { get; set; } = 0.90;
    public bool Topmost { get; set; } = true;
    public bool ShowInTaskbar { get; set; } = true;
    public int Height { get; set; } = 48;
    public bool RunOnStartup { get; set; } = false;
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
    /// Determines if the settings file has been modified externally since the last time it was used.
    /// </summary>
    public bool CheckIfModifiedExternally() =>
        File.GetLastWriteTimeUtc(Path) > _fileLastUsed;

    /// <summary>
    /// Saves to the default path.
    /// </summary>
    public void Save()
    {
        using (var fileStream = new FileStream(Path, FileMode.Create))
        using (var streamWriter = new StreamWriter(fileStream))
        using (var jsonWriter = new JsonTextWriter(streamWriter))
            JsonSerializer.Create(_jsonSerializerSettings).Serialize(jsonWriter, this);

        _fileLastUsed = DateTime.UtcNow;
    }

    /// <summary>
    /// Saves to the default path unless a save has already happened from an external source.
    /// </summary>
    public void SaveIfNotModifiedExternally()
    {
        if (!CheckIfModifiedExternally())
            Save();
    }

    /// <summary>
    /// Loads from the default path.
    /// </summary>
    private static Settings Load()
    {
        using var fileStream = new FileStream(Path, FileMode.Open);
        using var streamReader = new StreamReader(fileStream);
        using var jsonReader = new JsonTextReader(streamReader);

        return JsonSerializer.Create(_jsonSerializerSettings).Deserialize<Settings>(jsonReader);
    }

    /// <summary>
    /// Returns loaded settings from the default path or null if it fails.
    /// </summary>
    private static Settings TryLoad()
    {
        try
        {
            return Load();
        }
        catch
        {
            return null;
        }
    }

    private static string GetSettingsPath()
    {
        var exeInfo = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location);
        var exeNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(exeInfo.FullName);
        return System.IO.Path.Combine(exeInfo.DirectoryName, exeNameWithoutExtension + ".settings");
    }
}