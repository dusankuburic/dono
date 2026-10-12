namespace BuildingManager.Api.Services;

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
