namespace RecipeAI.Interfaces;

public interface ILanguageDetectionService
{
    string DetectLanguage(string text);
    bool IsSupported(string languageCode);
}