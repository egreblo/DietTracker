namespace DietTracker.Web.Services;

/// <summary>
/// Translation seam. Keys are the Croatian texts themselves; an English table can be added later
/// without touching any page (unknown keys fall back to the key).
/// </summary>
public class Loc
{
    private readonly Dictionary<string, string> _table = [];

    public string this[string key] => _table.TryGetValue(key, out var value) ? value : key;

    public string this[string key, params object[] args] => string.Format(this[key], args);
}
