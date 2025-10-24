namespace RecipeAI.Options;

public record OllamaOptions
{
    public const string SectionName = "Ollama";
    public required string Endpoint { get; init; }
    public required string ModelName { get; init; }
}