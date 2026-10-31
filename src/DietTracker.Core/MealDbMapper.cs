using System.Text.Json;

namespace DietTracker.Core;

public record MealDbMeal(Recipe Recipe, string? Category, string? Area);

/// <summary>Maps a TheMealDB meal JSON object (strMeal, strIngredient1..20, ...) to a <see cref="Recipe"/>.</summary>
public static class MealDbMapper
{
    /// <summary>TheMealDB does not say how many people a dish feeds; four is the typical recipe size.</summary>
    public const int AssumedServings = 4;

    public static MealDbMeal Map(JsonElement meal)
    {
        var ingredients = new List<Ingredient>();
        for (var i = 1; i <= 20; i++)
        {
            var name = Text(meal, $"strIngredient{i}");
            if (name is null) continue;

            var measure = Text(meal, $"strMeasure{i}");
            var food = FoodTable.Find(name);
            ingredients.Add(new Ingredient
            {
                Name = name,
                Note = measure,
                Grams = MeasureParser.ParseGrams(measure, food?.PieceGrams, food?.CupGrams) is { } g
                    ? Math.Round(g, 1)
                    : null,
            });
        }

        var recipe = new Recipe
        {
            Name = Text(meal, "strMeal") ?? "",
            Source = RecipeSource.Imported,
            InPool = true,
            Ingredients = ingredients,
            Instructions = Text(meal, "strInstructions")?.Replace("\r\n", "\n"),
            LinkUrl = Text(meal, "strYoutube") ?? Text(meal, "strSource"),
            ImageUrl = Text(meal, "strMealThumb"),
            ExternalId = Text(meal, "idMeal"),
            Servings = AssumedServings,
        };
        NutritionEstimator.Apply(recipe);

        return new MealDbMeal(recipe, Text(meal, "strCategory"), Text(meal, "strArea"));
    }

    private static string? Text(JsonElement obj, string property)
    {
        if (!obj.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
            return null;
        var text = value.GetString()?.Trim();
        return string.IsNullOrEmpty(text) ? null : text;
    }
}
