using Microsoft.AspNetCore.Mvc;
using RecipeAI.Extensions;
using RecipeAI.Interfaces;
using RecipeAI.Options;
using RecipeAI.Responses;
using RecipeAI.Serilog;
using RecipeAI.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog config 
Log.Logger = SerilogHelper.CreateLogger(builder.Configuration);
builder.Host.UseSerilog();

// Add services to the container

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register HttpClient factory for Ollama calls
builder.Services.AddHttpClient();

// Configure options
builder.Services.Configure<TesseractOptions>(
    builder.Configuration.GetSection(TesseractOptions.SectionName));
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));

// Register services
builder.Services.AddScoped<IOcrService, TesseractOcrService>();
builder.Services.AddScoped<ILanguageDetectionService, LanguageDetectionService>();
builder.Services.AddScoped<INutritionAnalysisService, OllamaNutritionService>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IActivityProvider, ActivityProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RecipeAI v1");
    });
}

app.UseCustomExceptionHandler();

app.UseHttpsRedirection();

// API endpoints
app.MapPost("/api/analyze-recipe", async (
    [FromForm(Name = "image")] IFormFile image,
    IOcrService ocrService,
    ILanguageDetectionService languageService,
    INutritionAnalysisService nutritionService,
    ILogger<Program> logger) =>
{
    try
    {
        if (image is null || image.Length == 0)
            return Results.BadRequest("No image file provided");

        using var ms = new MemoryStream();
        await image.CopyToAsync(ms);
        var imageData = ms.ToArray();

        var extractedText = await ocrService.ExtractTextFromImageAsync(imageData);
        var language = languageService.DetectLanguage(extractedText);
        var nutritionInfo = await nutritionService.AnalyzeIngredientsAsync(extractedText, language);

        return Results.Ok(new
        {
            text = extractedText,
            language,
            nutrition = nutritionInfo
        });
}
    catch (Exception ex)
    {
    logger.LogError(ex, "Error processing recipe image");
    return Results.StatusCode(500);
}
})
.Accepts<IFormFile>("multipart/form-data")
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces<ApiErrorResponse>(StatusCodes.Status500InternalServerError)
.DisableAntiforgery()
.WithName("AnalyzeRecipe")
.WithOpenApi();

app.Run();

