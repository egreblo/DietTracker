using DietTracker.Core;
using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;

namespace DietTracker.Web.Pages;

public partial class Uvoz
{
    [Inject] private MealDbService MealDb { get; set; } = default!;
    [Inject] private RecipeService Recipes { get; set; } = default!;

    private string query = "";
    private List<string> categories = [];
    private List<MealDbMeal>? results;
    private HashSet<string> savedIds = [];
    private bool busy;
    private bool saving;
    private string? error;

    private int NewCount => results?.Count(m => !IsAdded(m.Recipe)) ?? 0;

    private bool IsAdded(Recipe r) => r.ExternalId is not null && savedIds.Contains(r.ExternalId);

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var existing = await Recipes.GetAllAsync();
            savedIds = existing.Where(r => r.ExternalId is not null).Select(r => r.ExternalId!).ToHashSet();
            categories = await MealDb.CategoriesAsync();
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }

    private Task SearchAsync() =>
        string.IsNullOrWhiteSpace(query) ? Task.CompletedTask : LoadAsync(() => MealDb.SearchAsync(query));

    private Task RandomAsync() => LoadAsync(() => MealDb.RandomAsync(6));

    private Task CategoryAsync(string? category) =>
        string.IsNullOrEmpty(category) ? Task.CompletedTask : LoadAsync(() => MealDb.ByCategoryAsync(category));

    private async Task LoadAsync(Func<Task<List<MealDbMeal>>> fetch)
    {
        busy = true;
        error = null;
        try { results = await fetch(); }
        catch (Exception ex) { error = ex.Message; results = null; }
        finally { busy = false; }
    }

    private Task AddAsync(Recipe recipe) => SaveAsync([recipe]);

    private Task AddAllAsync() =>
        SaveAsync(results!.Select(m => m.Recipe).Where(r => !IsAdded(r)).ToList());

    private async Task SaveAsync(List<Recipe> toSave)
    {
        saving = true;
        error = null;
        try
        {
            await Recipes.SaveManyAsync(toSave);
            foreach (var r in toSave.Where(r => r.ExternalId is not null))
                savedIds.Add(r.ExternalId!);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        finally
        {
            saving = false;
        }
    }
}
