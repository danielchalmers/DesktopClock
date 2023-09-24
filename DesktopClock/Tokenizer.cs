using System;
using System.Text.RegularExpressions;

namespace DesktopClock;

public static class Tokenizer
{
    private static readonly Regex _tokenizerRegex = new("{([^{}]+)}", RegexOptions.Compiled);

    /// <summary>
    /// Formats with a tokenized format in mind, or treats it as a regular formatting string.
    /// Falls back to the default format on any exception.
    /// </summary>
    /// <param name="formattable">The object to format.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="formatProvider">The format provider.</param>
    /// <returns></returns>
    public static string FormatWithTokenizerOrFallBack(IFormattable formattable, string format, IFormatProvider formatProvider)
    {
        try
        {
            if (format.Contains("}"))
            {
                return _tokenizerRegex.Replace(format, (m) =>
                {
                    var formatString = m.Value.Replace("{", "").Replace("}", "");
                    return formattable.ToString(formatString, formatProvider);
                });
            }

            // Use basic formatter if no special formatting tokens are present.
            return formattable.ToString(format, formatProvider);
        }
        catch
        {
            // Fallback to the default format.
            return formattable.ToString();
        }
    }
}
