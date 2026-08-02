namespace DailyVitals.Web.Services.Email;

public interface ITransactionalEmailSender
{
    Task<EmailSendResult> SendAsync(
        TransactionalEmailMessage message,
        CancellationToken cancellationToken = default);
}

public sealed record TransactionalEmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string TextBody);

public sealed record EmailSendResult(string ProviderMessageId);
