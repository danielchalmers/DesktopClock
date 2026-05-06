using System;
using System.Globalization;
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
                        return FormatToken(formattable, formatString, formatProvider);
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

    private static string FormatToken(IFormattable formattable, string format, IFormatProvider formatProvider)
    {
        if (TryFormatWeekToken(formattable, format, out var result))
        {
            return result;
        }

        return formattable.ToString(format, formatProvider);
    }

    private static bool TryFormatWeekToken(IFormattable formattable, string format, out string result)
    {
        result = null;

        if (!TryGetDateTime(formattable, out var dateTime))
        {
            return false;
        }

        switch (format)
        {
            case "week":
                result = GetIsoWeek(dateTime).ToString(CultureInfo.InvariantCulture);
                return true;

            case "weekYear":
                result = GetIsoWeekYear(dateTime).ToString(CultureInfo.InvariantCulture);
                return true;

            default:
                return false;
        }
    }

    private static bool TryGetDateTime(IFormattable formattable, out DateTime dateTime)
    {
        switch (formattable)
        {
            case DateTime value:
                dateTime = value;
                return true;

            case DateTimeOffset value:
                dateTime = value.DateTime;
                return true;

            default:
                dateTime = default;
                return false;
        }
    }

    private static int GetIsoWeek(DateTime dateTime)
    {
        if (dateTime.DayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Wednesday)
        {
            dateTime = dateTime.AddDays(3);
        }

        var calendar = CultureInfo.InvariantCulture.Calendar;
        return calendar.GetWeekOfYear(dateTime, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    private static int GetIsoWeekYear(DateTime dateTime)
    {
        var day = dateTime.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)dateTime.DayOfWeek;
        return dateTime.AddDays(4 - day).Year;
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
