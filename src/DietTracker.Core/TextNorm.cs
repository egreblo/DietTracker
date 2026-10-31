using System.Globalization;
using System.Text;

namespace DietTracker.Core;

internal static class TextNorm
{
    /// <summary>Lower-cases and strips diacritics so "Češnjak" and "cesnjak" compare equal.</summary>
    public static string Fold(string text)
    {
        var decomposed = text.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Splits into letter-only tokens with a crude singular form, applied identically to names and aliases.</summary>
    public static string[] Tokens(string text)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        foreach (var c in Fold(text))
        {
            if (char.IsLetter(c)) { current.Append(c); continue; }
            if (current.Length > 0) { words.Add(Singular(current.ToString())); current.Clear(); }
        }
        if (current.Length > 0) words.Add(Singular(current.ToString()));
        return [.. words];
    }

    private static string Singular(string word)
    {
        if (word.Length > 4 && word.EndsWith("oes")) return word[..^2];
        if (word.Length > 3 && word.EndsWith('s') && !word.EndsWith("ss")) return word[..^1];
        return word;
    }
}
