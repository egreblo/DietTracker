using System.Text.Json;
using DietTracker.Core;

namespace DietTracker.Tests;

public class MealDbMapperTests
{
    private const string Sample = """
        {
          "idMeal": "52940",
          "strMeal": "Brown Stew Chicken",
          "strCategory": "Chicken",
          "strArea": "Jamaican",
          "strInstructions": "Squeeze lime over chicken.\r\n\r\nSimmer for 30 minutes.",
          "strMealThumb": "https://www.themealdb.com/images/media/meals/sypxpx1515365095.jpg",
          "strYoutube": "https://www.youtube.com/watch?v=_gFB1fkNhXs",
          "strSource": "",
          "strIngredient1": "Chicken",
          "strIngredient2": "Tomato",
          "strIngredient3": "Onions",
          "strIngredient4": "Garlic Clove",
          "strIngredient5": "Salt",
          "strIngredient6": "Olive Oil",
          "strIngredient7": "",
          "strIngredient8": null,
          "strMeasure1": "1 whole",
          "strMeasure2": "1 chopped",
          "strMeasure3": "2 chopped",
          "strMeasure4": "2 chopped",
          "strMeasure5": "Pinch",
          "strMeasure6": "2 tbsp",
          "strMeasure7": " ",
          "strMeasure8": null
        }
        """;

    private static MealDbMeal Parse(string json) => MealDbMapper.Map(JsonDocument.Parse(json).RootElement);

    [Fact]
    public void MapsBasicFields()
    {
        var meal = Parse(Sample);
        var r = meal.Recipe;

        Assert.Equal("Brown Stew Chicken", r.Name);
        Assert.Equal(RecipeSource.Imported, r.Source);
        Assert.Equal("52940", r.ExternalId);
        Assert.Equal("Chicken", meal.Category);
        Assert.Equal("Jamaican", meal.Area);
        Assert.Equal("https://www.youtube.com/watch?v=_gFB1fkNhXs", r.LinkUrl);
        Assert.StartsWith("https://www.themealdb.com/images", r.ImageUrl);
        Assert.Equal(MealDbMapper.AssumedServings, r.Servings);
        Assert.Equal("Squeeze lime over chicken.\n\nSimmer for 30 minutes.", r.Instructions);
    }

    [Fact]
    public void SkipsBlankIngredientsAndKeepsMeasureText()
    {
        var r = Parse(Sample).Recipe;

        Assert.Equal(6, r.Ingredients.Count);
        Assert.Equal("1 whole", r.Ingredients[0].Note);
        Assert.Equal("Garlic Clove", r.Ingredients[3].Name);
    }

    [Fact]
    public void ConvertsMeasuresToGramsWhereItCan()
    {
        var r = Parse(Sample).Recipe;

        Assert.Equal(30, r.Ingredients[5].Grams);   // 2 tbsp olive oil
        Assert.Equal(6, r.Ingredients[3].Grams);    // "2 chopped" garlic cloves -> 2 x 3 g
        Assert.Equal(220, r.Ingredients[2].Grams);  // "2 chopped" onions -> 2 x 110 g
        Assert.Null(r.Ingredients[0].Grams);        // "1 whole" chicken: no piece weight known
    }

    [Fact]
    public void FallsBackToSourceLinkWhenThereIsNoVideo()
    {
        var json = Sample.Replace("\"strYoutube\": \"https://www.youtube.com/watch?v=_gFB1fkNhXs\"", "\"strYoutube\": \"\"")
                         .Replace("\"strSource\": \"\"", "\"strSource\": \"https://example.com/recipe\"");

        Assert.Equal("https://example.com/recipe", Parse(json).Recipe.LinkUrl);
    }

    [Fact]
    public void EstimatesNutritionOnImport()
    {
        var r = Parse(Sample).Recipe;

        Assert.True(r.MacrosEstimated);
        Assert.NotNull(r.Kcal);
        Assert.True(r.Kcal > 0);
    }
}
