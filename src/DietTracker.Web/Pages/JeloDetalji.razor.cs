using DietTracker.Core;
using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;

namespace DietTracker.Web.Pages;

public partial class JeloDetalji
{
    [Inject] private RecipeService Recipes { get; set; } = default!;

    [Parameter] public Guid Id { get; set; }

    private Recipe? recipe;
    private bool notFound;
    private string? error;

    protected override async Task OnParametersSetAsync()
    {
        recipe = null;
        notFound = false;
        error = null;
        try
        {
            recipe = await Recipes.GetAsync(Id);
            notFound = recipe is null;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            notFound = true;
        }
    }

    private async Task TogglePool(bool inPool)
    {
        try { await Recipes.SetPoolAsync(recipe!, inPool); }
        catch (Exception ex) { error = ex.Message; }
    }

    private static string Quantity(Ingredient i)
    {
        if (!string.IsNullOrWhiteSpace(i.Note)) return i.Note;
        return i.Grams is { } g ? $"{Fmt.Number(g)} g" : "";
    }

    /// <summary>Instructions are plain text; every non-empty line becomes a paragraph.</summary>
    private static IEnumerable<string> Paragraphs(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool IsHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";

    private string LinkLabel(string url)
    {
        var host = new Uri(url).Host;
        return host.Contains("youtube") || host.Contains("youtu.be") ? L["Otvori video"] : L["Otvori link"];
    }
}
