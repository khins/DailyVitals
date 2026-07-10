using DailyVitals.Data.Services;
using DailyVitals.Domain.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DailyVitals.Web.Services;

public sealed class LocalLoginService
{
    private readonly IConfiguration _configuration;
    private readonly LoginUserService _loginUserService;
    private readonly ILogger<LocalLoginService> _logger;

    public LocalLoginService(
        IConfiguration configuration,
        LoginUserService loginUserService,
        ILogger<LocalLoginService> logger)
    {
        _configuration = configuration;
        _loginUserService = loginUserService;
        _logger = logger;
    }

    public bool ValidateCredentials(string? userName, string? password)
    {
        return ValidateLogin(userName, password) is not null;
    }

    public LoginUser? ValidateLogin(string? userName, string? password)
    {
        return Authenticate(userName, password).LoginUser;
    }

    public LocalLoginResult Authenticate(string? userName, string? password)
    {
        TrySeedConfiguredLogin();

        try
        {
            var result = _loginUserService.ValidateLogin(userName, password);
            if (result.IsSuccess)
            {
                _logger.LogInformation("Successful sign-in for {UserName}.", userName?.Trim());
                return LocalLoginResult.Success(result.LoginUser!);
            }

            if (result.IsLocked)
            {
                _logger.LogWarning(
                    "Locked account sign-in attempt for {UserName}. Locked until {LockedUntil}.",
                    userName?.Trim(),
                    result.LockedUntil);
                return LocalLoginResult.Locked(result.LockedUntil);
            }

            _logger.LogWarning("Failed sign-in attempt for {UserName}.", userName?.Trim());
            return LocalLoginResult.Invalid();
        }
        catch (Exception ex)
        {
            if (!ValidateConfiguredFallback(userName, password))
            {
                _logger.LogError(ex, "Login validation failed and configured fallback did not match.");
                return LocalLoginResult.Invalid();
            }

            _logger.LogWarning(ex, "Using configured fallback login because database login validation failed.");
            return LocalLoginResult.Success(new LoginUser
            {
                UserName = userName?.Trim() ?? string.Empty,
                IsActive = true
            });
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

public sealed class LocalLoginResult
{
    private LocalLoginResult(LoginUser? loginUser, bool isLocked, DateTime? lockedUntil)
    {
        LoginUser = loginUser;
        IsLocked = isLocked;
        LockedUntil = lockedUntil;
    }

    public LoginUser? LoginUser { get; }
    public bool IsSuccess => LoginUser is not null;
    public bool IsLocked { get; }
    public DateTime? LockedUntil { get; }

    public static LocalLoginResult Success(LoginUser loginUser) =>
        new(loginUser, isLocked: false, lockedUntil: null);

    public static LocalLoginResult Invalid() =>
        new(null, isLocked: false, lockedUntil: null);

    public static LocalLoginResult Locked(DateTime? lockedUntil) =>
        new(null, isLocked: true, lockedUntil);
}
