using DietTracker.Core;

namespace DietTracker.Tests;

public class MealGeneratorTests
{
    private static List<Recipe> Pool(int n) =>
        Enumerable.Range(1, n).Select(i => new Recipe { Name = $"Jelo {i}" }).ToList();

    private static List<DateOnly> Week() =>
        Enumerable.Range(0, 7).Select(i => new DateOnly(2026, 11, 2).AddDays(i)).ToList();

    [Fact]
    public void FillsEveryDayWithoutRepeatsWhenPoolIsLargeEnough()
    {
        var pool = Pool(10);
        for (var seed = 0; seed < 50; seed++)
        {
            var result = MealGenerator.Generate(pool, Week(), [], [], new Random(seed));
            Assert.Equal(7, result.Count);
            Assert.Equal(7, result.Values.Distinct().Count());
        }
    }

    [Fact]
    public void AvoidsLastWeekWhenThereAreEnoughOtherDishes()
    {
        var pool = Pool(14);
        var lastWeek = pool.Take(7).Select(r => r.Id).ToList();
        for (var seed = 0; seed < 50; seed++)
        {
            var result = MealGenerator.Generate(pool, Week(), [], lastWeek, new Random(seed));
            Assert.DoesNotContain(result.Values, id => lastWeek.Contains(id));
        }
    }

    [Fact]
    public void FallsBackToLastWeekWhenNeeded()
    {
        var pool = Pool(7);
        var result = MealGenerator.Generate(pool, Week(), [], pool.Select(r => r.Id), new Random(1));
        Assert.Equal(7, result.Count);
        Assert.Equal(7, result.Values.Distinct().Count());
    }

    [Fact]
    public void RespectsAlreadyUsedFromLockedDays()
    {
        var pool = Pool(8);
        var locked = pool[0].Id;
        var days = Week().Skip(1).ToList();
        for (var seed = 0; seed < 50; seed++)
        {
            var result = MealGenerator.Generate(pool, days, [locked], [], new Random(seed));
            Assert.Equal(6, result.Count);
            Assert.DoesNotContain(locked, result.Values);
        }
    }

    [Fact]
    public void SmallPoolRepeatsButNeverOnConsecutiveDays()
    {
        var pool = Pool(3);
        for (var seed = 0; seed < 100; seed++)
        {
            var days = Week();
            var result = MealGenerator.Generate(pool, days, [], [], new Random(seed));
            Assert.Equal(7, result.Count);
            for (var i = 1; i < days.Count; i++)
                Assert.NotEqual(result[days[i - 1]], result[days[i]]);
        }
    }

    [Fact]
    public void SingleDishPoolStillFillsWeek()
    {
        var result = MealGenerator.Generate(Pool(1), Week(), [], [], new Random(0));
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void EmptyPoolGivesEmptyResult()
    {
        Assert.Empty(MealGenerator.Generate([], Week(), [], [], new Random(0)));
    }

    [Fact]
    public void ReplacementPrefersDishesNotInWeek()
    {
        var pool = Pool(8);
        var used = pool.Take(7).Select(r => r.Id).ToList();
        var pick = MealGenerator.PickReplacement(pool, used, used[0], new Random(0));
        Assert.Equal(pool[7].Id, pick);
    }

    [Fact]
    public void ReplacementNeverReturnsCurrentWhenAlternativesExist()
    {
        var pool = Pool(3);
        var used = pool.Select(r => r.Id).ToList();
        for (var seed = 0; seed < 50; seed++)
        {
            var pick = MealGenerator.PickReplacement(pool, used, pool[0].Id, new Random(seed));
            Assert.NotEqual(pool[0].Id, pick);
        }
    }

    [Fact]
    public void ReplacementIsNullWhenOnlyCurrentExists()
    {
        var pool = Pool(1);
        Assert.Null(MealGenerator.PickReplacement(pool, [pool[0].Id], pool[0].Id, new Random(0)));
    }
}
