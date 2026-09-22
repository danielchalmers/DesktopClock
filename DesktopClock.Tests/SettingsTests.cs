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

        using var __ = new CanBeSavedScope();

        var settings = CreateSettingsInstance();
        settings.Format = "saved without exiting";

        // The save runs on a short timer, so let the dispatcher run for a bit.
        PumpDispatcher(TimeSpan.FromSeconds(2));

        // Read the file directly; populating another instance here would queue its own save and leak into other tests.
        Assert.Contains("saved without exiting", File.ReadAllText(Settings.FilePath));
    }

    [Fact]
    public void EditingTheFile_ShouldCancelASaveStillWaitingFromAnEarlierChange()
    {
        using var _ = new TempSettingsFileScope();
        using var __ = new CanBeSavedScope();

        var settings = CreateSettingsInstance();
        settings.Height = 99;

        // Edit the file by hand before that change is saved, then let the watcher report it.
        const string handEdit = "{ \"Format\": \"edited by hand\" }";
        File.WriteAllText(Settings.FilePath, handEdit);
        typeof(Settings).GetMethod("FileChanged", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(settings, new object[] { null, null });
        PumpDispatcher(TimeSpan.FromSeconds(2));

        Assert.Equal("edited by hand", settings.Format);
        Assert.Equal(handEdit, File.ReadAllText(Settings.FilePath));
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

        var loaded = LoadAndAttemptSave();
        releaser.Join();

        Assert.Equal("kept through a lock", loaded.Format);
    }

    /// <summary>
    /// Runs the app's startup load, restoring the static state it sets so other tests aren't affected.
    /// </summary>
    private static Settings LoadAndAttemptSave()
    {
        var canBeSaved = typeof(Settings).GetProperty(nameof(Settings.CanBeSaved), BindingFlags.Public | BindingFlags.Static)!.GetSetMethod(nonPublic: true)!;
        var originalCanBeSaved = Settings.CanBeSaved;

        try
        {
            canBeSaved.Invoke(null, new object[] { false });

            var loadAndAttemptSave = typeof(Settings).GetMethod("LoadAndAttemptSave", BindingFlags.NonPublic | BindingFlags.Static)!;
            return (Settings)loadAndAttemptSave.Invoke(null, null)!;
        }
        finally
        {
            canBeSaved.Invoke(null, new object[] { originalCanBeSaved });
        }
    }

    private static void PumpDispatcher(TimeSpan duration)
    {
        var frame = new DispatcherFrame();
        var stopTimer = new DispatcherTimer { Interval = duration };
        stopTimer.Tick += (_, _) =>
        {
            stopTimer.Stop();
            frame.Continue = false;
        };
        stopTimer.Start();
        Dispatcher.PushFrame(frame);
    }

    /// <summary>
    /// Lets settings save on their own during a test, as they do once the app has confirmed the file is writable.
    /// </summary>
    private sealed class CanBeSavedScope : IDisposable
    {
        private static readonly MethodInfo _setCanBeSaved = typeof(Settings).GetProperty(nameof(Settings.CanBeSaved), BindingFlags.Public | BindingFlags.Static)!.GetSetMethod(nonPublic: true)!;
        private readonly bool _original = Settings.CanBeSaved;

        public CanBeSavedScope() => _setCanBeSaved.Invoke(null, new object[] { true });

        public void Dispose() => _setCanBeSaved.Invoke(null, new object[] { _original });
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
