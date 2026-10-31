namespace DietTracker.Core;

/// <summary>Per-serving estimate. <see cref="Skipped"/> lists ingredients that could not be counted (unknown food or quantity).</summary>
public record NutritionEstimate(
    double? Kcal, double? Protein, double? Carbs, double? Fat,
    int Counted, int Total, IReadOnlyList<string> Skipped)
{
    public bool HasData => Kcal.HasValue;
}

public static class NutritionEstimator
{
    private const double FryingOilThresholdGrams = 100;
    private const double FryingOilAbsorbed = 0.15;

    public static NutritionEstimate Estimate(IEnumerable<Ingredient> ingredients, int servings)
    {
        double kcal = 0, protein = 0, carbs = 0, fat = 0;
        var counted = 0;
        var total = 0;
        var skipped = new List<string>();

        foreach (var ingredient in ingredients.Where(i => !string.IsNullOrWhiteSpace(i.Name)))
        {
            total++;
            var food = FoodTable.Find(ingredient.Name);
            if (food is null)
            {
                skipped.Add(ingredient.Name.Trim());
                continue;
            }

            var grams = ingredient.Grams ?? MeasureParser.ParseGrams(ingredient.Note, food.PieceGrams, food.CupGrams);
            if (grams is null)
            {
                if (food.Seasoning) counted++;
                else skipped.Add(ingredient.Name.Trim());
                continue;
            }

            // More than ~7 tbsp of oil in a recipe is deep-frying oil, most of which stays in the pan.
            if (food.FryingOil && grams > FryingOilThresholdGrams)
                grams *= FryingOilAbsorbed;

            var factor = grams.Value / 100.0;
            kcal += food.Kcal * factor;
            protein += food.Protein * factor;
            carbs += food.Carbs * factor;
            fat += food.Fat * factor;
            counted++;
        }

        if (counted == 0 || kcal <= 0)
            return new NutritionEstimate(null, null, null, null, counted, total, skipped);

        var per = Math.Max(1, servings);
        return new NutritionEstimate(
            Math.Round(kcal / per),
            Math.Round(protein / per, 1),
            Math.Round(carbs / per, 1),
            Math.Round(fat / per, 1),
            counted, total, skipped);
    }

    /// <summary>Writes the estimate into the recipe (only when there is data) and returns it.</summary>
    public static NutritionEstimate Apply(Recipe recipe)
    {
        var estimate = Estimate(recipe.Ingredients, recipe.Servings);
        if (!estimate.HasData) return estimate;

        recipe.Kcal = estimate.Kcal;
        recipe.Protein = estimate.Protein;
        recipe.Carbs = estimate.Carbs;
        recipe.Fat = estimate.Fat;
        recipe.MacrosEstimated = true;
        return estimate;
    }
}
