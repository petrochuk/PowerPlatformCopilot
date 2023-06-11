namespace DataverseCopilot.Extensions;

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
