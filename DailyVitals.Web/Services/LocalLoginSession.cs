using System.Security.Claims;
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

    public LocalLoginSession(
        IJSRuntime jsRuntime,
        AuthenticationStateProvider authenticationStateProvider,
        AuthTicketService authTicketService,
        NavigationManager navigation)
    {
        _jsRuntime = jsRuntime;
        _authenticationStateProvider = authenticationStateProvider;
        _authTicketService = authTicketService;
        _navigation = navigation;
    }

    public bool IsSignedIn { get; private set; }
    public string? UserName { get; private set; }
    public long? PersonId { get; private set; }
    public bool IsDemo { get; private set; }
    public bool RememberDevice { get; private set; }
    public bool CanWrite => IsSignedIn && !IsDemo;

    public void SignIn(string userName, long? personId, bool isDemo = false, bool rememberDevice = false)
    {
        IsSignedIn = true;
        UserName = userName;
        PersonId = personId;
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
}
