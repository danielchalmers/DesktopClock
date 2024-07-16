using System;
using System.Text.RegularExpressions;

namespace DesktopClock;

public static class Tokenizer
{
    private static readonly Regex _tokenizerRegex = new("{([^{}]+)}", RegexOptions.Compiled);

    /// <summary>
    /// <para>Returns a string formatted using a tokenized format or the default formatting method.</para>
    /// <para>Falls back to the default format on any exception.</para>
    /// </summary>
    /// <param name="formattable">The object to format.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="formatProvider">The format provider.</param>
    public static string FormatWithTokenizerOrFallBack(IFormattable formattable, string format, IFormatProvider formatProvider)
    {
        if (!string.IsNullOrWhiteSpace(format))
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
            }
        }

        // Fall back to the default format.
        return formattable.ToString();
    }
}
