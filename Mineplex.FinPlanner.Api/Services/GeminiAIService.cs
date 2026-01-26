using System.Text;
using System.Text.Json;
using Mineplex.FinPlanner.Api.Models;

namespace Mineplex.FinPlanner.Api.Services
{
    public interface IAIService
    {
        Task<AIRecommendation> AnalyzeHoldingAsync(Holding holding, string portfolioContext);
        Task<string?> LookupSymbolAsync(string assetName, string? market);
        Task<string> TestConnectionAsync();
        Task<string> ChatWithAIAsync(string userMessage, string context);
    }

    public class GeminiAIService : IAIService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GeminiAIService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<AIRecommendation> AnalyzeHoldingAsync(Holding holding, string portfolioContext)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return new AIRecommendation { Summary = "AI API Key not configured" };

            var prompt = $@"
System: You are a professional investment analyst providing recommendations 
for an Australian investor. Consider CGT implications, franking credits, 
and local market conditions.

User: Analyze this holding:
- Asset: {holding.Asset.Symbol} ({holding.Asset.Name})
- Category: {holding.Category?.Name ?? "Uncategorized"}
- Current Value: ${holding.CurrentValue}
- Units: {holding.Units}
- Cost Base: ${holding.AvgCost}

Portfolio context:
{portfolioContext}

Provide a JSON response with the following structure:
{{
  ""action"": ""BUY"" | ""SELL"" | ""HOLD"",
  ""summary"": ""...summary text..."",
  ""analysis"": ""...detailed analysis..."",
  ""confidence"": 0.0 to 1.0
}}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-preview:generateContent?key={apiKey}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();

                // Parse Gemini response structure to extract text, then parse our JSON inside it
                using var doc = JsonDocument.Parse(responseString);
                var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                // Clean markdown code blocks if present
                if (!string.IsNullOrEmpty(text))
                {
                    text = text.Replace("```json", "").Replace("```", "").Trim();

                    try
                    {
                        using var aiDoc = JsonDocument.Parse(text);
                        var root = aiDoc.RootElement;

                        return new AIRecommendation
                        {
                            Id = Guid.NewGuid(),
                            AssetId = holding.AssetId,
                            Action = root.GetProperty("action").GetString() ?? "HOLD",
                            Summary = root.GetProperty("summary").GetString() ?? "No summary provided",
                            Analysis = root.GetProperty("analysis").GetString() ?? "No analysis provided",
                            Confidence = root.GetProperty("confidence").GetDecimal(),
                            CreatedAt = DateTime.UtcNow
                        };
                    }
                    catch
                    {
                        return new AIRecommendation
                        {
                            AssetId = holding.AssetId,
                            Action = "HOLD",
                            Summary = "Failed to parse AI response",
                            Analysis = text,
                            Confidence = 0
                        };
                    }
                }
            }

            return new AIRecommendation { Summary = "AI Analysis failed" };
        }

        public async Task<string?> LookupSymbolAsync(string assetName, string? market)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return null;

            var prompt = $@"
System: You are a market data expert. Your task is to find the Yahoo Finance ticker symbol for a given asset name.
If it's an Australian asset (market is ASX or AU), ensure the symbol ends with '.AX'.

User: Find the Yahoo Finance symbol for:
Asset Name: {assetName}
Expected Market: {market ?? "Any"}

Return ONLY the symbol string (e.g., 'CBA.AX' or 'AAPL'). If you cannot find a clear match, return 'UNKNOWN'.
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Trim().Replace("`", "").ToUpper();
                        if (text == "UNKNOWN" || text.Length > 10) return null;
                        return text;
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

        public async Task<string> TestConnectionAsync()
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return "AI API Key not configured";

            var prompt = "Hello, world! Respond with 'AI service is online and configured correctly.'";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    return text?.Trim() ?? "Empty response from AI service";
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return $"AI service returned error: {response.StatusCode}. Details: {error}";
                }
            }
            catch (Exception ex)
            {
                return $"Exception occurred: {ex.Message}";
            }
        }
        public async Task<string> ChatWithAIAsync(string userMessage, string context)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            if (string.IsNullOrEmpty(apiKey)) return "AI API Key not configured";

            var prompt = $@"
System: You are a professional financial advisor assistant. Use the following context about the user's portfolio and retirement plans to answer their questions.
Be helpful, professional, and explain the 'why' behind your advice. Always include a disclaimer that this is not personal financial advice.

Context:
{context}

User's Question: {userMessage}
";

            var requestBody = new
            {
                contents = new[]
                {
                    new { parts = new[] { new { text = prompt } } }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync($"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash-exp:generateContent?key={apiKey}", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseString);
                    var text = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                    return text ?? "No response from AI service";
                }
                else
                {
                    return "Error connecting to AI service";
                }
            }
            catch
            {
                return "Failed to get AI response";
            }
        }
    }
}
