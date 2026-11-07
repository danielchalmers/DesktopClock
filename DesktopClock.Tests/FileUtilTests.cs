using System;
using System.IO;

namespace DesktopClock.Tests;

public class FileUtilTests
{
    [Fact]
    public void GetFileAtNextIndex_ShouldSkipTakenNamesInTheFilesFolderRegardlessOfWorkingDirectory()
    {
        var folder = Path.Combine(Path.GetTempPath(), "DesktopClock.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var originalWorkingDirectory = Environment.CurrentDirectory;

        try
        {
            var exe = new FileInfo(Path.Combine(folder, "DesktopClock.exe"));
            File.WriteAllText(exe.FullName, "");
            File.WriteAllText(Path.Combine(folder, "DesktopClock-2.exe"), "");

            // Windows starts the app from System32 at sign-in, not from its own folder.
            Environment.CurrentDirectory = Environment.SystemDirectory;

            var next = exe.GetFileAtNextIndex();

            Assert.Equal(Path.Combine(folder, "DesktopClock-3.exe"), next.FullName);
        }
        finally
        {
            Environment.CurrentDirectory = originalWorkingDirectory;
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void IsInTempFolder_ShouldMatchACopyOpenedFromInsideAZip()
    {
        // Windows opens an app inside a zip from a new folder like this one.
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("D") + "_DesktopClock.zip.123");
        Directory.CreateDirectory(folder);

        try
        {
            var exe = new FileInfo(Path.Combine(folder, "DesktopClock.exe"));
            File.WriteAllText(exe.FullName, "");

            Assert.True(exe.IsInTempFolder());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void IsInTempFolder_ShouldNotMatchAFolderNextToIt()
    {
        // A folder whose name starts with the temp folder's isn't inside it.
        var temp = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
        var exe = new FileInfo(temp + "Clock" + Path.DirectorySeparatorChar + "DesktopClock.exe");

        Assert.False(exe.IsInTempFolder());
    }
}
