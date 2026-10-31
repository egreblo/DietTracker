namespace DietTracker.Core;

/// <summary>
/// Random lunch scheduling. Pure functions: all randomness comes from the supplied <see cref="Random"/>.
/// </summary>
public static class MealGenerator
{
    /// <summary>
    /// Picks a recipe id for every day in <paramref name="daysToFill"/>.
    /// Preference order per day: not yet used this week and not eaten last week,
    /// not yet used this week, anything except yesterday's pick, anything.
    /// </summary>
    /// <param name="pool">Recipes available for scheduling.</param>
    /// <param name="daysToFill">Days that need a recipe (locked days must not be included).</param>
    /// <param name="alreadyUsed">Recipe ids already placed this week (e.g. on locked days).</param>
    /// <param name="lastWeek">Recipe ids eaten last week; avoided when possible.</param>
    public static Dictionary<DateOnly, Guid> Generate(
        IReadOnlyList<Recipe> pool,
        IEnumerable<DateOnly> daysToFill,
        IEnumerable<Guid> alreadyUsed,
        IEnumerable<Guid> lastWeek,
        Random rng)
    {
        var result = new Dictionary<DateOnly, Guid>();
        if (pool.Count == 0) return result;

        var used = new HashSet<Guid>(alreadyUsed);
        var avoid = new HashSet<Guid>(lastWeek);
        Guid? previous = null;

        foreach (var day in daysToFill.OrderBy(d => d))
        {
            var candidates = pool.Where(r => !used.Contains(r.Id) && !avoid.Contains(r.Id)).ToList();
            if (candidates.Count == 0)
                candidates = pool.Where(r => !used.Contains(r.Id)).ToList();
            if (candidates.Count == 0)
            {
                // Pool exhausted: start a new round, but never serve the same dish two days in a row.
                used.Clear();
                candidates = pool.Where(r => r.Id != previous).ToList();
                if (candidates.Count == 0) candidates = pool.ToList();
            }

            var pick = candidates[rng.Next(candidates.Count)];
            used.Add(pick.Id);
            result[day] = pick.Id;
            previous = pick.Id;
        }

        return result;
    }

    /// <summary>Picks a single replacement for one slot, avoiding dishes already in the week.</summary>
    public static Guid? PickReplacement(
        IReadOnlyList<Recipe> pool,
        IEnumerable<Guid> usedInWeek,
        Guid? current,
        Random rng)
    {
        var used = new HashSet<Guid>(usedInWeek);
        var candidates = pool.Where(r => !used.Contains(r.Id)).ToList();
        if (candidates.Count == 0) candidates = pool.Where(r => r.Id != current).ToList();
        if (candidates.Count == 0) return null;
        return candidates[rng.Next(candidates.Count)].Id;
    }
}
