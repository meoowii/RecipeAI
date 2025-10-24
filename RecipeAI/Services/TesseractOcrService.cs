using Tesseract;
using RecipeAI.Interfaces;

namespace RecipeAI.Services;

public class TesseractOcrService : IOcrService
{
    private readonly ILogger<TesseractOcrService> _logger;
    private readonly string _tessdataPath;

    public TesseractOcrService(ILogger<TesseractOcrService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _tessdataPath = configuration["Tesseract:TessdataPath"] ?? "tessdata";
    }

    public async Task<string> ExtractTextFromImageAsync(byte[] imageData)
    {
        try
        {
            using var engine = new TesseractEngine(_tessdataPath, "eng+pol", EngineMode.Default);
            using var ms = new MemoryStream(imageData);
            using var img = Pix.LoadFromMemory(imageData);
            using var page = engine.Process(img);

            return await Task.FromResult(page.GetText());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OCR processing");
            throw;
        }
    }
}