using DailyVitals.Domain.Models;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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

            var apiKey = GetSetting("OpenAiApiKey", "OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key is missing. Set OPENAI_API_KEY or add OpenAiApiKey to App.config.");

            var model = GetSetting("OpenAiModel", "OPENAI_MODEL");
            if (string.IsNullOrWhiteSpace(model))
                model = "gpt-5-nano";

            var requestBody = new
            {
                model,
                instructions = "You estimate nutrition content for a dialysis food tracking app. Use cautious, practical nutrition estimates and return only the requested structured data.",
                input = BuildPrompt(foodDescription),
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "food_phosphorus_estimate",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[]
                            {
                                "foodName",
                                "servingDescription",
                                "estimatedPhosphorusMg",
                                "estimatedCalories",
                                "estimatedSodiumMg",
                                "estimatedProteinG",
                                "estimatedPotassiumMg",
                                "confidence",
                                "sourceNotes"
                            },
                            properties = new
                            {
                                foodName = new { type = "string" },
                                servingDescription = new { type = "string" },
                                estimatedPhosphorusMg = new { type = "integer" },
                                estimatedCalories = new { type = new[] { "integer", "null" } },
                                estimatedSodiumMg = new { type = new[] { "integer", "null" } },
                                estimatedProteinG = new { type = new[] { "number", "null" } },
                                estimatedPotassiumMg = new { type = new[] { "integer", "null" } },
                                confidence = new { type = "string", @enum = new[] { "low", "medium", "high" } },
                                sourceNotes = new { type = "string" }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (IsInsufficientQuotaResponse(response.StatusCode, responseText))
                {
                    throw new InvalidOperationException(
                        "AI Lookup is configured, but the OpenAI project does not currently have available billing quota. " +
                        "Check the OpenAI API billing, credits, or project budget settings, then try again.");
                }

                throw new InvalidOperationException($"OpenAI estimate failed: {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            var estimateJson = ExtractResponseText(responseText);
            var estimate = JsonSerializer.Deserialize<FoodPhosphorusEstimate>(
                estimateJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (estimate == null)
                throw new InvalidOperationException("OpenAI did not return a phosphorus estimate.");

            if (estimate.EstimatedPhosphorusMg < 0)
                throw new InvalidOperationException("OpenAI returned an invalid phosphorus amount.");

            if ((estimate.EstimatedCalories.HasValue && estimate.EstimatedCalories.Value < 0) ||
                (estimate.EstimatedSodiumMg.HasValue && estimate.EstimatedSodiumMg.Value < 0) ||
                (estimate.EstimatedProteinG.HasValue && estimate.EstimatedProteinG.Value < 0) ||
                (estimate.EstimatedPotassiumMg.HasValue && estimate.EstimatedPotassiumMg.Value < 0))
                throw new InvalidOperationException("OpenAI returned an invalid nutrition estimate.");

            if (string.IsNullOrWhiteSpace(estimate.FoodName))
                estimate.FoodName = foodDescription.Trim();

            return estimate;
        }

        private static string BuildPrompt(string foodDescription)
        {
            return
                "Estimate nutrition content for this food item and serving:" + Environment.NewLine +
                foodDescription.Trim() + Environment.NewLine + Environment.NewLine +
                "Return phosphorus in milligrams, calories, sodium in milligrams, protein in grams, and potassium in milligrams. " +
                "Use a cautious estimate. If the serving size is unclear, make a reasonable serving-size assumption and say so in sourceNotes. " +
                "Use null for calories, sodium, protein, or potassium only when the field cannot be reasonably estimated.";
        }

        private static string ExtractResponseText(string responseText)
        {
            var response = JsonSerializer.Deserialize<OpenAiResponse>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var outputText = response?.OutputText;
            if (!string.IsNullOrWhiteSpace(outputText))
                return outputText;

            if (response?.Output != null)
            {
                foreach (var item in response.Output)
                {
                    if (item.Content == null)
                        continue;

                    foreach (var content in item.Content)
                    {
                        if (!string.IsNullOrWhiteSpace(content.Text))
                            return content.Text;

                        if (!string.IsNullOrWhiteSpace(content.Refusal))
                            throw new InvalidOperationException($"OpenAI refused the estimate request: {content.Refusal}");
                    }
                }
            }

            throw new InvalidOperationException("OpenAI response did not include estimate text.");
        }

        private static string? GetSetting(string appSettingKey, string environmentVariableKey)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableKey);
            if (!string.IsNullOrWhiteSpace(value))
                return value;

            return ConfigurationManager.AppSettings[appSettingKey];
        }

        private static bool IsInsufficientQuotaResponse(HttpStatusCode statusCode, string responseText)
        {
            return statusCode == HttpStatusCode.TooManyRequests &&
                responseText.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class OpenAiResponse
        {
            [JsonPropertyName("output_text")]
            public string? OutputText { get; set; }

            [JsonPropertyName("output")]
            public OpenAiOutputItem[]? Output { get; set; }
        }

        private sealed class OpenAiOutputItem
        {
            [JsonPropertyName("content")]
            public OpenAiContentItem[]? Content { get; set; }
        }

        private sealed class OpenAiContentItem
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }

            [JsonPropertyName("refusal")]
            public string? Refusal { get; set; }
        }
    }
}
