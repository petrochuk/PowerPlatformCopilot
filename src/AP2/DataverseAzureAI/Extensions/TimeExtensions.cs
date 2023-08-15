namespace AP2.DataverseAzureAI.Extensions;

public static class TimeExtensions
{
    public static string ToRelativeSentence(this DateTime value)
    {
        var now = DateTime.Now;
        var timeDiff = now - value;

        if (timeDiff.TotalMinutes < 2)
            return "a moment ago";

        if (timeDiff.TotalMinutes < 10)
            return "a few minutes ago";

        if (timeDiff.TotalMinutes < 20)
            return "about 15 minutes ago";

        if (timeDiff.TotalMinutes < 45)
            return "about half hour ago";

        if (timeDiff.TotalMinutes < 75)
            return "about an hour ago";

        if (timeDiff.TotalHours < 24)
        {
            if (value.Day < now.Day)
            {
                if(5 < timeDiff.TotalHours)
                    return "yesterday";

                return "a few hours";
            }

            return now.ToTimeOfDay(momentInTime: true);
        }

        if (timeDiff.TotalDays < 2)
            return "yesterday";

        if (timeDiff.TotalDays < 5)
            return $"a few days ago";

        if (timeDiff.TotalDays < 10)
            return $"about a week ago";

        if (timeDiff.TotalDays < 20)
            return $"a few weeks ago";

        if (timeDiff.TotalDays < 45)
            return $"a month ago";

        if (timeDiff.TotalDays < 300)
            return $"a few month ago";

        if (timeDiff.TotalDays < 400)
            return $"a year ago";

        if (timeDiff.TotalDays < 1000)
            return $"a few years ago";

        return "A long time ago, in a galaxy far far away...";
    }

    public static string ToTimeOfDay(this DateTime dateTime, bool momentInTime = false)
    {
        if (dateTime.Hour < 3)
            return momentInTime ? "at night" : "night";
        if (dateTime.Hour < 6)
            return "early morning";
        if (dateTime.Hour < 12)
            return momentInTime ? "in the morning" : "morning";
        if (dateTime.Hour < 17)
            return momentInTime ? "in the afternoon" : "afternoon";
        if (dateTime.Hour < 22)
            return momentInTime ? "at night" : "night";

        return momentInTime ? "in the evening" : "evening";
    }

    public static bool RelativeEquals(this DateTime dateTime, string compareTo)
    {
        if (string.IsNullOrWhiteSpace(compareTo))
            return false;

        DateTime startDateTime, endDateTime;
        compareTo = compareTo.Trim();

        if (DateTime.TryParse(compareTo, out var compareToDateTime))
        {
            startDateTime = compareToDateTime.Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else if (string.Compare(compareTo, "today", StringComparison.OrdinalIgnoreCase) == 0)
        {
            startDateTime = DateTime.Now.Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else if (string.Compare(compareTo, "yesterday", StringComparison.OrdinalIgnoreCase) == 0)
        {
            startDateTime = DateTime.Now.AddDays(-1).Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else if (string.Compare(compareTo, "tomorrow", StringComparison.OrdinalIgnoreCase) == 0)
        {
            startDateTime = DateTime.Now.AddDays(1).Date;
            endDateTime = startDateTime.AddDays(1);
        }
        else
        {
            var parts = compareTo.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
            {
                parts = compareTo.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length < 2)
                {
                    if (compareTo.StartsWith("last", StringComparison.OrdinalIgnoreCase))
                        parts = new[] { "last", compareTo.Substring(4) };
                    else if (compareTo.StartsWith("prev", StringComparison.OrdinalIgnoreCase))
                        parts = new[] { "prev", compareTo.Substring(4) };
                    else if (compareTo.StartsWith("previous", StringComparison.OrdinalIgnoreCase))
                        parts = new[] { "previous", compareTo.Substring(8) };
                    else if (compareTo.StartsWith("next", StringComparison.OrdinalIgnoreCase))
                        parts = new[] { "next", compareTo.Substring(4) };
                    else
                        return false;
                }
            }

            if (parts.Length == 2)
            {
                if (string.Compare(parts[0], "last", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "prev", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "previous", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (string.Compare(parts[1], "day", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = DateTime.Now.AddDays(-1).Date;
                        endDateTime = startDateTime.AddDays(1);
                    }
                    else if (string.Compare(parts[1], "week", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = DateTime.Now.AddDays(-7).StartOfWeek(DayOfWeek.Sunday);
                        endDateTime = startDateTime.AddDays(7);
                    }
                    else if (string.Compare(parts[1], "month", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = (new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)).AddMonths(-1);
                        endDateTime = startDateTime.AddMonths(1);
                    }
                    else if (string.Compare(parts[1], "year", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = new DateTime(DateTime.Now.Year - 1, 1, 1);
                        endDateTime = new DateTime(DateTime.Now.Year, 1, 1);
                    }
                    else
                        return false;
                }
                else // TODO add Next
                    return false;
            }
            else if (parts.Length == 3)
            {
                if (!parts[1].TryParseToLong(out var total))
                    return false;

                if (string.Compare(parts[0], "last", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "prev", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(parts[0], "previous", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (string.Compare(parts[2], "day", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(parts[2], "days", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(parts[2], "day(s)", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = DateTime.Now.AddDays(-total);
                        endDateTime = DateTime.Now;
                    }
                    else if (string.Compare(parts[2], "week", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(parts[2], "weeks", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(parts[2], "week(s)", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = DateTime.Now.AddDays(-7 * total).StartOfWeek(DayOfWeek.Sunday);
                        endDateTime = DateTime.Now;
                    }
                    else if (string.Compare(parts[2], "month", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(parts[2], "months", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(parts[2], "month(s)", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime =DateTime.Now.AddMonths(-(int)total);
                        endDateTime = DateTime.Now;
                    }
                    else if (string.Compare(parts[2], "year", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(parts[2], "years", StringComparison.OrdinalIgnoreCase) == 0 ||
                            string.Compare(parts[2], "year(s)", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        startDateTime = DateTime.Now.AddYears(-(int)total);
                        endDateTime = DateTime.Now;
                    }
                    else
                        return false;
                }
                else // TODO add Next
                    return false;
            }
            else
                return false;
        }

        return dateTime >= startDateTime && dateTime < endDateTime;
    }

    public static DateTime StartOfWeek(this DateTime dateTime, DayOfWeek startOfWeek)
    {
        int diff = (7 + (dateTime.DayOfWeek - startOfWeek)) % 7;
        return dateTime.AddDays(-1 * diff).Date;
    }

    public static string ToTimeOfYear(this DateTime dateTime)
    {
        if (dateTime.Month < 3)
            return "winter";
        if (dateTime.Month < 6)
            return "spring";
        if (dateTime.Month < 9)
            return "summer";
        if (dateTime.Month < 12)
            return "autumn";

        return "winter";
    }
}
