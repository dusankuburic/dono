namespace BuildingManager.Api.Services;

/// <summary>
/// Outbound SMS abstraction (UC-005: SMS costs apply). The dev implementation
/// logs instead of sending; production registers AcsSmsSender when
/// Sms:ConnectionString and Sms:From are configured. Phone numbers are E.164
/// (e.g. +381641234567).
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string toPhone, string message);
}

public class DevLoggingSmsSender : ISmsSender
{
    private readonly ILogger<DevLoggingSmsSender> _logger;

    public DevLoggingSmsSender(ILogger<DevLoggingSmsSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string toPhone, string message)
    {
        _logger.LogInformation("DEV SMS to {Phone}: {Message}", toPhone, message);
        return Task.CompletedTask;
    }
}
