using DietTracker.Core;

namespace DietTracker.Web.Services;

public class RecipeService(SupabaseApi api)
{
    private List<Recipe>? _cache;

    public async Task<List<Recipe>> GetAllAsync(bool force = false)
    {
        if (force || _cache is null)
            _cache = await api.GetAsync<Recipe>("rest/v1/recipes?select=*&order=name.asc");
        return _cache;
    }

    public async Task<Recipe?> GetAsync(Guid id) =>
        (await GetAllAsync()).FirstOrDefault(r => r.Id == id);

    /// <summary>Inserts or updates (primary-key upsert).</summary>
    public async Task SaveAsync(Recipe recipe)
    {
        await api.WriteAsync(HttpMethod.Post, "rest/v1/recipes", recipe, "resolution=merge-duplicates,return=minimal");
        _cache = null;
    }

    public async Task SetPoolAsync(Recipe recipe, bool inPool)
    {
        await api.WriteAsync(HttpMethod.Patch, $"rest/v1/recipes?id=eq.{recipe.Id}",
            new Dictionary<string, object?> { ["in_pool"] = inPool });
        recipe.InPool = inPool;
    }

    public async Task DeleteAsync(Guid id)
    {
        await api.DeleteAsync($"rest/v1/recipes?id=eq.{id}");
        _cache = null;
    }
}
