using DailyVitals.Domain.Models;
using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DailyVitals.Data.Services
{
    public class FoodPhosphorusEstimateService
    {
        private static readonly HttpClient HttpClient = new();

        public async Task<FoodPhosphorusEstimate> EstimateAsync(string foodDescription)
        {
            if (string.IsNullOrWhiteSpace(foodDescription))
                throw new InvalidOperationException("Enter a food item before estimating phosphorus.");

            var apiKey = GetSetting("GeminiApiKey", "GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Gemini API key is missing. Set GEMINI_API_KEY or add GeminiApiKey to App.config.");

            var model = GetSetting("GeminiModel", "GEMINI_MODEL");
            if (string.IsNullOrWhiteSpace(model))
                model = "gemini-2.0-flash";

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Uri.EscapeDataString(model)}:generateContent?key={Uri.EscapeDataString(apiKey)}";
            var prompt = BuildPrompt(foodDescription);
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    responseMimeType = "application/json"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var response = await HttpClient.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Gemini estimate failed: {(int)response.StatusCode} {response.ReasonPhrase}. {responseText}");

            var estimateJson = ExtractResponseText(responseText);
            var estimate = JsonSerializer.Deserialize<FoodPhosphorusEstimate>(
                estimateJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (estimate == null)
                throw new InvalidOperationException("Gemini did not return a phosphorus estimate.");

            if (estimate.EstimatedPhosphorusMg < 0)
                throw new InvalidOperationException("Gemini returned an invalid phosphorus amount.");

            if (string.IsNullOrWhiteSpace(estimate.FoodName))
                estimate.FoodName = foodDescription.Trim();

            return estimate;
        }

        private static string BuildPrompt(string foodDescription)
        {
            return
                "Estimate the phosphorus content in milligrams for this food item and serving:" + Environment.NewLine +
                foodDescription.Trim() + Environment.NewLine + Environment.NewLine +
                "This is for a dialysis food tracking app. Return only valid JSON with these exact properties:" + Environment.NewLine +
                "{" + Environment.NewLine +
                "  \"foodName\": \"normalized food name\"," + Environment.NewLine +
                "  \"servingDescription\": \"serving size or portion assumed\"," + Environment.NewLine +
                "  \"estimatedPhosphorusMg\": 0," + Environment.NewLine +
                "  \"confidence\": \"low, medium, or high\"," + Environment.NewLine +
                "  \"sourceNotes\": \"short note about assumptions, brand variation, additives, or uncertainty\"" + Environment.NewLine +
                "}" + Environment.NewLine + Environment.NewLine +
                "Use a cautious estimate. If the serving size is unclear, make a reasonable serving-size assumption and say so in sourceNotes.";
        }

        private static string ExtractResponseText(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
                throw new InvalidOperationException("Gemini returned no estimate candidates.");

            var firstCandidate = candidates[0];
            var parts = firstCandidate
                .GetProperty("content")
                .GetProperty("parts");

            if (parts.GetArrayLength() == 0 ||
                !parts[0].TryGetProperty("text", out var textElement))
                throw new InvalidOperationException("Gemini response did not include estimate text.");

            return textElement.GetString() ?? throw new InvalidOperationException("Gemini estimate text was empty.");
        }

        private static string? GetSetting(string appSettingKey, string environmentVariableKey)
        {
            var value = ConfigurationManager.AppSettings[appSettingKey];
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            return Environment.GetEnvironmentVariable(environmentVariableKey);
        }
    }
}
