using System.Text;
using DietTracker.Core;

namespace DietTracker.Tests;

public class IcsBuilderTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);

    private static IcsEvent Event(string summary, string? description = null) => new(
        "abc@diettracker", new DateOnly(2026, 11, 2), new TimeOnly(12, 0), TimeSpan.FromHours(1), summary, description);

    [Fact]
    public void ProducesWellFormedCalendarWithCrlf()
    {
        var ics = IcsBuilder.Build([Event("RUČAK - pasta sa tunom")], Now);

        Assert.StartsWith("BEGIN:VCALENDAR\r\n", ics);
        Assert.EndsWith("END:VCALENDAR\r\n", ics);
        Assert.Contains("VERSION:2.0\r\n", ics);
        Assert.Contains("DTSTART:20261102T120000\r\n", ics);
        Assert.Contains("DTEND:20261102T130000\r\n", ics);
        Assert.Contains("DTSTAMP:20261101T080000Z\r\n", ics);
        Assert.Contains("SUMMARY:RUČAK - pasta sa tunom\r\n", ics);
        Assert.DoesNotContain("\n", ics.Replace("\r\n", ""));
    }

    [Fact]
    public void EscapesSpecialCharacters()
    {
        Assert.Equal(@"a\, b\; c\\d\ne", IcsBuilder.Escape("a, b; c\\d\ne"));
    }

    [Fact]
    public void FoldsLongLinesAtSeventyFiveOctetsWithoutSplittingCharacters()
    {
        var description = string.Concat(Enumerable.Repeat("čćšđž", 40));
        var ics = IcsBuilder.Build([Event("x", description)], Now);

        foreach (var line in ics.Split("\r\n"))
            Assert.True(Encoding.UTF8.GetByteCount(line) <= 75, $"Line too long: {line}");

        // Unfolding restores the original text.
        var unfolded = ics.Replace("\r\n ", "");
        Assert.Contains("DESCRIPTION:" + description, unfolded);
    }

    [Fact]
    public void EmptyDescriptionIsOmitted()
    {
        var ics = IcsBuilder.Build([Event("x", "  ")], Now);
        Assert.DoesNotContain("DESCRIPTION", ics);
    }
}
