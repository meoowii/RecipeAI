using RecipeAI.Interfaces;

namespace RecipeAI.Services;

public class LanguageDetectionService : ILanguageDetectionService
{
    private readonly HashSet<string> _supportedLanguages = new() { "en", "pl" };
    private readonly Dictionary<char, string> _characterLanguageMap = new()
    {
        {'¹', "pl"}, {'æ', "pl"}, {'ê', "pl"}, {'³', "pl"}, {'ñ', "pl"},
        {'ó', "pl"}, {'œ', "pl"}, {'Ÿ', "pl"}, {'¿', "pl"}
    };

    public string DetectLanguage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en";

        text = text.ToLowerInvariant();

        // Check for Polish characters
        foreach (var character in text)
        {
            if (_characterLanguageMap.TryGetValue(character, out var language))
                return language;
        }

        // Default to English if no Polish characters found
        return "en";
    }

    public bool IsSupported(string languageCode)
    {
        return _supportedLanguages.Contains(languageCode.ToLowerInvariant());
    }
}