using DietTracker.Core;
using DietTracker.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DietTracker.Web.Pages;

public partial class Tjedan
{
    [Inject] private RecipeService Recipes { get; set; } = default!;
    [Inject] private PlanService Plan { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private DateOnly today;
    private DateOnly weekStart;
    private List<Recipe> recipes = [];
    private List<Recipe> pool = [];
    private Dictionary<DateOnly, PlanEntry> entries = [];
    private List<Guid> lastWeek = [];

    private bool loading = true;
    private bool busy;
    private bool confirmGenerate;
    private DateOnly? swapFrom;
    private string? error;

    private IEnumerable<DateOnly> Days => Enumerable.Range(0, 7).Select(weekStart.AddDays);

    private int unlockedWithRecipe =>
        entries.Values.Count(e => !e.Locked && e.RecipeId is not null);

    protected override async Task OnInitializedAsync()
    {
        var iso = await JS.InvokeAsync<string>("dt.today");
        today = DateOnly.ParseExact(iso, "yyyy-MM-dd");
        weekStart = Fmt.MondayOf(today);
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        loading = true;
        error = null;
        swapFrom = null;
        confirmGenerate = false;
        try
        {
            recipes = await Recipes.GetAllAsync(force: true);
            pool = recipes.Where(r => r.InPool).ToList();
            var thisWeek = await Plan.GetRangeAsync(weekStart, weekStart.AddDays(6));
            entries = thisWeek.ToDictionary(e => e.Date);
            var previous = await Plan.GetRangeAsync(weekStart.AddDays(-7), weekStart.AddDays(-1));
            lastWeek = previous.Where(e => e.RecipeId is not null).Select(e => e.RecipeId!.Value).ToList();
        }
        catch (Exception ex)
        {
            error = ex.Message;
            entries = [];
        }
        loading = false;
    }

    private Task ShiftWeek(int weeks)
    {
        weekStart = weekStart.AddDays(7 * weeks);
        return LoadAsync();
    }

    private Task ThisWeek()
    {
        weekStart = Fmt.MondayOf(today);
        return LoadAsync();
    }

    /// <summary>Daily average of the lunches that have nutrition data (null when none do).</summary>
    private string? Summary()
    {
        var withData = Days.Select(d => RecipeOf(Entry(d))).Where(r => r?.Kcal is not null).Select(r => r!).ToList();
        if (withData.Count == 0) return null;

        var n = withData.Count;
        var scheduled = Days.Count(d => RecipeOf(Entry(d)) is not null);
        return L["Ručkovi, prosjek po danu: ~{0} kcal · B {1} g · U {2} g · M {3} g ({4} od {5} jela ima podatke)",
            Fmt.Number(withData.Sum(r => r.Kcal ?? 0) / n),
            Fmt.Number(withData.Sum(r => r.Protein ?? 0) / n),
            Fmt.Number(withData.Sum(r => r.Carbs ?? 0) / n),
            Fmt.Number(withData.Sum(r => r.Fat ?? 0) / n),
            n, scheduled];
    }

    private PlanEntry? Entry(DateOnly day) => entries.GetValueOrDefault(day);

    private Recipe? RecipeOf(PlanEntry? e) =>
        e?.RecipeId is { } id ? recipes.FirstOrDefault(r => r.Id == id) : null;

    /// <summary>Pool recipes plus the one currently scheduled (it may have left the pool since).</summary>
    private IEnumerable<Recipe> OptionsFor(Recipe? current) =>
        recipes.Where(r => r.InPool || r.Id == current?.Id).OrderBy(r => r.Name);

    private PlanEntry Draft(DateOnly day) =>
        entries.TryGetValue(day, out var e) ? e.Clone() : new PlanEntry { Date = day };

    private IEnumerable<Guid> UsedInWeek() =>
        entries.Values.Select(e => e.RecipeId).OfType<Guid>();

    private async Task RunAsync(Func<Task> action)
    {
        busy = true;
        error = null;
        try { await action(); }
        catch (Exception ex) { error = ex.Message; }
        finally { busy = false; }
    }

    private Task SaveAsync(params PlanEntry[] changed) => RunAsync(async () =>
    {
        await Plan.UpsertAsync(changed.ToList());
        foreach (var e in changed) entries[e.Date] = e;
    });

    private Task OnGenerateClick()
    {
        if (unlockedWithRecipe > 0)
        {
            confirmGenerate = true;
            return Task.CompletedTask;
        }
        return GenerateAsync();
    }

    private Task GenerateAsync()
    {
        confirmGenerate = false;
        return RunAsync(async () =>
        {
            var lockedDays = Days.Where(d => entries.TryGetValue(d, out var e) && e.Locked).ToHashSet();
            var toFill = Days.Where(d => !lockedDays.Contains(d)).ToList();
            var usedByLocked = lockedDays.Select(d => entries[d].RecipeId).OfType<Guid>();

            var picks = MealGenerator.Generate(pool, toFill, usedByLocked, lastWeek, Random.Shared);

            var batch = toFill.Where(picks.ContainsKey).Select(d =>
            {
                var e = Draft(d);
                e.RecipeId = picks[d];
                return e;
            }).ToList();

            await Plan.UpsertAsync(batch);
            foreach (var e in batch) entries[e.Date] = e;
        });
    }

    private Task RerollAsync(DateOnly day)
    {
        var e = Draft(day);
        var pick = MealGenerator.PickReplacement(pool, UsedInWeek(), e.RecipeId, Random.Shared);
        if (pick is null) return Task.CompletedTask;
        e.RecipeId = pick;
        return SaveAsync(e);
    }

    private Task SetRecipeAsync(DateOnly day, string? value)
    {
        var e = Draft(day);
        if (Guid.TryParse(value, out var id))
        {
            e.RecipeId = id;
        }
        else
        {
            // "— odaberi —" clears a recipe but keeps a locked "no lunch" day as it is.
            if (e.Locked && e.RecipeId is null) return Task.CompletedTask;
            return ClearAsync(day);
        }
        return SaveAsync(e);
    }

    private Task ToggleLockAsync(DateOnly day)
    {
        var e = Draft(day);
        e.Locked = !e.Locked;
        return SaveAsync(e);
    }

    private Task SkipAsync(DateOnly day)
    {
        var e = Draft(day);
        e.RecipeId = null;
        e.Locked = true;
        return SaveAsync(e);
    }

    private Task ClearAsync(DateOnly day)
    {
        if (!entries.TryGetValue(day, out var existing)) return Task.CompletedTask;
        return RunAsync(async () =>
        {
            await Plan.DeleteAsync(existing.Id);
            entries.Remove(day);
        });
    }

    private Task SwapClickAsync(DateOnly day)
    {
        if (swapFrom is null)
        {
            swapFrom = day;
            return Task.CompletedTask;
        }
        if (swapFrom == day)
        {
            swapFrom = null;
            return Task.CompletedTask;
        }

        var a = Draft(swapFrom.Value);
        var b = Draft(day);
        (a.RecipeId, b.RecipeId) = (b.RecipeId, a.RecipeId);
        swapFrom = null;
        return SaveAsync(a, b);
    }

    private async Task DownloadIcs()
    {
        var events = Days
            .Select(d => (Day: d, Entry: Entry(d)))
            .Where(x => x.Entry?.RecipeId is not null)
            .Select(x => (x.Day, x.Entry, Recipe: RecipeOf(x.Entry)))
            .Where(x => x.Recipe is not null)
            .Select(x => new IcsEvent(
                $"{x.Entry!.Id}@diettracker",
                x.Day,
                new TimeOnly(12, 0),
                TimeSpan.FromHours(1),
                $"RUČAK - {x.Recipe!.Name}",
                Describe(x.Recipe)))
            .ToList();

        if (events.Count == 0) return;

        var ics = IcsBuilder.Build(events, DateTime.UtcNow);
        await JS.InvokeVoidAsync("dt.download", $"rucak-{Fmt.Iso(weekStart)}.ics", ics, "text/calendar;charset=utf-8");
    }

    private static string Describe(Recipe r)
    {
        var parts = new List<string>();
        if (r.Ingredients.Count > 0)
            parts.Add("Sastojci: " + string.Join(", ", r.Ingredients.Select(i => i.Name)));
        if (!string.IsNullOrWhiteSpace(r.LinkUrl))
            parts.Add(r.LinkUrl);
        return string.Join("\n", parts);
    }
}
