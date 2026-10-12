using Azure.Communication.Email;
using Azure.Communication.Sms;

namespace BuildingManager.Api.Services;

/// <summary>
/// Real email via Azure Communication Services. Registered when
/// Email:ConnectionString and Email:SenderAddress are configured; the sender
/// address must be verified in the ACS resource (DoNotReply@domain or similar).
/// </summary>
public class AcsEmailSender : IEmailSender
{
    private readonly EmailClient _client;
    private readonly string _senderAddress;

    public AcsEmailSender(string connectionString, string senderAddress)
    {
        _client = new EmailClient(connectionString);
        _senderAddress = senderAddress;
    }

    public async Task SendAsync(string toEmail, string subject, string plainBody, string? htmlBody = null)
    {
        var message = new EmailMessage(
            senderAddress: _senderAddress,
            content: new EmailContent(subject) { PlainText = plainBody, Html = htmlBody },
            recipients: new EmailRecipients(new[] { new EmailAddress(toEmail) }));

        // WaitUntil.Completed so a rejected send surfaces here instead of
        // silently vanishing into ACS's queue.
        await _client.SendAsync(Azure.WaitUntil.Completed, message);
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken) =>
        SendAsync(email, "Reset your Building Manager password",
            $"Someone requested a password reset for your account.\n\n" +
            $"Reset code (valid 1 hour): {resetToken}\n\n" +
            "If this wasn't you, you can ignore this message.");
}

/// <summary>
/// Real SMS via Azure Communication Services (E.164 numbers). Registered when
/// Sms:ConnectionString and Sms:From are configured; SMS costs apply.
/// </summary>
public class AcsSmsSender : ISmsSender
{
    private readonly SmsClient _client;
    private readonly string _from;

    public AcsSmsSender(string connectionString, string fromNumber)
    {
        _client = new SmsClient(connectionString);
        _from = fromNumber;
    }

    public async Task SendAsync(string toPhone, string message)
    {
        await _client.SendAsync(
            from: _from,
            to: toPhone,
            message: message,
            options: new SmsSendOptions(enableDeliveryReport: true));
    }
}
