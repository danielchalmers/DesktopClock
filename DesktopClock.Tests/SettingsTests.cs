using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using DesktopClock.Properties;

namespace DesktopClock.Tests;

public class SettingsPersistenceTests
{
    [Fact]
    public void Save_ThenPopulate_ShouldRoundTripKeyProperties()
    {
        using var _ = new TempSettingsFileScope();

        var original = CreateSettingsInstance();
        original.FontFamily = "My custom font";
        original.Height = 72;
        original.Topmost = false;
        original.Format = "{HH:mm:ss}";
        original.RunOnStartup = true;
        original.ClickThrough = true;
        original.SettingsWindowWidth = 840;
        original.SettingsWindowHeight = 640;
        original.SettingsScrollPosition = 128;

        var saved = original.Save();

        Assert.True(saved);
        Assert.True(File.Exists(Settings.FilePath));

        var loaded = CreateSettingsInstance();
        PopulateFromFile(loaded);

        Assert.Equal(original.FontFamily, loaded.FontFamily);
        Assert.Equal(original.Height, loaded.Height);
        Assert.Equal(original.Topmost, loaded.Topmost);
        Assert.Equal(original.Format, loaded.Format);
        Assert.Equal(original.RunOnStartup, loaded.RunOnStartup);
        Assert.Equal(original.ClickThrough, loaded.ClickThrough);
        Assert.Equal(original.SettingsWindowWidth, loaded.SettingsWindowWidth);
        Assert.Equal(original.SettingsWindowHeight, loaded.SettingsWindowHeight);
        Assert.Equal(original.SettingsScrollPosition, loaded.SettingsScrollPosition);
    }

    [Fact]
    public void Save_ThenPopulate_ShouldRoundTripWpfAndTimeTypes()
    {
        using var _ = new TempSettingsFileScope();

        var original = CreateSettingsInstance();
        original.TextColor = Color.FromArgb(0xFF, 0x12, 0x34, 0x56);
        original.OuterColor = Color.FromRgb(0xAB, 0xCD, 0xEF);
        original.BackgroundImageStretch = Stretch.UniformToFill;
        original.WavFileInterval = new TimeSpan(1, 15, 0);
        original.CountdownTo = new DateTime(2027, 3, 14, 9, 26, 53);

        Assert.True(original.Save());

        var loaded = CreateSettingsInstance();
        PopulateFromFile(loaded);

        Assert.Equal(original.TextColor, loaded.TextColor);
        Assert.Equal(original.OuterColor, loaded.OuterColor);
        Assert.Equal(original.BackgroundImageStretch, loaded.BackgroundImageStretch);
        Assert.Equal(original.WavFileInterval, loaded.WavFileInterval);
        Assert.Equal(original.CountdownTo, loaded.CountdownTo);

        // Countdown targets are local wall-clock times; the formatter relies on the Kind staying Unspecified.
        Assert.Equal(DateTimeKind.Unspecified, loaded.CountdownTo.Kind);
    }

    private static Settings CreateSettingsInstance() =>
        (Settings)Activator.CreateInstance(typeof(Settings), nonPublic: true)!;

    private static void PopulateFromFile(Settings settings)
    {
        var populateMethod = typeof(Settings).GetMethod(
            "Populate",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(populateMethod);
        populateMethod.Invoke(null, new object[] { settings });
    }

    private sealed class TempSettingsFileScope : IDisposable
    {
        private readonly string _originalFilePath;

        public TempSettingsFileScope()
        {
            _originalFilePath = Settings.FilePath;

            var tempDir = Path.Combine(Path.GetTempPath(), "DesktopClock.Tests");
            Directory.CreateDirectory(tempDir);
            var tempFilePath = Path.Combine(tempDir, $"{Guid.NewGuid():N}.settings");

            SetSettingsFilePath(tempFilePath);

            if (File.Exists(Settings.FilePath))
                File.Delete(Settings.FilePath);
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(Settings.FilePath))
                    File.Delete(Settings.FilePath);
            }
            finally
            {
                SetSettingsFilePath(_originalFilePath);
            }
        }

        private static void SetSettingsFilePath(string filePath)
        {
            var filePathProperty = typeof(Settings).GetProperty(
                nameof(Settings.FilePath),
                BindingFlags.Public | BindingFlags.Static);

            Assert.NotNull(filePathProperty);

            var setMethod = filePathProperty.GetSetMethod(nonPublic: true);
            Assert.NotNull(setMethod);
            setMethod.Invoke(null, new object[] { filePath });
        }
    }
}
