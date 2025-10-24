using RecipeAI.Interfaces;
using RecipeAI.Models;
using RecipeAI.Options;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RecipeAI.Services;

public class OllamaNutritionService : INutritionAnalysisService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OllamaNutritionService> _logger;
    private readonly OllamaOptions _options;

    public OllamaNutritionService(
        ILogger<OllamaNutritionService> logger,
        IOptions<OllamaOptions> options,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<NutritionInfo> AnalyzeIngredientsAsync(string ingredients, string language)
    {
        try
        {
            var prompt = BuildPrompt(ingredients, language);

            // 1. Wywołanie modelu
            var text = await SendToOllamaAsync(prompt);
            var info = ParseToNutrition(text);

            // 2. Retry, jeśli model zwróci same zera
            if (HasZero(info))
            {
                var retryPrompt = prompt + "\n\nIMPORTANT: If any field would be zero, estimate instead and return a non-zero value.";
                var retryText = await SendToOllamaAsync(retryPrompt);
                var retryInfo = ParseToNutrition(retryText);

                if (!HasZero(retryInfo))
                    return retryInfo;
            }

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during nutrition analysis");
            return DefaultNutrition();
        }
    }

    // Helpery

    private async Task<string> SendToOllamaAsync(string prompt)
    {
        var client = _httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.Endpoint.TrimEnd('/')}/api/generate");

        var body = new
        {
            model = _options.ModelName,
            prompt,
            stream = false,
            options = new
            {
                temperature = 0.0,
                top_p = 1.0,
                repeat_penalty = 1.1,
                seed = 42,
                num_predict = 800,
                stop = new[] { "```" }
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();

        _logger.LogInformation("Ollama status: {Status}. Payload length: {Len}", response.StatusCode, content?.Length ?? 0);

        if (!response.IsSuccessStatusCode || string.IsNullOrWhiteSpace(content))
            return string.Empty;

        try
        {
            var ollama = JsonSerializer.Deserialize<OllamaGenerateResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            return ollama?.Response ?? content;
        }
        catch
        {
            return content;
        }
    }

    private NutritionInfo ParseToNutrition(string modelOutput)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
            return DefaultNutrition();

        var json = ExtractJsonObject(modelOutput);
        if (string.IsNullOrWhiteSpace(json))
            return DefaultNutrition();

        try
        {
            var info = JsonSerializer.Deserialize<NutritionInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (info is not null) return info;
        }
        catch
        {

        }

        return ParseNutritionTolerant(json) ?? DefaultNutrition();
    }

    private static bool HasZero(NutritionInfo n) =>
        n.Calories <= 0 || n.Proteins <= 0 || n.Carbohydrates <= 0 || n.Fats <= 0;

    private static string BuildPrompt(string ingredients, string language)
    {
        bool isPolish = language?.Equals("pl", StringComparison.OrdinalIgnoreCase) == true;
        return isPolish ? BuildPolishPrompt(ingredients) : BuildEnglishPrompt(ingredients);
    }

    private static string BuildPolishPrompt(string ingredients)
    {
        return "Jesteś dietetykiem. Policz makroskładniki dla całego przepisu na podstawie listy składników.\n\n" +
               "Zasady:\n" +
               "- Jeśli brakuje dokładnych gramów/ilości, użyj rozsądnych, kuchennych estymat (np. 1 łyżka ~ 15 ml, 1 ząbek czosnku ~ 3 g, 1 szklanka mąki ~ 120 g itd.).\n" +
               "- Konwertuj jednostki i sumuj po wszystkich składnikach.\n" +
               "- Zwracaj zawsze liczby > 0 (bez zer; jeśli brak danych – oszacuj).\n" +
               "- Zwróć tylko czysty JSON dokładnie w schemacie:\n" +
               "{\"Calories\": number, \"Proteins\": number, \"Carbohydrates\": number, \"Fats\": number}\n" +
               "- Użyj kropki jako separatora dziesiętnego. Żadnego dodatkowego tekstu.\n\n" +
               "Składniki:\n" +
               $"```{ingredients}```";
    }

    private static string BuildEnglishPrompt(string ingredients)
    {
        return "You are a nutritionist. Compute macros for the whole recipe from the ingredient list.\n\n" +
               "Rules:\n" +
               "- If exact quantities are missing, use reasonable, common-kitchen estimates (e.g., 1 tbsp ~ 15 ml, 1 garlic clove ~ 3 g, 1 cup flour ~ 120 g, etc.).\n" +
               "- Convert units and sum across all ingredients.\n" +
               "- Always return numbers > 0 (no zeros; estimate when uncertain).\n" +
               "- Return only raw JSON in exactly this schema:\n" +
               "{\"Calories\": number, \"Proteins\": number, \"Carbohydrates\": number, \"Fats\": number}\n" +
               "- Use dot as decimal separator. No prose.\n\n" +
               "Ingredients:\n" +
               $"```{ingredients}```";
    }

    private static NutritionInfo DefaultNutrition() => new()
    {
        Calories = 0,
        Proteins = 0,
        Carbohydrates = 0,
        Fats = 0
    };

    private static string? ExtractJsonObject(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
        }

        if (trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[^1] == '}')
            return trimmed;

        int depth = 0, start = -1;
        for (int i = 0; i < trimmed.Length; i++)
        {
            if (trimmed[i] == '{')
            {
                if (depth == 0) start = i;
                depth++;
            }
            else if (trimmed[i] == '}')
            {
                depth--;
                if (depth == 0 && start >= 0)
                {
                    var candidate = trimmed[start..(i + 1)];
                    if (IsValidJson(candidate))
                        return candidate;
                }
            }
        }
        return null;
    }

    private static bool IsValidJson(string candidate)
    {
        try
        {
            using var _ = JsonDocument.Parse(candidate);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static NutritionInfo? ParseNutritionTolerant(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            static double ToNumber(JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetDouble(out var d)) return d;
                if (el.ValueKind == JsonValueKind.String)
                {
                    var s = Regex.Replace(el.GetString()?.Replace(',', '.') ?? "0", @"[^\d\.\-]", "");
                    return double.TryParse(s, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var ds) ? ds : 0;
                }
                return 0;
            }

            double GetValue(string name)
            {
                if (TryFindProperty(root, name, out var el))
                    return ToNumber(el);
                return 0;
            }

            return new NutritionInfo
            {
                Calories = GetValue("Calories"),
                Proteins = GetValue("Proteins"),
                Carbohydrates = GetValue("Carbohydrates"),
                Fats = GetValue("Fats"),
                Ingredients = Array.Empty<Ingredient>()
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool TryFindProperty(JsonElement root, string name, out JsonElement found)
    {
        foreach (var p in root.EnumerateObject())
        {
            if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                found = p.Value;
                return true;
            }

            if (p.Value.ValueKind == JsonValueKind.Object && TryFindProperty(p.Value, name, out found))
                return true;
        }

        found = default;
        return false;
    }

    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
