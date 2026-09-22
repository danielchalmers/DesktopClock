using System.IO;

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
}
