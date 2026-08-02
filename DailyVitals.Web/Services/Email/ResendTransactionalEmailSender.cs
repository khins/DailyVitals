using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace DailyVitals.Web.Services.Email;

public sealed class ResendTransactionalEmailSender(
    HttpClient httpClient,
    IOptions<ResendEmailOptions> options,
    ILogger<ResendTransactionalEmailSender> logger) : ITransactionalEmailSender
{
    private readonly ResendEmailOptions _options = options.Value;

    public async Task<EmailSendResult> SendAsync(
        TransactionalEmailMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Resend:ApiKey is not configured.");

        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Resend:FromEmail is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "emails");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            from = FormatSender(_options.FromName, _options.FromEmail),
            to = new[] { message.To },
            subject = message.Subject,
            html = message.HtmlBody,
            text = message.TextBody
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Resend rejected an email request with HTTP status {StatusCode}.",
                (int)response.StatusCode);
            throw new HttpRequestException(
                $"Resend email request failed with HTTP status {(int)response.StatusCode}: {GetErrorMessage(responseBody)}",
                null,
                response.StatusCode);
        }

        var result = JsonSerializer.Deserialize<ResendSendResponse>(responseBody);
        if (string.IsNullOrWhiteSpace(result?.Id))
            throw new InvalidOperationException("Resend returned a successful response without an email id.");

        return new EmailSendResult(result.Id);
    }

    private static string FormatSender(string name, string email) =>
        string.IsNullOrWhiteSpace(name) ? email : $"{name.Trim()} <{email.Trim()}>";

    private static string GetErrorMessage(string responseBody)
    {
        try
        {
            return JsonSerializer.Deserialize<ResendErrorResponse>(responseBody)?.Message
                ?? "The provider did not return an error message.";
        }
        catch (JsonException)
        {
            return "The provider returned an unreadable error response.";
        }
    }

    private sealed record ResendSendResponse(
        [property: JsonPropertyName("id")] string Id);

    private sealed record ResendErrorResponse(
        [property: JsonPropertyName("message")] string? Message);
}
