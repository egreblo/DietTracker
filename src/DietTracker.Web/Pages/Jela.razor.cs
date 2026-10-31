using DietTracker.Core;
using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;

namespace DietTracker.Web.Pages;

public partial class Jela
{
    [Inject] private RecipeService Recipes { get; set; } = default!;

    private static readonly (string Value, string Label)[] Filters =
        [("all", "Sva"), ("pool", "U skupu"), ("manual", "Moja"), ("imported", "S weba")];

    private List<Recipe>? all;
    private string query = "";
    private string filter = "all";
    private string? error;

    private int PoolCount => all?.Count(r => r.InPool) ?? 0;

    private IEnumerable<Recipe> Visible => (all ?? []).Where(r =>
        (filter switch
        {
            "pool" => r.InPool,
            "manual" => r.Source == RecipeSource.Manual,
            "imported" => r.Source == RecipeSource.Imported,
            _ => true,
        })
        && Matches(r));

    private bool Matches(Recipe r) =>
        string.IsNullOrWhiteSpace(query)
        || r.Name.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase)
        || r.Ingredients.Any(i => i.Name.Contains(query.Trim(), StringComparison.CurrentCultureIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        try { all = await Recipes.GetAllAsync(force: true); }
        catch (Exception ex) { error = ex.Message; all = []; }
    }

    private async Task TogglePool(Recipe recipe, bool inPool)
    {
        try
        {
            error = null;
            await Recipes.SetPoolAsync(recipe, inPool);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
    }
}
