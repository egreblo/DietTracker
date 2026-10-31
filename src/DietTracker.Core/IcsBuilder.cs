using System.Text;

namespace DietTracker.Core;

public record IcsEvent(
    string Uid,
    DateOnly Date,
    TimeOnly Start,
    TimeSpan Duration,
    string Summary,
    string? Description = null);

/// <summary>Builds an RFC 5545 calendar. Times are "floating" so they land at the same wall-clock time anywhere.</summary>
public static class IcsBuilder
{
    public static string Build(IEnumerable<IcsEvent> events, DateTime nowUtc, string calendarName = "Ručak")
    {
        var lines = new List<string>
        {
            "BEGIN:VCALENDAR",
            "VERSION:2.0",
            "PRODID:-//DietTracker//HR",
            "CALSCALE:GREGORIAN",
            "METHOD:PUBLISH",
            "X-WR-CALNAME:" + Escape(calendarName),
        };

        foreach (var e in events)
        {
            var start = e.Date.ToDateTime(e.Start);
            lines.Add("BEGIN:VEVENT");
            lines.Add("UID:" + e.Uid);
            lines.Add("DTSTAMP:" + nowUtc.ToString("yyyyMMdd'T'HHmmss'Z'"));
            lines.Add("DTSTART:" + start.ToString("yyyyMMdd'T'HHmmss"));
            lines.Add("DTEND:" + start.Add(e.Duration).ToString("yyyyMMdd'T'HHmmss"));
            lines.Add("SUMMARY:" + Escape(e.Summary));
            if (!string.IsNullOrWhiteSpace(e.Description))
                lines.Add("DESCRIPTION:" + Escape(e.Description));
            lines.Add("END:VEVENT");
        }

        lines.Add("END:VCALENDAR");
        return string.Concat(lines.Select(l => Fold(l) + "\r\n"));
    }

    public static string Escape(string text) => text
        .Replace("\\", "\\\\")
        .Replace(";", "\\;")
        .Replace(",", "\\,")
        .Replace("\r\n", "\\n")
        .Replace("\n", "\\n")
        .Replace("\r", "\\n");

    /// <summary>Folds a content line to at most 75 octets per line without splitting a UTF-8 character.</summary>
    public static string Fold(string line)
    {
        if (Encoding.UTF8.GetByteCount(line) <= 75) return line;

        var sb = new StringBuilder();
        var octets = 0;
        var limit = 75;
        foreach (var rune in line.EnumerateRunes())
        {
            var size = rune.Utf8SequenceLength;
            if (octets + size > limit)
            {
                sb.Append("\r\n ");
                octets = 1; // the leading space counts toward the next line's limit
            }
            sb.Append(rune.ToString());
            octets += size;
        }
        return sb.ToString();
    }
}
