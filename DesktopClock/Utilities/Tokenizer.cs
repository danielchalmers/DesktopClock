using System;
using System.Text.RegularExpressions;

namespace DesktopClock;

public static class Tokenizer
{
    private static readonly Regex _tokenizerRegex = new("{([^{}]+)}", RegexOptions.Compiled);
    public const string FormatErrorMessage = "Bad format";

    /// <summary>
    /// <para>Returns a string formatted using a tokenized format or the default formatting method.</para>
    /// <para>Returns a short error message when a non-empty format is malformed.</para>
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
                if (UsesTokenSyntax(format))
                {
                    if (!HasValidTokenSyntax(format))
                    {
                        return FormatErrorMessage;
                    }

                    return _tokenizerRegex.Replace(format, (m) =>
                    {
                        var formatString = m.Groups[1].Value;
                        return formattable.ToString(formatString, formatProvider);
                    });
                }

                // Use basic formatter if no special formatting tokens are present.
                return formattable.ToString(format, formatProvider);
            }
            catch
            {
                return FormatErrorMessage;
            }
        }

        // Fall back to the default format.
        return formattable.ToString();
    }

    private static bool UsesTokenSyntax(string format)
    {
        return format.Contains("{") || format.Contains("}");
    }

    private static bool HasValidTokenSyntax(string format)
    {
        var index = 0;
        while (index < format.Length)
        {
            if (format[index] == '}')
            {
                return false;
            }

            if (format[index] != '{')
            {
                index++;
                continue;
            }

            var closeIndex = format.IndexOf('}', index + 1);
            if (closeIndex == -1 || closeIndex == index + 1)
            {
                return false;
            }

            if (format.IndexOf('{', index + 1, closeIndex - index - 1) != -1)
            {
                return false;
            }

            index = closeIndex + 1;
        }

        return true;
    }
}
