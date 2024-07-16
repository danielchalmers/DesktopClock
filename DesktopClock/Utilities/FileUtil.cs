using System.IO;

namespace DesktopClock;

public static class FileUtil
{
    /// <summary>
    /// Returns an indexed version of the filename that doesn't exist yet.
    /// </summary>
    public static FileInfo GetFileAtNextIndex(this FileInfo fileInfo)
    {
        var i = 1;
        FileInfo file;
        do
        {
            i++;
            var baseName = Path.GetFileNameWithoutExtension(fileInfo.FullName);
            file = new FileInfo($"{baseName}-{i}{fileInfo.Extension}");
        } while (file.Exists);

        return file;
    }
}
