using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
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
    public void Save_OverReadOnlyFile_ShouldFailWithoutLeavingTempFile()
    {
        using var _ = new TempSettingsFileScope();

        var settings = CreateSettingsInstance();
        Assert.True(settings.Save());
        var file = new FileInfo(Settings.FilePath) { IsReadOnly = true };

        try
        {
            Assert.False(settings.Save());
            Assert.False(File.Exists(Settings.FilePath + ".tmp"));
        }
        finally
        {
            file.IsReadOnly = false;
        }
    }

    [Fact]
    public void Save_AfterTheFolderIsGone_ShouldFailWithoutThrowing()
    {
        // A clock on a USB stick that was unplugged: turning the file watcher back on after the failed save used to throw, which showed the crash message.
        var folder = Path.Combine(Path.GetTempPath(), "DesktopClock.Tests", Guid.NewGuid().ToString("N"));
        using var _ = new TempSettingsFileScope(folder);

        var settings = CreateSettingsInstance();
        Assert.True(settings.Save());

        Directory.Delete(folder, true);

        Assert.False(settings.Save());
    }

    [Fact]
    public void Load_InAFolderThatCantBeListed_ShouldStillWork()
    {
        // Some managed shares let users open files but not list the folder: watching it used to throw while the settings loaded, so the clock never appeared.
        var folder = Path.Combine(Path.GetTempPath(), "DesktopClock.Tests", Guid.NewGuid().ToString("N"));
        using var _ = new TempSettingsFileScope(folder);
        var denyList = new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.ListDirectory, AccessControlType.Deny);
        var security = Directory.GetAccessControl(folder);
        security.AddAccessRule(denyList);
        Directory.SetAccessControl(folder, security);

        try
        {
            var settings = CreateSettingsInstance();
            settings.Format = "saved without listing";

            Assert.True(settings.Save());
            Assert.Contains("saved without listing", File.ReadAllText(Settings.FilePath));
        }
        finally
        {
            security.RemoveAccessRule(denyList);
            Directory.SetAccessControl(folder, security);
        }
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

    [Fact]
    public void Load_WithFileLockedLongerThanItWaits_ShouldNotSaveDefaultsOverIt()
    {
        using var _ = new TempSettingsFileScope();

        var original = CreateSettingsInstance();
        original.Format = "kept through a long lock";
        Assert.True(original.Save());

        // Let go after loading gives up but while the startup save is still retrying, which is when the defaults used to replace the file.
        var lockStream = new FileStream(Settings.FilePath, FileMode.Open, FileAccess.Read, FileShare.None);
        var releaser = new System.Threading.Thread(() =>
        {
            System.Threading.Thread.Sleep(1100);
            lockStream.Dispose();
        });
        releaser.Start();

        LoadAndAttemptSave(out var canBeSaved);
        releaser.Join();

        Assert.Contains("kept through a long lock", File.ReadAllText(Settings.FilePath));
        Assert.False(canBeSaved);
    }

    [Theory]
    [InlineData("")] // Like issue #7, where the file was left empty by a shutdown.
    [InlineData("not settings")]
    public void Load_WithUnreadableFile_ShouldStartLikeAFirstRun(string contents)
    {
        using var _ = new TempSettingsFileScope();

        // A first run picks colors that match the system theme.
        var firstRun = LoadAndAttemptSave();
        if (DesktopClock.Utilities.SystemThemeService.TryGetThemeDefaults(out var textColor, out var outerColor))
        {
            Assert.Equal(textColor, firstRun.TextColor);
            Assert.Equal(outerColor, firstRun.OuterColor);
        }

        File.WriteAllText(Settings.FilePath, contents);

        var loaded = LoadAndAttemptSave();

        Assert.Equal(firstRun.TextColor, loaded.TextColor);
        Assert.Equal(firstRun.OuterColor, loaded.OuterColor);
    }

    /// <summary>
    /// Runs the app's startup load, restoring the static state it sets so other tests aren't affected.
    /// </summary>
    private static Settings LoadAndAttemptSave() => LoadAndAttemptSave(out _);

    /// <summary>
    /// Runs the app's startup load, restoring the static state it sets so other tests aren't affected, and reports whether it decided the file can be saved.
    /// </summary>
    private static Settings LoadAndAttemptSave(out bool canBeSaved)
    {
        var setCanBeSaved = typeof(Settings).GetProperty(nameof(Settings.CanBeSaved), BindingFlags.Public | BindingFlags.Static)!.GetSetMethod(nonPublic: true)!;
        var originalCanBeSaved = Settings.CanBeSaved;

        try
        {
            setCanBeSaved.Invoke(null, new object[] { false });

            var loadAndAttemptSave = typeof(Settings).GetMethod("LoadAndAttemptSave", BindingFlags.NonPublic | BindingFlags.Static)!;
            var settings = (Settings)loadAndAttemptSave.Invoke(null, null)!;
            canBeSaved = Settings.CanBeSaved;
            return settings;
        }
        finally
        {
            setCanBeSaved.Invoke(null, new object[] { originalCanBeSaved });
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

    [Theory]
    [InlineData(ClockLayer.AlwaysOnTop, true, false)]
    [InlineData(ClockLayer.Normal, false, false)]
    [InlineData(ClockLayer.PinToDesktop, false, true)]
    public void Layer_ShouldSetBothSwitches(ClockLayer layer, bool topmost, bool pinToDesktop)
    {
        var settings = CreateSettingsInstance();
        settings.Topmost = !topmost;
        settings.PinToDesktop = !pinToDesktop;

        settings.Layer = layer;

        Assert.Equal(topmost, settings.Topmost);
        Assert.Equal(pinToDesktop, settings.PinToDesktop);
        Assert.Equal(layer, settings.Layer);
    }

    [Fact]
    public void Layer_ShouldBeAlwaysOnTop_WhenAFileHasBothSwitchesOn()
    {
        var settings = CreateSettingsInstance();
        settings.Topmost = true;
        settings.PinToDesktop = true;

        Assert.Equal(ClockLayer.AlwaysOnTop, settings.Layer);
    }

    [Fact]
    public void Layer_ShouldNotify_WhenEitherSwitchChanges()
    {
        var settings = CreateSettingsInstance();
        settings.Topmost = true;
        var changed = new List<string>();
        settings.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        settings.Topmost = false;
        settings.PinToDesktop = true;

        Assert.Equal(2, changed.Count(name => name == nameof(Settings.Layer)));
    }

    [Fact]
    public void Save_ShouldKeepTheTwoSwitchesInsteadOfTheLayer()
    {
        using var _ = new TempSettingsFileScope();

        var settings = CreateSettingsInstance();
        settings.Layer = ClockLayer.PinToDesktop;

        Assert.True(settings.Save());

        var json = File.ReadAllText(Settings.FilePath);
        Assert.Contains("\"Topmost\": false", json);
        Assert.Contains("\"PinToDesktop\": true", json);
        Assert.DoesNotContain("\"Layer\"", json);
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
        private readonly string _ownFolder;

        /// <summary>
        /// Points settings at a new file in the shared test folder, or in the given folder, which is created now and deleted afterwards.
        /// </summary>
        public TempSettingsFileScope(string ownFolder = null)
        {
            _originalFilePath = Settings.FilePath;
            _ownFolder = ownFolder;

            var tempDir = ownFolder ?? Path.Combine(Path.GetTempPath(), "DesktopClock.Tests");
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

                if (_ownFolder != null && Directory.Exists(_ownFolder))
                    Directory.Delete(_ownFolder, true);
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
