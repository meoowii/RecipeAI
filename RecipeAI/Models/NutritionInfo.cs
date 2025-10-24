namespace RecipeAI.Models;

public record NutritionInfo
{
    public double Calories { get; set; }
    public double Proteins { get; set; }
    public double Carbohydrates { get; set; }
    public double Fats { get; set; }
    public IReadOnlyList<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
}

public record Ingredient
{
    public string Name { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Unit { get; set; } = string.Empty;
    public NutritionInfo? NutritionPer100g { get; set; }
}