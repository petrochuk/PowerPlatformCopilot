namespace AP2.DataverseAzureAI.Extensions;

public static class NumberExtensions
{
    private static readonly Dictionary<string, long> NumberTable = new(StringComparer.InvariantCultureIgnoreCase);

    static NumberExtensions()
    {
        NumberTable.Add("zero", 0);
        NumberTable.Add("one", 1);
        NumberTable.Add("two", 2);
        NumberTable.Add("three", 3);
        NumberTable.Add("four", 4);
        NumberTable.Add("five", 5);
        NumberTable.Add("six", 6);
        NumberTable.Add("seven", 7);
        NumberTable.Add("eight", 8);
        NumberTable.Add("nine", 9);
        NumberTable.Add("ten", 10);
        NumberTable.Add("eleven", 11);
        NumberTable.Add("twelve", 12);
        NumberTable.Add("thirteen", 13);
        NumberTable.Add("fourteen", 14);
        NumberTable.Add("fifteen", 15);
        NumberTable.Add("sixteen", 16);
        NumberTable.Add("seventeen", 17);
        NumberTable.Add("eighteen", 18);
        NumberTable.Add("nineteen", 19);
        NumberTable.Add("twenty", 20);
        NumberTable.Add("thirty", 30);
        NumberTable.Add("forty", 40);
        NumberTable.Add("fifty", 50);
        NumberTable.Add("sixty", 60);
        NumberTable.Add("seventy", 70);
        NumberTable.Add("eighty", 80);
        NumberTable.Add("ninety", 90);
        NumberTable.Add("hundred", 100);
        NumberTable.Add("thousand", 1000);
        NumberTable.Add("million", 1000000);
        NumberTable.Add("billion", 1000000000);
        NumberTable.Add("trillion", 1000000000000);
        NumberTable.Add("quadrillion", 1000000000000000);
        NumberTable.Add("quintillion", 1000000000000000000);
    }

    public static bool TryParseToLong(this string numberString, out long total)
    {
        if (string.IsNullOrWhiteSpace(numberString))
        {
            total = 0L;
            return false;
        }

        if (long.TryParse(numberString, out total))
            return true;

        var parts = numberString.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        total = 0L;
        long acc = 0L;
        foreach (var part in parts)
        {
            if (!NumberTable.TryGetValue(part, out var n))
                return false;
            if (n >= 1000)
            {
                total += (acc * n);
                acc = 0;
            }
            else if (n >= 100)
            {
                acc *= n;
            }
            else acc += n;
        }

        total += acc;

        return true;
    }
}
