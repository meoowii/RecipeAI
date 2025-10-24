namespace RecipeAI.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextFromImageAsync(byte[] imageData);
}