using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DesktopClock;

public record DateFormatExample
{
    private DateFormatExample(string format, string example)
    {
        Format = format;
        Example = example;
    }

    /// <summary>
    /// The actual format (<c>dddd, MMMM dd</c>).
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// An example of the format in action (<c>Monday, July 15</c>).
    /// </summary>
    public string Example { get; }

    /// <summary>
    /// Creates a <see cref="DateFormatExample" /> for the given format.
    /// </summary>
    public static DateFormatExample FromFormat(string format, DateTimeOffset dateTimeOffset)
    {
        var example = Tokenizer.FormatWithTokenizerOrFallBack(dateTimeOffset, format, CultureInfo.DefaultThreadCurrentCulture);
        return new(format, example);
    }

    /// <summary>
    /// Common date time formatting strings and an example string for each.
    /// </summary>
    /// <remarks>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings">Standard date and time format strings</see>
    /// <br/>
    /// <see href="https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings">Custom date and time format strings</see>
    /// </remarks>
    public static IReadOnlyCollection<DateFormatExample> DefaultExamples { get; } = new[]
    {
        // Custom formats
        "{ddd}, {MMM dd}, {HH:mm}",           // Custom format: "Mon, Apr 10, 14:30"
        "{ddd}, {MMM dd}, {h:mm tt}",         // Custom format: "Mon, Apr 10, 2:30 PM"
        "{ddd}, {MMM dd}, {HH:mm:ss}",        // Custom format: "Mon, Apr 10, 14:30:45"
        "{ddd}, {MMM dd}, {h:mm:ss tt}",      // Custom format: "Mon, Apr 10, 2:30:45 PM"
        "{ddd}, {MMM dd}, {HH:mm K}",         // Custom format: "Mon, Apr 10, 14:30 +02:00"
        "{ddd}, {MMM dd}, {h:mm tt K}",       // Custom format: "Mon, Apr 10, 2:30 PM +02:00"
        "{ddd}, {MMM dd}, {yyyy} {HH:mm}",    // Custom format: "Mon, Apr 10, 2023 14:30"
        "{ddd}, {MMM dd}, {yyyy} {h:mm tt}",  // Custom format: "Mon, Apr 10, 2023 14:30"
        "{dddd}, {MMMM dd}",                  // Custom format: "Monday, April 10"
        "{dddd}, {MMMM dd}, {HH:mm}",         // Custom format: "Monday, April 10, 14:30"
        "{dddd}, {MMMM dd}, {h:mm tt}",       // Custom format: "Monday, April 10, 2:30 PM"
        "{dddd}, {MMM dd}, {HH:mm}",          // Custom format: "Monday, Apr 10, 14:30"
        "{dddd}, {MMM dd}, {h:mm tt}",        // Custom format: "Monday, Apr 10, 2:30 PM"
        "{dddd}, {MMM dd}, {HH:mm:ss}",       // Custom format: "Monday, Apr 10, 14:30:45"
        "{dddd}, {MMM dd}, {h:mm:ss tt}",     // Custom format: "Monday, Apr 10, 2:30:45 PM"
    
        // Standard formats
        "D",                                  // Long date pattern: Monday, June 15, 2009 (en-US)
        "f",                                  // Full date/time pattern (short time): Monday, June 15, 2009 1:45 PM (en-US)
        "F",                                  // Full date/time pattern (long time): Monday, June 15, 2009 1:45:30 PM (en-US)
        "R",                                  // RFC1123 pattern: Mon, 15 Jun 2009 20:45:30 GMT (DateTimeOffset)
        "M",                                  // Month/day pattern: June 15 (en-US)
        "Y",                                  // Year month pattern: June 2009 (en-US)
        "t",                                  // Short time pattern: 1:45 PM (en-US)
        "T",                                  // Long time pattern: 1:45:30 PM (en-US)
        "d",                                  // Short date pattern: 6/15/2009 (en-US)
        "g",                                  // General date/time pattern (short time): 6/15/2009 1:45 PM (en-US)
        "G",                                  // General date/time pattern (long time): 6/15/2009 1:45:30 PM (en-US)
        "u",                                  // Universal sortable date/time pattern: 2009-06-15 13:45:30Z (DateTime)
        //"U",                                // Universal full date/time pattern: Monday, June 15, 2009 8:45:30 PM (en-US) // Not available for DateTimeOffset.
        "s",                                  // Sortable date/time pattern: 2009-06-15T13:45:30
        //"O",                                // Round-trip date/time pattern: 2009-06-15T13:45:30.0000000-07:00 (DateTimeOffset) // Too precise with milliseconds.
    }.Select(f => FromFormat(f, DateTimeOffset.Now)).ToList();
}
