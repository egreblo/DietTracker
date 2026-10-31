using System.Text.Json;
using DietTracker.Core;

namespace DietTracker.Tests;

public class JsonTests
{
    [Fact]
    public void RecipeUsesSnakeCaseColumnNames()
    {
        var recipe = new Recipe
        {
            Name = "Pasta sa tunom",
            InPool = true,
            LinkUrl = "https://example.com",
            Ingredients = [new Ingredient { Name = "Tuna", Grams = 120 }],
        };

        var json = JsonSerializer.Serialize(recipe, Json.Options);

        Assert.Contains("\"in_pool\":true", json);
        Assert.Contains("\"link_url\":\"https://example.com\"", json);
        Assert.Contains("\"macros_estimated\":false", json);
        Assert.Contains("\"ingredients\":[{\"name\":\"Tuna\",\"grams\":120,\"note\":null}]", json);
        Assert.DoesNotContain("created_at", json); // default value is left to the database
    }

    [Fact]
    public void RecipeDeserializesPostgrestRow()
    {
        const string row = """
            {"id":"6f0f0c10-0c5a-4b52-9a9b-5d7b7d6a1111","name":"Piletina","source":"imported","in_pool":false,
             "ingredients":[{"name":"Piletina","grams":200.5,"note":null}],"instructions":null,"link_url":null,
             "image_url":null,"servings":2,"kcal":512.5,"protein":null,"carbs":null,"fat":null,
             "macros_estimated":true,"external_id":"52772","created_at":"2026-11-01T10:00:00.123456+00:00"}
            """;

        var recipe = JsonSerializer.Deserialize<Recipe>(row, Json.Options)!;

        Assert.Equal("Piletina", recipe.Name);
        Assert.Equal(RecipeSource.Imported, recipe.Source);
        Assert.False(recipe.InPool);
        Assert.Equal(200.5, recipe.Ingredients[0].Grams);
        Assert.Equal(2, recipe.Servings);
        Assert.Equal(512.5, recipe.Kcal);
        Assert.True(recipe.MacrosEstimated);
        Assert.Equal("52772", recipe.ExternalId);
        Assert.Equal(2026, recipe.CreatedAt.Year);
    }

    [Fact]
    public void PlanEntryRoundTripsDateAsIso()
    {
        var entry = new PlanEntry { Date = new DateOnly(2026, 11, 2), RecipeId = null, Locked = true };

        var json = JsonSerializer.Serialize(entry, Json.Options);
        Assert.Contains("\"date\":\"2026-11-02\"", json);
        Assert.Contains("\"meal_type\":\"lunch\"", json);
        Assert.Contains("\"recipe_id\":null", json);

        var back = JsonSerializer.Deserialize<PlanEntry>(json, Json.Options)!;
        Assert.Equal(entry.Date, back.Date);
        Assert.True(back.Locked);
    }
}
