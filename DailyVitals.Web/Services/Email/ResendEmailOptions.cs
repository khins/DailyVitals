namespace DailyVitals.Web.Services.Email;

public sealed class ResendEmailOptions
{
    public const string SectionName = "Resend";

    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = "no-reply@myactivevitals.com";
    public string FromName { get; set; } = "My Active Vitals";
}
