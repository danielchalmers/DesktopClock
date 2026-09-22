using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Threading;
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

    [Fact]
    public void Save_OverExistingFile_ShouldReplaceItWithoutLeavingTempFile()
    {
        using var _ = new TempSettingsFileScope();

        var settings = CreateSettingsInstance();
        settings.Format = "first";
        Assert.True(settings.Save());

        settings.Format = "second";
        Assert.True(settings.Save());

        var loaded = CreateSettingsInstance();
        PopulateFromFile(loaded);

        Assert.Equal("second", loaded.Format);
        Assert.False(File.Exists(Settings.FilePath + ".tmp"));
    }

    [Fact]
    public void ChangingASetting_ShouldSaveItShortlyAfterwards()
    {
        using var _ = new TempSettingsFileScope();

        var canBeSavedProperty = typeof(Settings).GetProperty(nameof(Settings.CanBeSaved), BindingFlags.Public | BindingFlags.Static)!;
        var originalCanBeSaved = Settings.CanBeSaved;
        canBeSavedProperty.GetSetMethod(nonPublic: true)!.Invoke(null, new object[] { true });

        try
        {
            var settings = CreateSettingsInstance();
            settings.Format = "saved without exiting";

            // The save runs on a short timer, so let the dispatcher run for a bit.
            var frame = new DispatcherFrame();
            var stopTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            stopTimer.Tick += (_, _) =>
            {
                stopTimer.Stop();
                frame.Continue = false;
            };
            stopTimer.Start();
            Dispatcher.PushFrame(frame);

            var loaded = CreateSettingsInstance();
            PopulateFromFile(loaded);

            Assert.Equal("saved without exiting", loaded.Format);
        }
        finally
        {
            canBeSavedProperty.GetSetMethod(nonPublic: true)!.Invoke(null, new object[] { originalCanBeSaved });
        }
    }

    [Fact]
    public void Load_WithUnreadableFile_ShouldMoveItToBackupAndUseDefaults()
    {
        using var _ = new TempSettingsFileScope();

        // A missing comma, like a typo made while editing the file by hand.
        const string brokenJson = "{ \"Format\": \"{HH:mm}\" \"FontFamily\": \"Georgia\" }";
        File.WriteAllText(Settings.FilePath, brokenJson);

        var loaded = LoadAndAttemptSave(out var movedToBackup);

        Assert.True(movedToBackup);
        Assert.Equal(brokenJson, File.ReadAllText(Settings.BackupFilePath));
        Assert.Equal(CreateSettingsInstance().Format, loaded.Format);
        Assert.True(File.Exists(Settings.FilePath));
    }

    [Fact]
    public void Load_WithEmptyFile_ShouldUseDefaultsWithoutBackup()
    {
        using var _ = new TempSettingsFileScope();

        File.WriteAllText(Settings.FilePath, "");

        var loaded = LoadAndAttemptSave(out var movedToBackup);

        Assert.False(movedToBackup);
        Assert.False(File.Exists(Settings.BackupFilePath));
        Assert.Equal(CreateSettingsInstance().Format, loaded.Format);
        Assert.NotEqual(0, new FileInfo(Settings.FilePath).Length);
    }

    [Fact]
    public void Load_WithBrieflyLockedFile_ShouldWaitAndKeepSettings()
    {
        using var _ = new TempSettingsFileScope();

        var original = CreateSettingsInstance();
        original.Format = "kept through a lock";
        Assert.True(original.Save());

        // Hold the file like antivirus scanning it, then let go shortly after loading starts.
        var lockStream = new FileStream(Settings.FilePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var releaser = new System.Threading.Thread(() =>
        {
            System.Threading.Thread.Sleep(150);
            lockStream.Dispose();
        });
        releaser.Start();

        var loaded = LoadAndAttemptSave(out var movedToBackup);
        releaser.Join();

        Assert.False(movedToBackup);
        Assert.Equal("kept through a lock", loaded.Format);
    }

    /// <summary>
    /// Runs the app's startup load, restoring the static state it sets so other tests aren't affected.
    /// </summary>
    private static Settings LoadAndAttemptSave(out bool movedToBackup)
    {
        var canBeSaved = typeof(Settings).GetProperty(nameof(Settings.CanBeSaved), BindingFlags.Public | BindingFlags.Static)!.GetSetMethod(nonPublic: true)!;
        var movedUnreadableFileToBackup = typeof(Settings).GetProperty(nameof(Settings.MovedUnreadableFileToBackup), BindingFlags.Public | BindingFlags.Static)!.GetSetMethod(nonPublic: true)!;
        var originalCanBeSaved = Settings.CanBeSaved;

        try
        {
            canBeSaved.Invoke(null, new object[] { false });
            movedUnreadableFileToBackup.Invoke(null, new object[] { false });

            var loadAndAttemptSave = typeof(Settings).GetMethod("LoadAndAttemptSave", BindingFlags.NonPublic | BindingFlags.Static)!;
            var settings = (Settings)loadAndAttemptSave.Invoke(null, null)!;
            movedToBackup = Settings.MovedUnreadableFileToBackup;
            return settings;
        }
        finally
        {
            canBeSaved.Invoke(null, new object[] { originalCanBeSaved });
            movedUnreadableFileToBackup.Invoke(null, new object[] { false });
        }
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

                if (File.Exists(Settings.BackupFilePath))
                    File.Delete(Settings.BackupFilePath);
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
