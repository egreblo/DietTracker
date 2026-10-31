using DietTracker.Core;

namespace DietTracker.Tests;

public class NutritionEstimatorTests
{
    [Theory]
    [InlineData("Chicken Breasts", "chicken breast")]
    [InlineData("Olive Oil", "olive oil")]
    [InlineData("Extra virgin olive oil", "olive oil")]
    [InlineData("Red Onions", "onion")]
    [InlineData("Tomatoes", "tomato")]
    [InlineData("Chicken Stock", "stock")]
    [InlineData("Garlic Clove", "garlic")]
    [InlineData("Kidney Beans", "kidney bean")]
    [InlineData("Češnjak", "garlic")]
    [InlineData("pileća prsa", "chicken breast")]
    [InlineData("Plain Flour", "flour")]
    [InlineData("Red Pepper Flakes", "salt")]
    public void MatchesIngredientNames(string name, string expectedFirstAlias)
    {
        var food = FoodTable.Find(name);
        Assert.NotNull(food);
        Assert.Equal(expectedFirstAlias, food!.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unobtainium")]
    public void UnknownNamesDoNotMatch(string name)
    {
        Assert.Null(FoodTable.Find(name));
    }

    [Fact]
    public void EstimatesPerServing()
    {
        var ingredients = new List<Ingredient>
        {
            new() { Name = "Chicken breast", Grams = 400 }, // 480 kcal, 90 P
            new() { Name = "Rice", Grams = 200 },           // 720 kcal, 14 P, 158 C
            new() { Name = "Olive oil", Note = "2 tbsp" },  // 30 g -> 265 kcal, 30 F
        };

        var e = NutritionEstimator.Estimate(ingredients, servings: 2);

        Assert.True(e.HasData);
        Assert.Equal(3, e.Counted);
        Assert.Empty(e.Skipped);
        Assert.Equal(733, e.Kcal!.Value, 1);   // (480 + 720 + 265.2) / 2
        Assert.Equal(52, e.Protein!.Value, 1); // (90 + 14) / 2
        Assert.Equal(79, e.Carbs!.Value, 1);
    }

    [Fact]
    public void ReportsIngredientsItCouldNotCount()
    {
        var ingredients = new List<Ingredient>
        {
            new() { Name = "Rice", Grams = 100 },
            new() { Name = "Unobtainium", Grams = 50 },
            new() { Name = "Carrot", Note = "some" },
            new() { Name = "Salt", Note = "to taste" },
        };

        var e = NutritionEstimator.Estimate(ingredients, 1);

        Assert.Equal(["Unobtainium", "Carrot"], e.Skipped);
        Assert.Equal(2, e.Counted); // rice + salt (seasoning without quantity counts as zero)
        Assert.Equal(4, e.Total);
        Assert.Equal(360, e.Kcal!.Value, 1);
    }

    [Fact]
    public void SunDriedTomatoesAreNotFreshTomatoes()
    {
        var e = NutritionEstimator.Estimate([new Ingredient { Name = "Sun-Dried Tomatoes", Note = "8" }], 1);

        Assert.Equal(165, e.Kcal!.Value, 0); // 8 x 8 g at 258 kcal/100 g
    }

    [Fact]
    public void CupOfFlourWeighsLessThanCupOfWater()
    {
        var e = NutritionEstimator.Estimate([new Ingredient { Name = "Flour", Note = "1/2 cup" }], 1);

        Assert.Equal(228, e.Kcal!.Value, 0); // 62.5 g at 364 kcal/100 g
    }

    [Fact]
    public void LargeAmountsOfOilAreTreatedAsFryingOil()
    {
        var frying = NutritionEstimator.Estimate([new Ingredient { Name = "Vegetable Oil", Note = "2 cups" }], 1);
        var dressing = NutritionEstimator.Estimate([new Ingredient { Name = "Olive Oil", Note = "2 tbsp" }], 1);

        Assert.Equal(636, frying.Kcal!.Value, 0);  // 480 g x 15% absorbed
        Assert.Equal(265, dressing.Kcal!.Value, 0); // 30 g, counted in full
    }

    [Fact]
    public void NoUsableIngredientsGivesNoData()
    {
        var e = NutritionEstimator.Estimate([new Ingredient { Name = "Unobtainium", Grams = 10 }], 1);
        Assert.False(e.HasData);
    }

    [Fact]
    public void ApplyFillsRecipeAndMarksItEstimated()
    {
        var recipe = new Recipe
        {
            Servings = 1,
            Ingredients = [new Ingredient { Name = "Egg", Note = "2" }], // 2 x 50 g
        };

        NutritionEstimator.Apply(recipe);

        Assert.Equal(143, recipe.Kcal);
        Assert.True(recipe.MacrosEstimated);
    }

    [Fact]
    public void ApplyLeavesRecipeUntouchedWithoutData()
    {
        var recipe = new Recipe { Kcal = 500, Ingredients = [new Ingredient { Name = "Unobtainium", Grams = 5 }] };

        NutritionEstimator.Apply(recipe);

        Assert.Equal(500, recipe.Kcal);
        Assert.False(recipe.MacrosEstimated);
    }
}
