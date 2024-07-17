using System;
using System.IO;
using System.Threading.Tasks;
using DesktopClock.Properties;

namespace DesktopClock.Tests;

public class SettingsTests : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        // Ensure the settings file does not exist before each test.
        if (File.Exists(Settings.FilePath))
        {
            File.Delete(Settings.FilePath);
        }

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        // Clean up.
        if (File.Exists(Settings.FilePath))
        {
            File.Delete(Settings.FilePath);
        }

        return Task.CompletedTask;
    }

    [Fact(Skip = "File path is unauthorized")]
    public void Save_ShouldWriteSettingsToFile()
    {
        var settings = Settings.Default;
        settings.FontFamily = "My custom font";

        var result = settings.Save();

        Assert.True(result);
        Assert.True(File.Exists(Settings.FilePath));

        var savedSettings = File.ReadAllText(Settings.FilePath);
        Assert.Contains("My custom font", savedSettings);
    }

    [Fact(Skip = "File path is unauthorized")]
    public void LoadFromFile_ShouldPopulateSettings()
    {
        var json = "{\"FontFamily\": \"Consolas\"}";
        File.WriteAllText(Settings.FilePath, json);
        var settings = Settings.Default;
        Assert.Equal("My custom font", settings.FontFamily);
    }

    [Fact(Skip = "The process cannot access the file [...] because it is being used by another process.")]
    public void ScaleHeight_ShouldAdjustHeightByExpectedAmount()
    {
        var settings = Settings.Default;

        Assert.Equal(48, settings.Height);

        settings.ScaleHeight(2);

        Assert.Equal(64, settings.Height);

        settings.ScaleHeight(-2);

        Assert.Equal(47, settings.Height);
    }

    [Fact(Skip = "The process cannot access the file [...] because it is being used by another process.")]
    public void GetTimeZoneInfo_ShouldReturnExpectedTimeZoneInfo()
    {
        var settings = Settings.Default;

        // Default TimeZone should return Local
        var localTimeZone = TimeZoneInfo.Local;
        var timeZoneInfo = settings.GetTimeZoneInfo();
        Assert.Equal(localTimeZone, timeZoneInfo);

        // Set to a specific time zone
        settings.TimeZone = "Pacific Standard Time";
        timeZoneInfo = settings.GetTimeZoneInfo();
        Assert.Equal("Pacific Standard Time", timeZoneInfo.Id);
    }
}
