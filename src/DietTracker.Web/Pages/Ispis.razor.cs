using DietTracker.Core;
using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DietTracker.Web.Pages;

public partial class Ispis
{
    [Inject] private RecipeService Recipes { get; set; } = default!;
    [Inject] private PlanService Plan { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Parameter, SupplyParameterFromQuery(Name = "od")] public string? From { get; set; }

    private DateOnly weekStart;
    private List<Recipe> recipes = [];
    private Dictionary<DateOnly, PlanEntry> entries = [];
    private bool loading = true;
    private bool showIngredients;
    private bool showPreparation;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        if (!DateOnly.TryParseExact(From, "yyyy-MM-dd", out var start))
            start = DateOnly.ParseExact(await JS.InvokeAsync<string>("dt.today"), "yyyy-MM-dd");
        weekStart = Fmt.MondayOf(start);

        try
        {
            recipes = await Recipes.GetAllAsync();
            var list = await Plan.GetRangeAsync(weekStart, weekStart.AddDays(6));
            entries = list.ToDictionary(e => e.Date);
        }
        catch (Exception ex)
        {
            error = ex.Message;
        }
        loading = false;
    }

    private Recipe? RecipeFor(DateOnly day) =>
        entries.TryGetValue(day, out var e) && e.RecipeId is { } id
            ? recipes.FirstOrDefault(r => r.Id == id)
            : null;

    private static string Describe(Ingredient i) =>
        i.Grams is { } g ? $"{i.Name} ({Fmt.Number(g)} g)" : i.Name;

    private Task Print() => JS.InvokeVoidAsync("dt.print").AsTask();
}
