using System.Globalization;
using System.Text.RegularExpressions;

namespace DietTracker.Core;

/// <summary>Turns free-text measures ("2 tbsp", "1 1/2 cups", "400g", "3 žlice") into grams. Volumes are treated as 1 g per ml.</summary>
public static partial class MeasureParser
{
    private static readonly Dictionary<string, double> UnitGrams = new()
    {
        ["g"] = 1, ["gr"] = 1, ["gram"] = 1, ["grams"] = 1, ["grama"] = 1,
        ["kg"] = 1000, ["kilo"] = 1000,
        ["ml"] = 1, ["millilitre"] = 1, ["milliliter"] = 1, ["l"] = 1000, ["litre"] = 1000, ["liter"] = 1000,
        ["cl"] = 10, ["dl"] = 100,
        ["tbsp"] = 15, ["tbs"] = 15, ["tblsp"] = 15, ["tablespoon"] = 15, ["tablespoons"] = 15,
        ["zlica"] = 15, ["zlice"] = 15, ["zlicu"] = 15, ["zlica."] = 15,
        ["tsp"] = 5, ["teaspoon"] = 5, ["teaspoons"] = 5, ["zlicica"] = 5, ["zlicice"] = 5,
        ["cup"] = 240, ["cups"] = 240, ["salica"] = 240, ["salice"] = 240,
        ["oz"] = 28.35, ["ounce"] = 28.35, ["ounces"] = 28.35,
        ["lb"] = 453.6, ["lbs"] = 453.6, ["pound"] = 453.6, ["pounds"] = 453.6,
        ["pinch"] = 0.4, ["dash"] = 0.6, ["prstohvat"] = 0.4,
    };

    private static readonly Dictionary<string, double> ContainerGrams = new()
    {
        ["can"] = 400, ["cans"] = 400, ["tin"] = 400, ["tins"] = 400,
    };

    private static readonly HashSet<string> MultiplierUnits = ["x", "can", "cans", "tin", "tins", "pack", "packs", "packet", "jar", "jars"];

    [GeneratedRegex(@"^\s*(?:(?<whole>\d+)\s+(?<n1>\d+)/(?<d1>\d+)|(?<n2>\d+)/(?<d2>\d+)|(?<dec>\d+(?:[.,]\d+)?))(?:\s*-\s*\d+(?:[.,]\d+)?)?\s*(?<unit>[a-z]+)?")]
    private static partial Regex Leading();

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(kg|g|gr|ml|l)\b")]
    private static partial Regex Explicit();

    /// <param name="measure">Free-text quantity, e.g. "2 tbsp".</param>
    /// <param name="pieceGrams">Typical weight of one piece of the ingredient, used for counts like "2 onions".</param>
    /// <param name="cupGrams">Weight of one cup of this ingredient; defaults to 240 g (a cup of water).</param>
    /// <returns>Grams, or null when the quantity cannot be determined.</returns>
    public static double? ParseGrams(string? measure, double? pieceGrams = null, double? cupGrams = null)
    {
        if (string.IsNullOrWhiteSpace(measure)) return null;

        var text = TextNorm.Fold(measure)
            .Replace("½", " 1/2").Replace("¼", " 1/4").Replace("¾", " 3/4")
            .Replace("⅓", " 1/3").Replace("⅔", " 2/3").Replace("⅛", " 1/8");

        var m = Leading().Match(text);
        if (!m.Success)
            return ExplicitGrams(text);

        var quantity = Quantity(m);
        var unit = m.Groups["unit"].Success ? m.Groups["unit"].Value : "";

        if (cupGrams is { } cup && unit is "cup" or "cups" or "salica" or "salice")
            return quantity * cup;

        if (UnitGrams.TryGetValue(unit, out var perUnit))
            return quantity * perUnit;

        var rest = text[(m.Index + m.Length)..];
        var explicitGrams = ExplicitGrams(rest);

        if (explicitGrams is not null)
            return MultiplierUnits.Contains(unit) ? quantity * explicitGrams : explicitGrams;

        if (ContainerGrams.TryGetValue(unit, out var container))
            return quantity * container;

        // "5 thinly sliced", "2 cloves", "1 large": a count of pieces.
        return pieceGrams is { } piece ? quantity * piece : null;
    }

    private static double Quantity(Match m)
    {
        if (m.Groups["whole"].Success)
            return double.Parse(m.Groups["whole"].Value, CultureInfo.InvariantCulture)
                   + Fraction(m.Groups["n1"].Value, m.Groups["d1"].Value);
        if (m.Groups["n2"].Success)
            return Fraction(m.Groups["n2"].Value, m.Groups["d2"].Value);
        return double.Parse(m.Groups["dec"].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
    }

    private static double Fraction(string n, string d)
    {
        var den = double.Parse(d, CultureInfo.InvariantCulture);
        return den == 0 ? 0 : double.Parse(n, CultureInfo.InvariantCulture) / den;
    }

    private static double? ExplicitGrams(string text)
    {
        var m = Explicit().Match(text);
        if (!m.Success) return null;
        var value = double.Parse(m.Groups[1].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
        return m.Groups[2].Value is "kg" or "l" ? value * 1000 : value;
    }
}
