namespace RecipeAI.Options;

public record TesseractOptions
{
    public const string SectionName = "Tesseract";
    public required string BaseDataPath { get; init; }
}