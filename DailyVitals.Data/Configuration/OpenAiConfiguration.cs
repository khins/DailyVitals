using System.Configuration;

namespace DailyVitals.Data.Configuration
{
    public static class OpenAiConfiguration
    {
        private const string ApiKeyEnvironmentVariable = "OPENAI_API_KEY";
        private const string ModelEnvironmentVariable = "OPENAI_MODEL";
        private const string DefaultModel = "gpt-5.4-mini";

        private static string? _configuredApiKey;
        private static string? _configuredModel;

        public static void Configure(string? apiKey, string? model)
        {
            _configuredApiKey = Normalize(apiKey);
            _configuredModel = Normalize(model);
        }

        public static string? GetApiKey()
        {
            return Normalize(Environment.GetEnvironmentVariable(ApiKeyEnvironmentVariable))
                ?? _configuredApiKey
                ?? Normalize(ConfigurationManager.AppSettings["OpenAiApiKey"]);
        }

        public static string GetModel()
        {
            return Normalize(Environment.GetEnvironmentVariable(ModelEnvironmentVariable))
                ?? _configuredModel
                ?? Normalize(ConfigurationManager.AppSettings["OpenAiModel"])
                ?? DefaultModel;
        }

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
