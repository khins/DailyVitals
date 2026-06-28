using System.Text.Json;
using DailyVitals.Data.Services;
using Microsoft.JSInterop;

namespace DailyVitals.Web.Services;

public sealed class LocalLoginSession
{
    private const string StorageKey = "dailyvitals.loginSession";
    private const int RememberDeviceDays = 30;
    private readonly IJSRuntime _jsRuntime;
    private readonly LoginUserService _loginUserService;

    public LocalLoginSession(IJSRuntime jsRuntime, LoginUserService loginUserService)
    {
        _jsRuntime = jsRuntime;
        _loginUserService = loginUserService;
    }

    public bool IsSignedIn { get; private set; }
    public string? UserName { get; private set; }
    public long? PersonId { get; private set; }
    public bool IsDemo { get; private set; }
    public bool CanWrite => IsSignedIn && !IsDemo;

    public void SignIn(string userName, long? personId, bool isDemo = false)
    {
        IsSignedIn = true;
        UserName = userName;
        PersonId = personId;
        IsDemo = isDemo;
    }

    public async Task SignInAsync(string userName, long? personId, bool rememberDevice, bool isDemo = false)
    {
        SignIn(userName, personId, isDemo);

        var expiresAt = rememberDevice
            ? DateTimeOffset.UtcNow.AddDays(RememberDeviceDays)
            : (DateTimeOffset?)null;
        var storedSession = JsonSerializer.Serialize(new StoredLoginSession(userName, personId, expiresAt));
        await ClearBrowserStorageAsync();

        var storageName = rememberDevice ? "localStorage.setItem" : "sessionStorage.setItem";
        await InvokeBrowserStorageAsync(() => _jsRuntime.InvokeVoidAsync(storageName, StorageKey, storedSession).AsTask());
    }

    public async Task UpdateUserNameAsync(string userName)
    {
        if (!IsSignedIn)
            return;

        var localSession = await ReadBrowserStorageAsync("localStorage.getItem");
        var useLocalStorage = localSession is not null;
        var storedSession = JsonSerializer.Serialize(new StoredLoginSession(userName, PersonId, localSession?.ExpiresAt));

        UserName = userName;
        await ClearBrowserStorageAsync();

        var storageName = useLocalStorage ? "localStorage.setItem" : "sessionStorage.setItem";
        await InvokeBrowserStorageAsync(() => _jsRuntime.InvokeVoidAsync(storageName, StorageKey, storedSession).AsTask());
    }

    public async Task RestoreAsync()
    {
        if (IsSignedIn)
            return;

        var storedSession = await ReadBrowserStorageAsync("localStorage.getItem")
            ?? await ReadBrowserStorageAsync("sessionStorage.getItem");

        if (storedSession is null || string.IsNullOrWhiteSpace(storedSession.UserName))
            return;

        if (storedSession.ExpiresAt.HasValue && storedSession.ExpiresAt.Value <= DateTimeOffset.UtcNow)
        {
            await ClearBrowserStorageAsync();
            return;
        }

        var isDemo = false;
        try
        {
            isDemo = _loginUserService.IsDemoLogin(storedSession.UserName, storedSession.PersonId);
        }
        catch
        {
            // Database-backed mode is rechecked when available; normal local sessions remain usable offline.
        }

        SignIn(storedSession.UserName, storedSession.PersonId, isDemo);
    }

    public void SignOut()
    {
        IsSignedIn = false;
        UserName = null;
        PersonId = null;
        IsDemo = false;
    }

    public async Task SignOutAsync()
    {
        SignOut();
        await ClearBrowserStorageAsync();
    }

    private async Task<StoredLoginSession?> ReadBrowserStorageAsync(string browserStorageMethod)
    {
        try
        {
            var sessionJson = await _jsRuntime.InvokeAsync<string?>(browserStorageMethod, StorageKey);
            return string.IsNullOrWhiteSpace(sessionJson)
                ? null
                : JsonSerializer.Deserialize<StoredLoginSession>(sessionJson);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }
    }

    private async Task ClearBrowserStorageAsync()
    {
        await InvokeBrowserStorageAsync(() => _jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey).AsTask());
        await InvokeBrowserStorageAsync(() => _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", StorageKey).AsTask());
    }

    private static async Task InvokeBrowserStorageAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (InvalidOperationException)
        {
        }
        catch (JSException)
        {
        }
    }

    private sealed record StoredLoginSession(string UserName, long? PersonId, DateTimeOffset? ExpiresAt);
}
