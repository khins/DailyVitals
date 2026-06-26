using System.Text.Json;
using Microsoft.JSInterop;

namespace DailyVitals.Web.Services;

public sealed class LocalLoginSession
{
    private const string StorageKey = "dailyvitals.loginSession";
    private const int RememberDeviceDays = 30;
    private readonly IJSRuntime _jsRuntime;

    public LocalLoginSession(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsSignedIn { get; private set; }
    public string? UserName { get; private set; }
    public long? PersonId { get; private set; }

    public void SignIn(string userName, long? personId)
    {
        IsSignedIn = true;
        UserName = userName;
        PersonId = personId;
    }

    public async Task SignInAsync(string userName, long? personId, bool rememberDevice)
    {
        SignIn(userName, personId);

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

        SignIn(storedSession.UserName, storedSession.PersonId);
    }

    public void SignOut()
    {
        IsSignedIn = false;
        UserName = null;
        PersonId = null;
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
