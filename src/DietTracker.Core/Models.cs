using System.Text.Json.Serialization;

namespace DietTracker.Core;

public static class RecipeSource
{
    public const string Manual = "manual";
    public const string Imported = "imported";
}

public static class MealType
{
    public const string Lunch = "lunch";
}

public class Ingredient
{
    public string Name { get; set; } = "";
    public double? Grams { get; set; }
    public string? Note { get; set; }
}

public class Recipe
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Source { get; set; } = RecipeSource.Manual;
    public bool InPool { get; set; } = true;
    public List<Ingredient> Ingredients { get; set; } = [];
    public string? Instructions { get; set; }
    public string? LinkUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int Servings { get; set; } = 1;
    public double? Kcal { get; set; }
    public double? Protein { get; set; }
    public double? Carbs { get; set; }
    public double? Fat { get; set; }
    public bool MacrosEstimated { get; set; }
    public string? ExternalId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DateTime CreatedAt { get; set; }
}

public class PlanEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateOnly Date { get; set; }
    public string MealType { get; set; } = Core.MealType.Lunch;
    public Guid? RecipeId { get; set; }
    public bool Locked { get; set; }

    public PlanEntry Clone() => (PlanEntry)MemberwiseClone();
}
