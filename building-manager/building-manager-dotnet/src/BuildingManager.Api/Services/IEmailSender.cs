namespace BuildingManager.Api.Services;

/// <summary>
/// Outbound email abstraction. The dev implementation logs instead of sending;
/// production should register a real sender (SMTP, SendGrid, ...).
/// </summary>
public interface IEmailSender
{
    Task SendPasswordResetEmailAsync(string email, string resetToken);
}

public class DevLoggingEmailSender : IEmailSender
{
    private readonly ILogger<DevLoggingEmailSender> _logger;

    public DevLoggingEmailSender(ILogger<DevLoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetToken)
    {
        _logger.LogInformation(
            "DEV EMAIL to {Email}: password reset requested. Token: {Token} (valid 1 hour)",
            email, resetToken);
        return Task.CompletedTask;
    }
}
