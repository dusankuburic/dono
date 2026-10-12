using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace BuildingManager.Orleans.Grains;

/// <summary>
/// Runs the platform's background jobs (see ISchedulerGrain). Every few
/// minutes it publishes scheduled announcements that came due; the first pass
/// of each day also sends assembly reminders and the managers' digest of
/// overdue keys and breached emergency deadlines. Notifications are per-user
/// grains keyed by email, like everywhere else.
/// </summary>
public class SchedulerGrain : Grain, ISchedulerGrain, IRemindable
{
    private const string SweepReminder = "sweep";
    private static readonly TimeSpan FirstSweepDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SweepPeriod = TimeSpan.FromMinutes(5);
    private static readonly UserRole[] ManagerRoles =
        { UserRole.Manager, UserRole.SubstituteManager, UserRole.Admin, UserRole.CompanyAdmin };

    private readonly IPersistentState<SchedulerState> _state;
    private readonly ILogger<SchedulerGrain> _logger;

    public SchedulerGrain(
        [PersistentState("scheduler", "buildingStorage")] IPersistentState<SchedulerState> state,
        ILogger<SchedulerGrain> logger)
    {
        _state = state;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // The reminder table survives restarts, so this mostly no-ops; on a
        // fresh database it is what starts the sweep cycle.
        await this.RegisterOrUpdateReminder(SweepReminder, FirstSweepDelay, SweepPeriod);
        await base.OnActivateAsync(cancellationToken);
    }

    public Task ReceiveReminder(string reminderName, TickStatus status) =>
        reminderName == SweepReminder ? RunSweepAsync() : Task.CompletedTask;

    public async Task RunSweepAsync()
    {
        var now = DateTime.UtcNow;
        var buildings = await GrainFactory.GetGrain<IBuildingRegistryGrain>("global").GetAllAsync();

        foreach (var building in buildings)
        {
            try
            {
                await PublishDueAnnouncementsAsync(building.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Announcement sweep failed for building {BuildingId}", building.Id);
            }
        }

        // The daily jobs (reminders, digests) run on the first pass of each
        // day; one pass per building failure keeps the rest of the day intact.
        if (_state.State.LastDailyPassUtc?.Date == now.Date)
            return;

        foreach (var building in buildings)
        {
            try
            {
                await DailyPassAsync(building.Id, now);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Daily pass failed for building {BuildingId}", building.Id);
            }
        }

        _state.State.LastDailyPassUtc = now;
        if (_state.State.AssemblyReminderStage.Count > 1_000)
        {
            // Old finished assemblies pile up slowly; drop the completed ones.
            var finished = _state.State.AssemblyReminderStage
                .Where(kv => kv.Value >= Done).Select(kv => kv.Key).ToList();
            foreach (var key in finished)
                _state.State.AssemblyReminderStage.Remove(key);
        }
        await _state.WriteStateAsync();
    }

    private async Task PublishDueAnnouncementsAsync(string buildingId)
    {
        var published = await GrainFactory.GetGrain<IBuildingGrain>(buildingId).PublishDueAnnouncementsAsync();
        if (published.Count == 0)
            return;

        var members = await MembersAsync(buildingId);
        foreach (var announcement in published)
        {
            foreach (var email in members)
            {
                await AddNotificationAsync(email, buildingId, NotificationType.Announcement,
                    $"Announcement: {announcement.Title}", Preview(announcement.Content), "/announcements");
            }
        }
    }

    private async Task DailyPassAsync(string buildingId, DateTime now)
    {
        await SendAssemblyRemindersAsync(buildingId, now);
        await SendManagerDigestAsync(buildingId, now);
    }

    /// <summary>UC-007: remind members of a noticed assembly 2 days, 1 day and on the day.</summary>
    private async Task SendAssemblyRemindersAsync(string buildingId, DateTime now)
    {
        var assemblyIds = await GrainFactory.GetGrain<IAssemblyRegistryGrain>(buildingId).GetAssemblyIdsAsync();
        foreach (var assemblyId in assemblyIds)
        {
            var assembly = await GrainFactory.GetGrain<IAssemblyGrain>($"{buildingId}|{assemblyId}").GetAsync();
            if (assembly.Status != AssemblyStatus.NoticeSent)
                continue;

            var target = ReminderStage(assembly.Date - now);
            var key = $"{buildingId}|{assemblyId}";
            var sent = _state.State.AssemblyReminderStage.GetValueOrDefault(key);
            if (target <= sent)
                continue;

            if (target is TwoDays or OneDayOrToday)
            {
                var members = await MembersAsync(buildingId);
                foreach (var email in members)
                {
                    await AddNotificationAsync(email, buildingId, NotificationType.Assembly,
                        "Reminder: owners' assembly",
                        $"{assembly.Date:dd.MM.yyyy HH:mm} (UTC) at {assembly.Location}", "/assemblies");
                }
            }
            _state.State.AssemblyReminderStage[key] = target;
        }
    }

    /// <summary>UC-002/UC-023: a daily digest for managers of keys past due and emergencies past 48 hours.</summary>
    private async Task SendManagerDigestAsync(string buildingId, DateTime now)
    {
        var building = GrainFactory.GetGrain<IBuildingGrain>(buildingId);

        var overdueKeys = (await building.GetOverdueKeysAsync()).ToList();
        var breachedEmergencies = (await building.GetWorkOrdersAsync())
            .Where(w => w.IsEmergency
                        && w.Status is not (WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
                        && w.CreatedAt.AddHours(48) < now)
            .ToList();

        if (overdueKeys.Count == 0 && breachedEmergencies.Count == 0)
            return;

        var managers = await ManagerMembersAsync(buildingId);
        foreach (var email in managers)
        {
            if (overdueKeys.Count > 0)
            {
                var keys = string.Join("\n", overdueKeys.Take(5).Select(k =>
                    $"Key {k.Identifier}{(string.IsNullOrEmpty(k.UnitNumber) ? "" : $" (unit {k.UnitNumber})")} to " +
                    $"{k.CurrentCheckout?.RecipientName}, was due {k.CurrentCheckout?.ExpectedReturn:dd.MM.yyyy}"));
                await AddNotificationAsync(email, buildingId, NotificationType.WorkOrder,
                    overdueKeys.Count == 1 ? "1 key is overdue for return" : $"{overdueKeys.Count} keys are overdue for return",
                    keys, "/keys");
            }

            if (breachedEmergencies.Count > 0)
            {
                var emergencies = string.Join("\n", breachedEmergencies.Take(5).Select(w =>
                    $"{w.ReferenceNumber}: {w.Title} (opened {w.CreatedAt:dd.MM.yyyy})"));
                await AddNotificationAsync(email, buildingId, NotificationType.WorkOrder,
                    "Emergency past the 48-hour deadline", emergencies, "/workorders");
            }
        }
    }

    // Reminder stages advance monotonically: 0 = nothing sent, 2 = the two-day
    // reminder, 3 = the one-day/day-of reminder, 4 = nothing left to send.
    private const int TwoDays = 2;
    private const int OneDayOrToday = 3;
    private const int Done = 4;

    private static int ReminderStage(TimeSpan untilMeeting) =>
        untilMeeting <= TimeSpan.Zero ? Done
        : untilMeeting < TimeSpan.FromDays(1) ? OneDayOrToday
        : untilMeeting < TimeSpan.FromDays(2) ? TwoDays
        : 0;

    private async Task<List<string>> MembersAsync(string buildingId) =>
        await GrainFactory.GetGrain<IBuildingAccessGrain>(buildingId).GetMemberEmailsAsync();

    /// <summary>Building members whose account has a management role (they may also be plain owners registered as members).</summary>
    private async Task<List<string>> ManagerMembersAsync(string buildingId)
    {
        var managers = new List<string>();
        foreach (var email in await MembersAsync(buildingId))
        {
            try
            {
                var profile = await GrainFactory.GetGrain<IUserGrain>(email.Trim().ToLowerInvariant()).GetProfileAsync();
                if (ManagerRoles.Contains(profile.Role))
                    managers.Add(email);
            }
            catch (KeyNotFoundException)
            {
                // Member without an account; nobody to notify.
            }
        }
        return managers;
    }

    private async Task AddNotificationAsync(string email, string buildingId, NotificationType type,
        string title, string? body, string link)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            return;

        await GrainFactory.GetGrain<INotificationGrain>(normalized).AddAsync(new NotificationInfo
        {
            Id = Guid.NewGuid(),
            Type = type,
            BuildingId = buildingId,
            Title = title,
            Body = body,
            Link = link,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>The first line or so of the message, enough to recognize it in the bell list.</summary>
    private static string? Preview(string content)
    {
        var trimmed = content.Trim();
        return trimmed.Length <= 150 ? trimmed : trimmed[..147] + "…";
    }
}

[GenerateSerializer]
public class SchedulerState
{
    /// <summary>When the daily jobs last ran; one pass per calendar day.</summary>
    [Id(0)] public DateTime? LastDailyPassUtc { get; set; }

    /// <summary>Highest reminder stage sent per assembly, keyed "{buildingId}|{assemblyId}".</summary>
    [Id(1)] public Dictionary<string, int> AssemblyReminderStage { get; set; } = new();
}
