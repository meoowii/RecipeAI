using RecipeAI.Models;

namespace RecipeAI.Interfaces;

public interface INutritionAnalysisService
{
    Task<NutritionInfo> AnalyzeIngredientsAsync(string ingredients, string language);
}