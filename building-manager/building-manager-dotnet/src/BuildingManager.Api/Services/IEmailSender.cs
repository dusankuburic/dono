namespace BuildingManager.Api.Services;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string plainBody, string? htmlBody = null);

    Task SendPasswordResetEmailAsync(string email, string resetToken);
}

public class DevLoggingEmailSender : IEmailSender
{
    private readonly ILogger<DevLoggingEmailSender> _logger;

    public DevLoggingEmailSender(ILogger<DevLoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toEmail, string subject, string plainBody, string? htmlBody = null)
    {
        _logger.LogInformation("DEV EMAIL to {Email} — {Subject}: {Body}", toEmail, subject, plainBody);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken) =>
        SendAsync(email, "Reset your Building Manager password",
            $"Someone requested a password reset for your account. " +
            $"Reset code (valid 1 hour): {resetToken}");
}
