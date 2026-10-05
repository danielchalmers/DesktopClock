using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text.RegularExpressions;

namespace DesktopClock.Tests;

public class LocalizationTests
{
    private const string ResourcePrefix = "DesktopClock.Localization.Strings.";

    private static readonly Regex PlaceholderRegex = new(@"\{[^{}]*\}");
    private static readonly Regex UrlRegex = new(@"https?://[A-Za-z0-9./_-]*[A-Za-z0-9]");

    // Alt access keys that share a window must not collide.
    private static readonly string[][] AccessKeyGroups =
    {
        ["MenuCopy", "MenuHide", "MenuAlwaysOnTop", "MenuSettings", "MenuCheckForUpdates", "MenuGiveFeedback", "MenuExit"],
        ["NavDisplay", "NavCountdown", "NavWindow", "NavThemePresets", "NavTypography", "NavAppearance", "NavAlerts", "NavShortcuts", "NavDiscoverMore", "NavCredits", "SettingsFolder", "SettingsFile", "NewClock"],
    };

    private static readonly Dictionary<string, string> English = ReadStrings("");

    public static IEnumerable<object[]> Translations =>
        typeof(Loc).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourcePrefix) && n != ResourcePrefix + "resources")
            .Select(n => new object[] { n.Substring(ResourcePrefix.Length, n.Length - ResourcePrefix.Length - ".resources".Length) });

    [Fact]
    public void Translations_AreEmbeddedInTheExe()
    {
        Assert.NotEmpty(Translations);
        Assert.Equal("_Copy", Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("en-US")));
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Translation_HasTheSameKeysAsEnglish(string culture)
    {
        var translation = ReadStrings(culture + ".");

        Assert.Empty(English.Keys.Except(translation.Keys));
        Assert.Empty(translation.Keys.Except(English.Keys));
        Assert.DoesNotContain(translation, t => string.IsNullOrWhiteSpace(t.Value));

        // Single words like "Terminal" can legitimately match English, but a whole phrase means it was never translated.
        Assert.Empty(translation.Where(t => t.Value == English[t.Key] && Regex.Matches(t.Value, @"[^\W\d_]{2,}").Count > 1).Select(t => t.Key));
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Translation_KeepsPlaceholdersAndLinks(string culture)
    {
        foreach (var translated in ReadStrings(culture + "."))
        {
            var english = English[translated.Key];
            var value = translated.Value;
            Assert.True(Placeholders(english).SetEquals(Placeholders(value)), $"{culture} {translated.Key}: placeholders changed in \"{value}\"");
            Assert.True(Urls(english).SetEquals(Urls(value)), $"{culture} {translated.Key}: links changed in \"{value}\"");

            // A stray brace makes Loc.Format throw, and these messages are shown from catch blocks and the crash handler.
            Assert.True(PlaceholderRegex.Replace(value, "").IndexOfAny(['{', '}']) < 0, $"{culture} {translated.Key}: stray brace in \"{value}\"");
            Assert.True(english.Count(c => c == '\n') == value.Count(c => c == '\n'), $"{culture} {translated.Key}: line breaks changed in \"{value}\"");
            Assert.True(value == value.Trim(), $"{culture} {translated.Key}: leading or trailing whitespace in \"{value}\"");
            Assert.True(value.IndexOfAny(['\u2013', '\u2014']) < 0, $"{culture} {translated.Key}: dash in \"{value}\"");
        }
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Translation_HasOneUniqueAccessKeyPerLabel(string culture)
    {
        var translation = ReadStrings(culture + ".");

        foreach (var key in English.Keys.Where(k => English[k].Contains('_')))
        {
            var value = translation[key];
            var underscore = value.IndexOf('_');
            Assert.True(underscore >= 0 && underscore == value.LastIndexOf('_') && underscore < value.Length - 1 && char.IsLetterOrDigit(value[underscore + 1]),
                $"{culture} {key}: \"{value}\" needs exactly one underscore before a letter");
        }

        foreach (var group in AccessKeyGroups)
        {
            var letters = group.Select(k => char.ToUpperInvariant(translation[k][translation[k].IndexOf('_') + 1])).ToList();
            Assert.True(letters.Distinct().Count() == letters.Count, $"{culture}: duplicate access keys in {string.Join(", ", group)}");
        }
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Translation_HasPluralFormsForItsLanguage(string culture)
    {
        var expectedForms = CultureInfo.GetCultureInfo(culture).TwoLetterISOLanguageName switch
        {
            "pl" or "ru" or "uk" => 3,
            "id" or "ja" or "ko" or "tr" or "vi" or "zh" => 1,
            _ => 2,
        };

        foreach (var plural in ReadStrings(culture + ".").Where(t => English[t.Key].Contains('|')))
        {
            var forms = plural.Value.Split('|');
            Assert.True(forms.Length == expectedForms, $"{culture} {plural.Key}: expected {expectedForms} forms in \"{plural.Value}\"");
            Assert.DoesNotContain(forms, string.IsNullOrWhiteSpace);

            // Only a form used for exactly 1 may leave out the number; Russian and Ukrainian use their first form for 21, 31, and so on too.
            var formsWithNumber = forms.Length == 1 || culture is "ru" or "uk" ? forms : forms.Skip(1);
            Assert.All(formsWithNumber, form => Assert.Contains("{0}", form));
        }
    }

    [Fact]
    public void Get_FallsBackToTheClosestTranslationThenEnglish()
    {
        var german = ReadStrings("de.");

        Assert.Equal(german["MenuCopy"], Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("de-AT")));
        Assert.Equal("_Copy", Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("sv-SE")));
        Assert.Equal(ReadStrings("es.")["MenuCopy"], Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("ca-ES")));
        Assert.Equal(ReadStrings("ru.")["MenuCopy"], Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("be-BY")));
        Assert.Equal(ReadStrings("fr.")["MenuCopy"], Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("lb-LU")));
        Assert.Equal("NoSuchKey", Loc.Get("NoSuchKey"));
    }

    [Fact]
    public void Source_UsesExactlyTheDefinedKeys()
    {
        // Keys chosen at runtime must start with "Relative" (the relative time units) or be written as separate Loc.Get calls, so this scan can see them.
        var sourceFolder = FindSourceFolder();
        var usage = new Regex(@"Loc\.(?:Get|Format)\(\s*""(\w+)""|=""\{local:Loc\s+(\w+)\s*\}""|""(Relative\w+)""");
        var used = Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".xaml"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(f => usage.Matches(File.ReadAllText(f)).Cast<Match>())
            .Select(m => m.Groups.Cast<Group>().Skip(1).First(g => g.Success).Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.Empty(used.Except(English.Keys));
        Assert.Empty(English.Keys.Except(used));
    }

    private static HashSet<string> Placeholders(string text) => new(PlaceholderRegex.Matches(text).Cast<Match>().Select(m => m.Value));

    private static HashSet<string> Urls(string text) => new(UrlRegex.Matches(text).Cast<Match>().Select(m => m.Value));

    private static Dictionary<string, string> ReadStrings(string culturePart)
    {
        using var stream = typeof(Loc).Assembly.GetManifestResourceStream($"{ResourcePrefix}{culturePart}resources");
        using var reader = new ResourceReader(stream);
        return reader.Cast<DictionaryEntry>().ToDictionary(e => (string)e.Key, e => (string)e.Value);
    }

    private static string FindSourceFolder()
    {
        for (var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DesktopClock.sln")))
                return Path.Combine(dir.FullName, "DesktopClock");
        }

        throw new DirectoryNotFoundException("Couldn't find the solution folder.");
    }
}
