using System.Security.Claims;
using DailyVitals.Data.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace DailyVitals.Web.Services;

public sealed class LocalLoginSession
{
    private readonly IJSRuntime _jsRuntime;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly AuthTicketService _authTicketService;
    private readonly NavigationManager _navigation;
    private readonly PersonService _personService;

    public LocalLoginSession(
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authenticationStateProvider,
        AuthTicketService authTicketService,
        NavigationManager navigation,
        PersonService personService)
    {
        _jsRuntime = jsRuntime;
        _authenticationStateProvider = authenticationStateProvider;
        _authTicketService = authTicketService;
        _navigation = navigation;
        _personService = personService;
    }

    public bool IsSignedIn { get; private set; }
    public string? UserName { get; private set; }
    public string? PersonName { get; private set; }
    public string TimeZoneId { get; private set; } = "America/Chicago";
    public long? PersonId { get; private set; }
    public bool IsDemo { get; private set; }
    public bool RememberDevice { get; private set; }
    public bool CanWrite => IsSignedIn && !IsDemo;
    public string PersonDisplayName =>
        string.IsNullOrWhiteSpace(PersonName) ? UserName ?? "Person" : PersonName;
    public DateTime CurrentLocalTime => GetCurrentLocalTime(TimeZoneId);

    public void SignIn(string userName, long? personId, bool isDemo = false, bool rememberDevice = false)
    {
        IsSignedIn = true;
        UserName = userName;
        PersonId = personId;
        PersonName = ResolvePersonName(personId);
        TimeZoneId = ResolveTimeZoneId(personId);
        IsDemo = isDemo;
        RememberDevice = rememberDevice;
    }

    public async Task SignInAsync(string userName, long? personId, bool rememberDevice, bool isDemo = false)
    {
        if (!personId.HasValue)
            throw new InvalidOperationException("A person record is required to create an authenticated session.");

        var ticket = _authTicketService.Issue(userName, personId.Value, isDemo, rememberDevice);
        var succeeded = await _jsRuntime.InvokeAsync<bool>("dailyVitalsAuth.signIn", ticket);
        if (!succeeded)
            throw new InvalidOperationException("The server could not create the authentication session.");

        SignIn(userName, personId, isDemo, rememberDevice);
    }

    public async Task UpdateUserNameAsync(string userName)
    {
        if (!IsSignedIn || !PersonId.HasValue)
            return;

        var ticket = _authTicketService.Issue(userName, PersonId.Value, IsDemo, RememberDevice);
        var succeeded = await _jsRuntime.InvokeAsync<bool>("dailyVitalsAuth.signIn", ticket);
        if (!succeeded)
            throw new InvalidOperationException("The authentication session could not be refreshed.");

        UserName = userName;
    }

    public async Task RestoreAsync()
    {
        if (IsSignedIn)
            return;

        var principal = (await _authenticationStateProvider.GetAuthenticationStateAsync()).User;
        if (principal.Identity?.IsAuthenticated != true)
            return;

        var userName = principal.Identity.Name;
        var personIdValue = principal.FindFirstValue(AuthClaimTypes.PersonId);
        if (string.IsNullOrWhiteSpace(userName) ||
            !long.TryParse(personIdValue, out var personId) ||
            personId <= 0)
            return;

        var isDemo = string.Equals(
            principal.FindFirstValue(AuthClaimTypes.IsDemo),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);
        var rememberDevice = string.Equals(
            principal.FindFirstValue(AuthClaimTypes.RememberDevice),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);
        SignIn(userName, personId, isDemo, rememberDevice);
    }

    public void SignOut()
    {
        IsSignedIn = false;
        UserName = null;
        PersonId = null;
        PersonName = null;
        TimeZoneId = "America/Chicago";
        IsDemo = false;
        RememberDevice = false;
    }

    public async Task SignOutAsync()
    {
        await _jsRuntime.InvokeAsync<bool>("dailyVitalsAuth.signOut");
        SignOut();
        _navigation.NavigateTo("/signin", forceLoad: true, replace: true);
    }

    public static class AuthClaimTypes
    {
        public const string PersonId = "dailyvitals:person_id";
        public const string IsDemo = "dailyvitals:is_demo";
        public const string RememberDevice = "dailyvitals:remember_device";
    }

    private string? ResolvePersonName(long? personId)
    {
        if (!personId.HasValue)
            return null;

        try
        {
            var person = _personService.GetPersonById(personId.Value);
            return string.IsNullOrWhiteSpace(person?.FullName)
                ? null
                : person.FullName.Trim();
        }
        catch
        {
            // Authentication should remain usable if profile display data cannot
            // be loaded. PersonDisplayName will fall back to the account email.
            return null;
        }
    }

    public void UpdateTimeZone(string timeZoneId)
    {
        TimeZoneId = NormalizeTimeZoneId(timeZoneId);
    }

    private string ResolveTimeZoneId(long? personId)
    {
        if (!personId.HasValue)
            return "America/Chicago";

        try
        {
            return NormalizeTimeZoneId(_personService.GetPersonById(personId.Value)?.TimeZoneId);
        }
        catch
        {
            return "America/Chicago";
        }
    }

    private static string NormalizeTimeZoneId(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return "America/Chicago";

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return timeZoneId;
        }
        catch (TimeZoneNotFoundException)
        {
            return "America/Chicago";
        }
        catch (InvalidTimeZoneException)
        {
            return "America/Chicago";
        }
    }

    private static DateTime GetCurrentLocalTime(string timeZoneId)
    {
        var normalizedId = NormalizeTimeZoneId(timeZoneId);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(normalizedId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
    }
}
