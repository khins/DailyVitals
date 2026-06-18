using DailyVitals.Data.Services;
using Microsoft.Extensions.Configuration;

namespace DailyVitals.Web.Services;

public sealed class LocalLoginService
{
    private readonly IConfiguration _configuration;
    private readonly LoginUserService _loginUserService;

    public LocalLoginService(IConfiguration configuration, LoginUserService loginUserService)
    {
        _configuration = configuration;
        _loginUserService = loginUserService;
    }

    public bool ValidateCredentials(string? userName, string? password)
    {
        TrySeedConfiguredLogin();

        try
        {
            return _loginUserService.ValidateCredentials(userName, password) is not null;
        }
        catch
        {
            return ValidateConfiguredFallback(userName, password);
        }
    }

    public bool IsConfigured
    {
        get
        {
            if (HasConfiguredFallback())
                return true;

            try
            {
                return _loginUserService.HasAnyLoginUsers();
            }
            catch
            {
                return false;
            }
        }
    }

    private void TrySeedConfiguredLogin()
    {
        var configuredUserName = _configuration["DailyVitalsLogin:UserName"];
        var configuredPassword = _configuration["DailyVitalsLogin:Password"];

        if (string.IsNullOrWhiteSpace(configuredUserName) ||
            string.IsNullOrWhiteSpace(configuredPassword))
        {
            return;
        }

        try
        {
            _loginUserService.EnsureLoginUserExists(configuredUserName, configuredPassword);
        }
        catch
        {
            // The config login keeps local development usable until the database connection is available.
        }
    }

    private bool ValidateConfiguredFallback(string? userName, string? password)
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

    private bool HasConfiguredFallback() =>
        !string.IsNullOrWhiteSpace(_configuration["DailyVitalsLogin:UserName"]) &&
        !string.IsNullOrWhiteSpace(_configuration["DailyVitalsLogin:Password"]);
}
