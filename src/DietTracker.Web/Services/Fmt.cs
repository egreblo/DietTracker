using System.Globalization;

namespace DietTracker.Web.Services;

/// <summary>Croatian date formatting. Hard-coded because WASM may run with invariant globalization.</summary>
public static class Fmt
{
    private static readonly string[] DayNames =
        ["ponedjeljak", "utorak", "srijeda", "četvrtak", "petak", "subota", "nedjelja"];

    private static readonly string[] MonthsGenitive =
    [
        "siječnja", "veljače", "ožujka", "travnja", "svibnja", "lipnja",
        "srpnja", "kolovoza", "rujna", "listopada", "studenoga", "prosinca",
    ];

    public static DateOnly MondayOf(DateOnly d) => d.AddDays(-(((int)d.DayOfWeek + 6) % 7));

    public static string DayName(DateOnly d) => DayNames[((int)d.DayOfWeek + 6) % 7];

    public static string ShortDate(DateOnly d) => $"{d.Day}. {d.Month}.";

    public static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string WeekTitle(DateOnly monday)
    {
        var end = monday.AddDays(6);
        return monday.Month == end.Month
            ? $"{monday.Day}.–{end.Day}. {MonthsGenitive[end.Month - 1]} {end.Year}."
            : $"{ShortDate(monday)} – {ShortDate(end)} {end.Year}.";
    }

    public static string Number(double value) =>
        Math.Round(value).ToString("0", CultureInfo.InvariantCulture);
}
