using System;
using System.Collections.Concurrent;
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

            return base.InternalGetResourceSet(CultureInfo.InvariantCulture, createIfNotExists, tryParents);
        }

        private ResourceSet LoadCultureSet(string cultureName)
        {
            var stream = MainAssembly.GetManifestResourceStream($"{BaseName}.{cultureName}.resources");
            return stream == null ? null : new ResourceSet(stream);
        }
    }
}
