using Microsoft.Extensions.Configuration;

namespace DailyVitals.Web.Services;

public sealed class LocalLoginService
{
    private readonly IConfiguration _configuration;

    public LocalLoginService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool ValidateCredentials(string? userName, string? password)
    {
        var configuredUserName = _configuration["DailyVitalsLogin:UserName"];
        var configuredPassword = _configuration["DailyVitalsLogin:Password"];

        if (string.IsNullOrWhiteSpace(configuredUserName) ||
            string.IsNullOrWhiteSpace(configuredPassword))
        {
            return false;
        }

        return string.Equals(userName?.Trim(), configuredUserName.Trim(), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(password, configuredPassword, StringComparison.Ordinal);
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_configuration["DailyVitalsLogin:UserName"]) &&
        !string.IsNullOrWhiteSpace(_configuration["DailyVitalsLogin:Password"]);
}
