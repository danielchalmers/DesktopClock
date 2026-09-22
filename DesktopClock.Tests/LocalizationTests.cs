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
    private static readonly Regex UrlRegex = new(@"https://[\w./-]*\w");

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
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Translation_KeepsPlaceholdersAndLinks(string culture)
    {
        foreach (var translated in ReadStrings(culture + "."))
        {
            var english = English[translated.Key];
            Assert.True(Placeholders(english).SetEquals(Placeholders(translated.Value)), $"{culture} {translated.Key}: placeholders changed in \"{translated.Value}\"");

            foreach (Match url in UrlRegex.Matches(english))
                Assert.True(translated.Value.Contains(url.Value), $"{culture} {translated.Key}: missing {url.Value}");
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
            Assert.True(plural.Value.Split('|').Length == expectedForms, $"{culture} {plural.Key}: expected {expectedForms} forms in \"{plural.Value}\"");
    }

    [Fact]
    public void Get_FallsBackToTheClosestTranslationThenEnglish()
    {
        var german = ReadStrings("de.");

        Assert.Equal(german["MenuCopy"], Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("de-AT")));
        Assert.Equal("_Copy", Loc.Get("MenuCopy", CultureInfo.GetCultureInfo("sv-SE")));
        Assert.Equal("NoSuchKey", Loc.Get("NoSuchKey"));
    }

    [Fact]
    public void Source_OnlyUsesKeysThatExist()
    {
        var sourceFolder = FindSourceFolder();
        var usage = new Regex(@"Loc\.(?:Get|Format)\(""(\w+)""|=""\{local:Loc (\w+)\}""|""(Relative\w+)""");
        var used = Directory.EnumerateFiles(sourceFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".xaml"))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(f => usage.Matches(File.ReadAllText(f)).Cast<Match>())
            .Select(m => m.Groups.Cast<Group>().Skip(1).First(g => g.Success).Value)
            .Distinct()
            .ToList();

        Assert.NotEmpty(used);
        Assert.DoesNotContain(used, k => !English.ContainsKey(k));
    }

    private static HashSet<string> Placeholders(string text) => new(PlaceholderRegex.Matches(text).Cast<Match>().Select(m => m.Value));

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
