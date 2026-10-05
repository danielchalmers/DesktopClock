using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Windows.Markup;

namespace DesktopClock;

/// <summary>
/// Looks up UI text from Localization/Strings.resx in the Windows display language, falling back to English. In XAML, use <c>{local:Loc KeyName}</c>.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class Loc : MarkupExtension
{
    private static readonly ResourceManager _resources = new EmbeddedResourceManager("DesktopClock.Localization.Strings", typeof(Loc).Assembly);

    /// <summary>
    /// Windows language add-ons show their base language for anything they don't translate, so these get that language's strings instead of English. Remove a language from here if it gets its own translation.
    /// </summary>
    private static readonly Dictionary<string, string> _baseLanguages = new()
    {
        ["ca"] = "es", // Catalan
        ["eu"] = "es", // Basque
        ["gl"] = "es", // Galician
        ["be"] = "ru", // Belarusian
        ["tt"] = "ru", // Tatar
        ["ug"] = "zh-Hans", // Uyghur
    };

    public Loc(string key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public string Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider) => Get(Key);

    /// <summary>
    /// Returns the text for the key in the given language, or the current UI language when none is given. Returns the key itself if it doesn't exist so a typo shows up on screen.
    /// </summary>
    public static string Get(string key, CultureInfo culture = null) => _resources.GetString(key, culture) ?? key;

    /// <summary>
    /// Returns the text for the key with its placeholders filled in.
    /// </summary>
    public static string Format(string key, params object[] args) => string.Format(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>
    /// The language whose translation the culture uses, such as "de" for de-AT, or "es" for Catalan, which Windows backs with Spanish.
    /// </summary>
    public static string TranslationLanguage(CultureInfo culture) =>
        _baseLanguages.TryGetValue(culture.TwoLetterISOLanguageName, out var baseLanguage) ? baseLanguage : culture.TwoLetterISOLanguageName;

    /// <summary>
    /// Finds translations compiled into the exe as Strings.{culture}.resources, since satellite assemblies would break the single-file app (see the csproj).
    /// </summary>
    private sealed class EmbeddedResourceManager(string baseName, Assembly assembly) : ResourceManager(baseName, assembly)
    {
        private readonly ConcurrentDictionary<string, ResourceSet> _cultureSets = new();

        protected override ResourceSet InternalGetResourceSet(CultureInfo culture, bool createIfNotExists, bool tryParents)
        {
            // Walk up from a specific culture like de-AT to the closest translation like de; English is the neutral resources in the base implementation.
            for (var current = culture; !Equals(current, CultureInfo.InvariantCulture); current = current.Parent)
            {
                var set = _cultureSets.GetOrAdd(current.Name, LoadCultureSet);
                if (set != null)
                    return set;
            }

            if (_baseLanguages.TryGetValue(culture.TwoLetterISOLanguageName, out var baseLanguage) && _cultureSets.GetOrAdd(baseLanguage, LoadCultureSet) is { } baseSet)
                return baseSet;

            return base.InternalGetResourceSet(CultureInfo.InvariantCulture, createIfNotExists, tryParents);
        }

        private ResourceSet LoadCultureSet(string cultureName)
        {
            var stream = MainAssembly.GetManifestResourceStream($"{BaseName}.{cultureName}.resources");
            return stream == null ? null : new ResourceSet(stream);
        }
    }
}
