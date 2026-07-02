using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace DailyVitals.Web.Services;

public sealed class AuthTicketService
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromMinutes(2);
    private readonly ITimeLimitedDataProtector _protector;

    public AuthTicketService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider
            .CreateProtector("DailyVitals.Web.AuthenticationTicket.v1")
            .ToTimeLimitedDataProtector();
    }

    public string Issue(string userName, long personId, bool isDemo, bool rememberDevice)
    {
        var ticket = new AuthTicket(userName, personId, isDemo, rememberDevice);
        return _protector.Protect(JsonSerializer.Serialize(ticket), TicketLifetime);
    }

    public bool TryRedeem(string? protectedTicket, out AuthTicket ticket)
    {
        ticket = default!;
        if (string.IsNullOrWhiteSpace(protectedTicket))
            return false;

        try
        {
            var json = _protector.Unprotect(protectedTicket);
            var parsed = JsonSerializer.Deserialize<AuthTicket>(json);
            if (parsed is null || string.IsNullOrWhiteSpace(parsed.UserName) || parsed.PersonId <= 0)
                return false;

            ticket = parsed;
            return true;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public sealed record AuthTicket(string UserName, long PersonId, bool IsDemo, bool RememberDevice);
}
