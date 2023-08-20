using AP2.DataverseAzureAI.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Time.Testing;

namespace DataverseAzureAI_Tests;

[TestClass]
public class TimeExtensionsTest
{
    [DataTestMethod]
    [DataRow("older than 2 weeks", 
        3000, 10, 1,
        3000, 08, 1, true)]
    [DataRow("last 2 weeks", 
        3000, 10, 1,
        3000, 08, 1, false)]
    [DataRow("lt 30 days ago", 
        3000, 10, 1,
        3000, 09, 20, true)]
    public void RelativeEquals_Tests(string input, 
        int nowYear, int nowMonth, int nowDay,
        int year, int month, int day,
        bool expectedResult)
    {
#pragma warning disable EXTEXP0004 // Type is for evaluation purposes only and is subject to change or removal in future updates.
        var fakeTimeProvider = new FakeTimeProvider(new DateTimeOffset(nowYear, nowMonth, nowDay, 0, 0, 0, TimeSpan.Zero));
#pragma warning restore EXTEXP0004 // Type is for evaluation purposes only and is subject to change or removal in future updates.

        var dateTime = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
        dateTime.RelativeEquals(input, fakeTimeProvider).Should().Be(expectedResult);
    }

    [DataTestMethod]
    [DataRow("last 2 weeks", 0, false, 0)]
    [DataRow("last 2 weeks", 1, true, 14)]
    [DataRow("week", 0, true, 7)]
    [DataRow("one hundred days", 0, true, 100)]
    [DataRow("two hundred fifty six days", 0, true, 256)]
    [DataRow("23 month", 0, true, 23 * 30)]
    [DataRow("few years", 0, true, 3 * 365)]
    public void TryGetTimeOffset_Tests(string input, int startIndex, bool expectedResult, int expectedOffsetInDays)
    {
        var parts = input.Split(' ');

        parts.TryGetTimeSpan(startIndex, out var timeSpan).Should().Be(expectedResult);

        timeSpan.TotalDays.Should().Be(expectedOffsetInDays);
    }
}