using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using DesktopClock.Utilities;
using Microsoft.Win32;

namespace DesktopClock;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static FileInfo MainFileInfo = new(Process.GetCurrentProcess().MainModule.FileName);
    public static string MainFileDisplayText = $"{Path.GetFileNameWithoutExtension(MainFileInfo.Name)} {FileVersionInfo.GetVersionInfo(MainFileInfo.FullName).FileVersion}";

    static App()
    {
        // WPF treats all text as en-US unless told otherwise, which draws Chinese with a mix of Japanese and Traditional Chinese fallback fonts. Text runs (TextElement) don't inherit a FrameworkElement default, so they need the same override. This must run before any element is created.
        var language = XmlLanguage.GetLanguage(TextCulture().IetfLanguageTag);
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(language));
        FrameworkContentElement.LanguageProperty.OverrideMetadata(typeof(TextElement), new FrameworkPropertyMetadata(language));
    }

    /// <summary>
    /// The language on-screen text is in, which decides the fallback fonts for Chinese, Japanese, and Korean: the language of the translation being shown, unless only the regional format is East Asian, because then the East Asian text on screen is the dates and times.
    /// </summary>
    internal static CultureInfo TextCulture()
    {
        var display = CultureInfo.CurrentUICulture;
        var translation = Loc.TranslationLanguage(display);
        if (translation != display.TwoLetterISOLanguageName)
            display = CultureInfo.GetCultureInfo(translation);

        return !IsEastAsian(display) && IsEastAsian(CultureInfo.CurrentCulture) ? CultureInfo.CurrentCulture : display;
    }

    private static bool IsEastAsian(CultureInfo culture) => culture.TwoLetterISOLanguageName is "zh" or "ja" or "ko";

    protected override void OnStartup(StartupEventArgs e)
    {
        CrashHandler.Register(this);
        base.OnStartup(e);
        ThemeManager.Initialize();

        // Warm the settings window's system lists once the clock is up and idle, so opening settings later doesn't stall on enumerating them.
        Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            new Action(SettingsWindowViewModel.PrefetchSystemLists));
    }

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

        // Use the path as the name so we can handle multiple exes, but hash it or Windows won't like it.
        var keyName = GetSha256Hash(MainFileInfo.FullName);
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

        if (runOnStartup)
            key?.SetValue(keyName, MainFileInfo.FullName);
        else
            key?.DeleteValue(keyName, false);
    }

    /// <summary>
    /// Shows a singleton window of the specified type.
    /// If the window is already open, it activates the existing window.
    /// Otherwise, it creates and shows a new instance of the window.
    /// </summary>
    /// <typeparam name="T">The type of the window to show.</typeparam>
    /// <param name="owner">The owner window for the singleton window.</param>
    public static void ShowSingletonWindow<T>(Window owner) where T : Window, new()
    {
        var window = Current.Windows.OfType<T>().FirstOrDefault() ?? new T();
        window.Owner = owner;

        // Restore an existing window.
        if (window.IsVisible)
        {
            SystemCommands.RestoreWindow(window);
            window.Activate();
            return;
        }

        // Show the new window.
        window.Show();
    }
}
