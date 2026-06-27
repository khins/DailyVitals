using DailyVitals.Data.Configuration;
using DailyVitals.Domain.Models;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DailyVitals.Data.Services
{
    public class NutritionCoachService
    {
        private static readonly HttpClient HttpClient = new();

        public NutritionCoachSnapshot BuildSnapshot(
            IEnumerable<FoodPhosphorusIntake> history,
            NutritionGoal? goal,
            DateTime periodEnd,
            int days = 7)
        {
            if (days < 1)
                throw new ArgumentOutOfRangeException(nameof(days));

            var end = periodEnd.Date;
            var start = end.AddDays(-(days - 1));
            var entries = history
                .Where(x => x.ConsumedAt.Date >= start && x.ConsumedAt.Date <= end)
                .ToList();
            var daily = entries
                .GroupBy(x => x.ConsumedAt.Date)
                .Select(group => new
                {
                    Sodium = group.Sum(x => x.SodiumMg ?? 0),
                    Phosphorus = group.Sum(x => x.PhosphorusMg),
                    Protein = group.Sum(x => x.ProteinG ?? 0),
                    Potassium = group.Sum(x => x.PotassiumMg ?? 0)
                })
                .ToList();

            var sodiumGoal = goal?.SodiumLimitMg ?? 2000;
            var phosphorusGoal = goal?.PhosphorusLimitMg ?? 1000;
            var proteinGoal = goal?.ProteinTargetG ?? 60;
            var potassiumGoal = goal?.PotassiumLimitMg ?? 2000;

            return new NutritionCoachSnapshot
            {
                PeriodStart = start,
                PeriodEnd = end,
                DaysInPeriod = days,
                DaysLogged = daily.Count,
                FoodEntries = entries.Count,
                BindersLogged = entries.Sum(x => x.Binders),
                Sodium = BuildLimitMetric(daily.Select(x => (decimal)x.Sodium), sodiumGoal, "mg"),
                Phosphorus = BuildLimitMetric(daily.Select(x => (decimal)x.Phosphorus), phosphorusGoal, "mg"),
                Protein = BuildTargetMetric(daily.Select(x => x.Protein), proteinGoal, "g"),
                Potassium = BuildLimitMetric(daily.Select(x => (decimal)x.Potassium), potassiumGoal, "mg"),
                TopSodiumSources = BuildSources(entries, x => x.SodiumMg ?? 0, "mg"),
                TopPhosphorusSources = BuildSources(entries, x => x.PhosphorusMg, "mg"),
                TopProteinSources = BuildSources(entries, x => x.ProteinG ?? 0, "g"),
                TopPotassiumSources = BuildSources(entries, x => x.PotassiumMg ?? 0, "mg")
            };
        }

        public async Task<NutritionCoachStoredReview> GenerateReviewAsync(long personId, NutritionCoachSnapshot snapshot)
        {
            if (personId <= 0)
                throw new ArgumentOutOfRangeException(nameof(personId));

            if (snapshot.DaysLogged == 0)
                throw new InvalidOperationException("Log at least one day of nutrition before generating a coach review.");

            var apiKey = GetSetting("OpenAiApiKey", "OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key is missing. Set OPENAI_API_KEY or add OpenAiApiKey to App.config.");

            var model = GetSetting("OpenAiModel", "OPENAI_MODEL");
            if (string.IsNullOrWhiteSpace(model))
                model = "gpt-5.4-mini";

            var requestBody = new
            {
                model,
                instructions =
                    "You are a supportive nutrition pattern coach in a kidney-care tracking app. " +
                    "Use only the supplied calculated facts. Clearly distinguish logged days from the full period. " +
                    "Do not diagnose, prescribe, recommend changing binders or medication, or alter clinician-set goals. " +
                    "Give brief, practical food-tracking observations and cautious substitutions. Return only the requested structured data.",
                input = BuildPrompt(snapshot),
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "nutrition_coach_review",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            required = new[] { "headline", "summary", "wins", "focusAreas", "suggestedActions", "careTeamNote" },
                            properties = new
                            {
                                headline = new { type = "string" },
                                summary = new { type = "string" },
                                wins = new { type = "array", minItems = 1, maxItems = 3, items = new { type = "string" } },
                                focusAreas = new { type = "array", minItems = 1, maxItems = 3, items = new { type = "string" } },
                                suggestedActions = new { type = "array", minItems = 1, maxItems = 3, items = new { type = "string" } },
                                careTeamNote = new { type = "string" }
                            }
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                SaveResponse(
                    personId,
                    snapshot,
                    model,
                    responseText,
                    null,
                    (int)response.StatusCode,
                    false,
                    $"{(int)response.StatusCode} {response.ReasonPhrase}");

                if (response.StatusCode == HttpStatusCode.TooManyRequests &&
                    responseText.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "AI Coach is configured, but the OpenAI project does not currently have available billing quota.");
                }

                throw new InvalidOperationException($"AI Coach request failed: {(int)response.StatusCode} {response.ReasonPhrase}.");
            }

            string? reviewJson = null;
            NutritionCoachReview? review;

            try
            {
                reviewJson = ExtractResponseText(responseText);
                review = JsonSerializer.Deserialize<NutritionCoachReview>(
                    reviewJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                SaveResponse(
                    personId,
                    snapshot,
                    model,
                    responseText,
                    reviewJson,
                    (int)response.StatusCode,
                    false,
                    ex.Message);
                throw;
            }

            if (review == null || string.IsNullOrWhiteSpace(review.Headline) || string.IsNullOrWhiteSpace(review.Summary))
            {
                const string message = "OpenAI did not return a complete coach review.";
                SaveResponse(
                    personId,
                    snapshot,
                    model,
                    responseText,
                    reviewJson,
                    (int)response.StatusCode,
                    false,
                    message);
                throw new InvalidOperationException(message);
            }

            var storedReview = SaveResponse(
                personId,
                snapshot,
                model,
                responseText,
                reviewJson,
                (int)response.StatusCode,
                true,
                null);
            storedReview.Review = review;
            return storedReview;
        }

        public NutritionCoachStoredReview? GetLatestSuccessfulReview(
            long personId,
            DateTime periodStart,
            DateTime periodEnd)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureReviewTable(conn);

            const string sql = @"
                SELECT
                    nutrition_coach_review_id,
                    person_id,
                    period_start,
                    period_end,
                    model,
                    created_at,
                    review_json
                FROM public.nutrition_coach_review
                WHERE person_id = @person_id
                  AND period_start = @period_start
                  AND period_end = @period_end
                  AND is_success = TRUE
                  AND review_json IS NOT NULL
                ORDER BY created_at DESC, nutrition_coach_review_id DESC
                LIMIT 1;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("period_start", periodStart.Date);
            cmd.Parameters.AddWithValue("period_end", periodEnd.Date);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var review = JsonSerializer.Deserialize<NutritionCoachReview>(
                reader.GetString(6),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (review == null)
                return null;

            return new NutritionCoachStoredReview
            {
                NutritionCoachReviewId = reader.GetInt64(0),
                PersonId = reader.GetInt64(1),
                PeriodStart = reader.GetDateTime(2),
                PeriodEnd = reader.GetDateTime(3),
                Model = reader.GetString(4),
                CreatedAt = reader.GetDateTime(5),
                Review = review
            };
        }

        private static NutritionCoachMetric BuildLimitMetric(IEnumerable<decimal> values, decimal goal, string unit)
        {
            var loggedValues = values.ToList();
            return new NutritionCoachMetric
            {
                Goal = goal,
                AverageOnLoggedDays = loggedValues.Count == 0 ? 0 : loggedValues.Average(),
                DaysMeetingGoal = loggedValues.Count(x => x <= goal),
                GoalType = "limit",
                Unit = unit
            };
        }

        private static NutritionCoachMetric BuildTargetMetric(IEnumerable<decimal> values, decimal goal, string unit)
        {
            var loggedValues = values.ToList();
            return new NutritionCoachMetric
            {
                Goal = goal,
                AverageOnLoggedDays = loggedValues.Count == 0 ? 0 : loggedValues.Average(),
                DaysMeetingGoal = loggedValues.Count(x => x >= goal),
                GoalType = "target",
                Unit = unit
            };
        }

        private static List<NutritionCoachSource> BuildSources(
            IEnumerable<FoodPhosphorusIntake> entries,
            Func<FoodPhosphorusIntake, decimal> amountSelector,
            string unit)
        {
            return entries
                .Where(x => !string.IsNullOrWhiteSpace(x.FoodName) && amountSelector(x) > 0)
                .GroupBy(x => x.FoodName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group => new NutritionCoachSource
                {
                    FoodName = group.First().FoodName.Trim(),
                    Amount = group.Sum(amountSelector),
                    Unit = unit
                })
                .OrderByDescending(x => x.Amount)
                .ThenBy(x => x.FoodName)
                .Take(3)
                .ToList();
        }

        private static string BuildPrompt(NutritionCoachSnapshot snapshot)
        {
            return
                "Write a concise weekly review from this verified nutrition snapshot. " +
                "All compliance statements must use DaysLogged as the denominator, not DaysInPeriod. " +
                "Mention incomplete tracking when DaysLogged is less than DaysInPeriod. " +
                "Phosphorus values are logged dietary phosphorus; BindersLogged is context only and must not be converted into absorbed phosphorus. " +
                "Do not claim that a food caused a medical outcome. Keep each list item to one sentence.\n\n" +
                JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
        }

        private static NutritionCoachStoredReview SaveResponse(
            long personId,
            NutritionCoachSnapshot snapshot,
            string model,
            string apiResponse,
            string? reviewJson,
            int httpStatus,
            bool isSuccess,
            string? errorMessage)
        {
            using var conn = DbConnectionFactory.Create();
            conn.Open();
            EnsureReviewTable(conn);

            const string sql = @"
                INSERT INTO public.nutrition_coach_review (
                    person_id,
                    period_start,
                    period_end,
                    days_logged,
                    model,
                    snapshot_json,
                    api_response_text,
                    review_json,
                    http_status,
                    is_success,
                    error_message
                )
                VALUES (
                    @person_id,
                    @period_start,
                    @period_end,
                    @days_logged,
                    @model,
                    @snapshot_json,
                    @api_response_text,
                    @review_json,
                    @http_status,
                    @is_success,
                    @error_message
                )
                RETURNING nutrition_coach_review_id, created_at;";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("person_id", personId);
            cmd.Parameters.AddWithValue("period_start", snapshot.PeriodStart.Date);
            cmd.Parameters.AddWithValue("period_end", snapshot.PeriodEnd.Date);
            cmd.Parameters.AddWithValue("days_logged", snapshot.DaysLogged);
            cmd.Parameters.AddWithValue("model", model);
            cmd.Parameters.AddWithValue("snapshot_json", JsonSerializer.Serialize(snapshot));
            cmd.Parameters.AddWithValue("api_response_text", apiResponse);
            cmd.Parameters.AddWithValue("review_json", (object?)reviewJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("http_status", httpStatus);
            cmd.Parameters.AddWithValue("is_success", isSuccess);
            cmd.Parameters.AddWithValue("error_message", (object?)errorMessage ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                throw new InvalidOperationException("The AI Coach response could not be saved.");

            return new NutritionCoachStoredReview
            {
                NutritionCoachReviewId = reader.GetInt64(0),
                PersonId = personId,
                PeriodStart = snapshot.PeriodStart,
                PeriodEnd = snapshot.PeriodEnd,
                Model = model,
                CreatedAt = reader.GetDateTime(1)
            };
        }

        private static void EnsureReviewTable(NpgsqlConnection conn)
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS public.nutrition_coach_review (
                    nutrition_coach_review_id bigserial NOT NULL,
                    person_id int8 NOT NULL,
                    period_start date NOT NULL,
                    period_end date NOT NULL,
                    days_logged int4 NOT NULL,
                    model text NOT NULL,
                    snapshot_json text NOT NULL,
                    api_response_text text NOT NULL,
                    review_json text NULL,
                    http_status int4 NOT NULL,
                    is_success boolean NOT NULL,
                    error_message text NULL,
                    created_at timestamp NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    CONSTRAINT nutrition_coach_review_pkey PRIMARY KEY (nutrition_coach_review_id)
                );

                CREATE INDEX IF NOT EXISTS ix_nutrition_coach_review_person_period
                    ON public.nutrition_coach_review (person_id, period_end DESC, created_at DESC);";

            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.ExecuteNonQuery();
        }

        private static string ExtractResponseText(string responseText)
        {
            var response = JsonSerializer.Deserialize<OpenAiResponse>(
                responseText,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (!string.IsNullOrWhiteSpace(response?.OutputText))
                return response.OutputText;

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
                            throw new InvalidOperationException($"OpenAI refused the coach request: {content.Refusal}");
                    }
                }
            }

            throw new InvalidOperationException("OpenAI response did not include coach review text.");
        }

        private static string? GetSetting(string appSettingKey, string environmentVariableKey)
        {
            var value = Environment.GetEnvironmentVariable(environmentVariableKey);
            return string.IsNullOrWhiteSpace(value)
                ? ConfigurationManager.AppSettings[appSettingKey]
                : value;
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
