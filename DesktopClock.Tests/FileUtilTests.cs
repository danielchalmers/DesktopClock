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
}
