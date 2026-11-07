using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace DesktopClock;

public static class FileUtil
{
    /// <summary>
    /// Returns an indexed version of the filename, in the same folder, that doesn't exist yet.
    /// </summary>
    public static FileInfo GetFileAtNextIndex(this FileInfo fileInfo)
    {
        var i = 1;
        FileInfo file;
        do
        {
            i++;
            var baseName = Path.GetFileNameWithoutExtension(fileInfo.FullName);

            // Build the full path; a bare name would be checked against the working directory, which is System32 when Windows starts the app at sign-in.
            file = new FileInfo(Path.Combine(fileInfo.DirectoryName, $"{baseName}-{i}{fileInfo.Extension}"));
        } while (file.Exists);

        return file;
    }

    /// <summary>
    /// Whether the file is in the temp folder, which is where Windows puts an app opened straight from inside a zip, in a new folder each time.
    /// </summary>
    public static bool IsInTempFolder(this FileInfo fileInfo)
    {
        var tempFolder = GetLongPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return GetLongPath(fileInfo.FullName).StartsWith(tempFolder, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Expands short names like DANIEL~1, which the temp folder's path is sometimes given in.
    /// </summary>
    private static string GetLongPath(string path)
    {
        var longPath = new StringBuilder(32767);
        return GetLongPathName(path, longPath, longPath.Capacity) > 0 ? longPath.ToString() : path;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetLongPathName(string shortPath, StringBuilder longPath, int size);
}
