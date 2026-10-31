using DietTracker.Core;
using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;

namespace DietTracker.Web.Pages;

public partial class JeloUredi
{
    [Inject] private RecipeService Recipes { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Parameter] public Guid? Id { get; set; }

    private Recipe? recipe;
    private bool notFound;
    private bool busy;
    private bool confirmDelete;
    private string? error;
    private string? estimateMessage;

    private bool IsNew => Id is null;

    private void MacrosEdited() => recipe!.MacrosEstimated = false;

    private void EstimateMacros()
    {
        if (recipe is null) return;
        recipe.Servings = Math.Max(1, recipe.Servings);

        // Only look at ingredients that have a name; blank rows are dropped on save anyway.
        var estimate = NutritionEstimator.Estimate(recipe.Ingredients, recipe.Servings);
        if (!estimate.HasData)
        {
            estimateMessage = L["Nije moguće procijeniti: dodaj sastojke s količinom (grami ili mjera)."];
            return;
        }

        NutritionEstimator.Apply(recipe);
        estimateMessage = L["Procijenjeno iz {0} od {1} sastojaka.", estimate.Counted, estimate.Total];
        if (estimate.Skipped.Count > 0)
            estimateMessage += " " + L["Nije uračunato: {0}.", string.Join(", ", estimate.Skipped)];
    }

    protected override async Task OnParametersSetAsync()
    {
        recipe = null;
        notFound = false;
        confirmDelete = false;
        error = null;

        if (Id is null)
        {
            recipe = new Recipe { Ingredients = [new Ingredient()] };
            return;
        }

        try
        {
            recipe = await Recipes.GetAsync(Id.Value);
            notFound = recipe is null;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            notFound = true;
        }
    }

    private void AddIngredient() => recipe!.Ingredients.Add(new Ingredient());

    private static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    private async Task SaveAsync()
    {
        if (recipe is null) return;
        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            error = L["Unesi naziv jela."];
            return;
        }

        recipe.Name = recipe.Name.Trim();
        recipe.Servings = Math.Max(1, recipe.Servings);
        recipe.Ingredients = recipe.Ingredients.Where(i => !string.IsNullOrWhiteSpace(i.Name)).ToList();
        foreach (var ing in recipe.Ingredients)
            ing.Note = Blank(ing.Note);
        recipe.Instructions = Blank(recipe.Instructions);
        recipe.LinkUrl = Blank(recipe.LinkUrl);
        if (recipe.LinkUrl is not null && !IsHttpUrl(recipe.LinkUrl))
        {
            error = L["Link mora početi s http:// ili https://."];
            return;
        }

        await RunAsync(async () =>
        {
            await Recipes.SaveAsync(recipe);
            Nav.NavigateTo(IsNew ? "jela" : $"jela/{recipe.Id}");
        });
    }

    private Task DeleteAsync() => RunAsync(async () =>
    {
        await Recipes.DeleteAsync(recipe!.Id);
        Nav.NavigateTo("jela");
    });

    private async Task RunAsync(Func<Task> action)
    {
        busy = true;
        error = null;
        try { await action(); }
        catch (Exception ex) { error = ex.Message; }
        finally { busy = false; }
    }

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
