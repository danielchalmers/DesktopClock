using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Media;
using Newtonsoft.Json;
using WpfWindowPlacement;

namespace DesktopClock.Properties
{
    public sealed class Settings : INotifyPropertyChanged
    {
        private DateTime _fileLastUsed = DateTime.UtcNow;

        public static readonly string Path = "DesktopClock.settings";
        private static readonly Lazy<Settings> _default = new Lazy<Settings>(() => TryLoad() ?? new Settings());

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented
        };

        private Settings()
        {
            var random = new Random();

            // Random default theme.
            Theme = App.Themes[random.Next(0, App.Themes.Count)];
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static Settings Default => _default.Value;

        #region "Properties"

        public WindowPlacement Placement { get; set; }
        public bool Topmost { get; set; } = true;
        public bool ShowInTaskbar { get; set; } = true;
        public int Height { get; set; } = 48;
        public string TimeZone { get; set; } = string.Empty;
        public string Format { get; set; } = "dddd, MMM dd, HH:mm:ss";
        public bool BackgroundEnabled { get; set; } = false;
        public double BackgroundOpacity { get; set; } = 0.90;
        public Color OuterColor { get; set; }
        public Color TextColor { get; set; }
        public string FontFamily { get; set; } = "Arial";
        public DateTimeOffset CountdownTo { get; set; } = DateTimeOffset.MinValue;

        [JsonIgnore]
        public Theme Theme
        {
            get => new Theme("Custom", TextColor.ToString(), OuterColor.ToString());
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
        public void Save()
        {
            using (var fileStream = new FileStream(Path, FileMode.Create))
            using (var streamWriter = new StreamWriter(fileStream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
                JsonSerializer.Create(_jsonSerializerSettings).Serialize(jsonWriter, this);

            _fileLastUsed = DateTime.UtcNow;
        }

        /// <summary>
        /// Determines if the settings file has been modified externally since the last time it was used.
        /// </summary>
        public bool CheckIfModifiedExternally() =>
            File.GetLastWriteTimeUtc(Path) > _fileLastUsed;

        /// <summary>
        /// Loads from the default path in JSON format.
        /// </summary>
        private static Settings Load()
        {
            using (var fileStream = new FileStream(Path, FileMode.Open))
            using (var streamReader = new StreamReader(fileStream))
            using (var jsonReader = new JsonTextReader(streamReader))
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
    }
}