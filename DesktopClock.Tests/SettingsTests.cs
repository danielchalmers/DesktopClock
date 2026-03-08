using System;
using System.IO;
using System.Reflection;
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
    public void ScaleHeight_ShouldAdjustAndClamp()
    {
        var settings = CreateSettingsInstance();

        settings.Height = 48;
        settings.ScaleHeight(2);
        Assert.Equal(64, settings.Height);

        settings.ScaleHeight(-2);
        Assert.Equal(47, settings.Height);

        var minHeight = (int)Math.Exp(Settings.MinSizeLog);
        var maxHeight = (int)Math.Exp(Settings.MaxSizeLog);

        settings.Height = 48;
        settings.ScaleHeight(10_000);
        Assert.Equal(maxHeight, settings.Height);

        settings.Height = 48;
        settings.ScaleHeight(-10_000);
        Assert.Equal(minHeight, settings.Height);
    }

    [Fact]
    public void FileChanged_WhenSuppressed_ShouldNotPopulateFromDisk()
    {
        using var _ = new TempSettingsFileScope();

        var settings = CreateSettingsInstance();
        settings.FontFamily = "InMemory";
        File.WriteAllText(Settings.FilePath, "{\"FontFamily\":\"OnDisk\"}");

        SetSuppressFileChangedEvents(settings, 1);
        InvokeFileChanged(settings);

        Assert.Equal("InMemory", settings.FontFamily);
    }

    [Fact]
    public void FileChanged_WhenNotSuppressed_ShouldPopulateFromDisk()
    {
        using var _ = new TempSettingsFileScope();

        var settings = CreateSettingsInstance();
        settings.FontFamily = "InMemory";
        File.WriteAllText(Settings.FilePath, "{\"FontFamily\":\"OnDisk\"}");

        InvokeFileChanged(settings);

        Assert.Equal("OnDisk", settings.FontFamily);
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

    private static void InvokeFileChanged(Settings settings)
    {
        var fileChangedMethod = typeof(Settings).GetMethod(
            "FileChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(fileChangedMethod);
        fileChangedMethod.Invoke(settings, new object[]
        {
            settings,
            new FileSystemEventArgs(
                WatcherChangeTypes.Changed,
                Path.GetDirectoryName(Settings.FilePath)!,
                Path.GetFileName(Settings.FilePath)),
        });
    }

    private static void SetSuppressFileChangedEvents(Settings settings, int value)
    {
        var suppressField = typeof(Settings).GetField(
            "_suppressFileChangedEvents",
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(suppressField);
        suppressField.SetValue(settings, value);
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
