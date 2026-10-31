using DietTracker.Core;

namespace DietTracker.Web.Services;

public class PlanService(SupabaseApi api)
{
    public Task<List<PlanEntry>> GetRangeAsync(DateOnly from, DateOnly to) =>
        api.GetAsync<PlanEntry>(
            $"rest/v1/plan_entries?select=*&meal_type=eq.{MealType.Lunch}&date=gte.{Fmt.Iso(from)}&date=lte.{Fmt.Iso(to)}&order=date.asc");

    /// <summary>Upserts by (date, meal_type). Callers pass the existing entry's Id so the primary key never changes.</summary>
    public Task UpsertAsync(List<PlanEntry> entries) =>
        entries.Count == 0
            ? Task.CompletedTask
            : api.WriteAsync(HttpMethod.Post, "rest/v1/plan_entries?on_conflict=date,meal_type", entries,
                "resolution=merge-duplicates,return=minimal");

    public Task DeleteAsync(Guid id) => api.DeleteAsync($"rest/v1/plan_entries?id=eq.{id}");
}
