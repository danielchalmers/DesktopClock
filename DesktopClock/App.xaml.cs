using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace DesktopClock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static FileInfo MainFileInfo = new(Process.GetCurrentProcess().MainModule.FileName);

    /// <summary>
    /// Sets or deletes a value in the registry which enables the current executable to run on system startup.
    /// </summary>
    public static void SetRunOnStartup(bool runOnStartup)
    {
        static string GetSha256Hash(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            using var sha = new System.Security.Cryptography.SHA256Managed();
            var textData = System.Text.Encoding.UTF8.GetBytes(text);
            var hash = sha.ComputeHash(textData);
            return BitConverter.ToString(hash).Replace("-", string.Empty);
        }

        var keyName = GetSha256Hash(MainFileInfo.FullName);
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        if (runOnStartup)
            key?.SetValue(keyName, MainFileInfo.FullName); // Use the path as the name so we can handle multiple exes, but hash it or Windows won't like it.
        else
            key?.DeleteValue(keyName, false);
    }
}
