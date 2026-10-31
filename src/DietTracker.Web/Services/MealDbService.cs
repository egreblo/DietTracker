using System.Net.Http.Json;
using System.Text.Json;
using DietTracker.Core;

namespace DietTracker.Web.Services;

/// <summary>Browses TheMealDB (free test key "1", CORS-enabled) and maps meals to recipes.</summary>
public class MealDbService
{
    private const string BaseUrl = "https://www.themealdb.com/api/json/v1/1/";
    private const int CategoryLimit = 12;

    private readonly HttpClient _http = new();

    public Task<List<MealDbMeal>> SearchAsync(string query) =>
        FetchMealsAsync($"search.php?s={Uri.EscapeDataString(query.Trim())}");

    public async Task<List<MealDbMeal>> RandomAsync(int count)
    {
        var batches = await Task.WhenAll(Enumerable.Range(0, count).Select(_ => FetchMealsAsync("random.php")));
        return batches.SelectMany(b => b).DistinctBy(m => m.Recipe.ExternalId).ToList();
    }

    public async Task<List<string>> CategoriesAsync()
    {
        using var doc = await GetAsync("list.php?c=list");
        return doc.RootElement.TryGetProperty("meals", out var meals) && meals.ValueKind == JsonValueKind.Array
            ? meals.EnumerateArray().Select(m => m.GetProperty("strCategory").GetString()!).ToList()
            : [];
    }

    /// <summary>The filter endpoint only returns ids, so the first few meals are looked up in full.</summary>
    public async Task<List<MealDbMeal>> ByCategoryAsync(string category)
    {
        using var doc = await GetAsync($"filter.php?c={Uri.EscapeDataString(category)}");
        if (!doc.RootElement.TryGetProperty("meals", out var meals) || meals.ValueKind != JsonValueKind.Array)
            return [];

        var ids = meals.EnumerateArray().Select(m => m.GetProperty("idMeal").GetString()!).Take(CategoryLimit).ToList();
        var batches = await Task.WhenAll(ids.Select(id => FetchMealsAsync($"lookup.php?i={id}")));
        return batches.SelectMany(b => b).ToList();
    }

    private async Task<List<MealDbMeal>> FetchMealsAsync(string path)
    {
        using var doc = await GetAsync(path);
        return doc.RootElement.TryGetProperty("meals", out var meals) && meals.ValueKind == JsonValueKind.Array
            ? meals.EnumerateArray().Select(MealDbMapper.Map).Where(m => m.Recipe.Name.Length > 0).ToList()
            : [];
    }

    private async Task<JsonDocument> GetAsync(string path)
    {
        try
        {
            using var res = await _http.GetAsync(BaseUrl + path);
            res.EnsureSuccessStatusCode();
            return await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync());
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
            throw new ApiException("Pretraživanje weba nije uspjelo. Pokušaj ponovno.");
        }
    }
}
