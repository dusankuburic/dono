using System.ComponentModel.DataAnnotations;
using BuildingManager.Api.Authorization;
using BuildingManager.Api.Reports;
using BuildingManager.Api.Services;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace BuildingManager.Api.Controllers;

/// <summary>
/// Owners' assemblies of a building. Anyone with access to the building can
/// read them (owners see agendas, results and minutes); only management roles
/// run them. Every mutation returns the updated assembly.
/// </summary>
[ApiController]
[Authorize(Policy = AuthPolicies.BuildingAccess)]
[Route("api/buildings/{buildingId}/assemblies")]
public class AssembliesController : ControllerBase
{
    private readonly IGrainFactory _grainFactory;
    private readonly INotificationDispatcher _notifications;
    private readonly AssemblyReportGenerator _reports;

    public AssembliesController(IGrainFactory grainFactory, INotificationDispatcher notifications,
        AssemblyReportGenerator reports)
    {
        _grainFactory = grainFactory;
        _notifications = notifications;
        _reports = reports;
    }

    // The building is part of the grain key, so an assembly id from another
    // building resolves to an empty grain (404) instead of leaking data.
    private IAssemblyGrain Grain(string buildingId, Guid assemblyId) =>
        _grainFactory.GetGrain<IAssemblyGrain>($"{buildingId}|{assemblyId}");

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssemblySummary>>> List(string buildingId)
    {
        var ids = await _grainFactory.GetGrain<IAssemblyRegistryGrain>(buildingId).GetAssemblyIdsAsync();

        var summaries = new List<AssemblySummary>();
        foreach (var id in ids)
        {
            try
            {
                summaries.Add(await Grain(buildingId, id).GetSummaryAsync());
            }
            catch (KeyNotFoundException)
            {
                // Registered but never written (scheduling failed): skip it.
            }
        }

        return Ok(summaries.OrderByDescending(s => s.Date));
    }

    [HttpGet("{assemblyId:guid}")]
    public async Task<ActionResult<AssemblyDetails>> Get(string buildingId, Guid assemblyId) =>
        Ok(await Grain(buildingId, assemblyId).GetAsync());

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public async Task<ActionResult<AssemblyDetails>> Schedule(string buildingId, [FromBody] ScheduleAssemblyRequest request)
    {
        // Repeated sessions go through /repeat so their link is validated.
        var details = await ScheduleNew(buildingId, request with
        {
            SessionType = SessionType.First,
            PreviousAssemblyId = null
        });
        return CreatedAtAction(nameof(Get), new { buildingId, assemblyId = details.Id }, details);
    }

    /// <summary>Schedules the repeated session of a first session that did not reach quorum, with the same agenda.</summary>
    [HttpPost("{assemblyId:guid}/repeat")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public async Task<ActionResult<AssemblyDetails>> Repeat(string buildingId, Guid assemblyId, [FromBody] RepeatAssemblyRequest request)
    {
        var first = await Grain(buildingId, assemblyId).GetAsync();
        var details = await ScheduleNew(buildingId, new ScheduleAssemblyRequest
        {
            Date = request.Date,
            Location = string.IsNullOrWhiteSpace(request.Location) ? first.Location : request.Location,
            SessionType = SessionType.Repeated,
            PreviousAssemblyId = assemblyId,
            AllowShortNotice = request.AllowShortNotice,
            Agenda = first.Agenda.Select(i => new AgendaItemDraft
            {
                Title = i.Title,
                Description = i.Description,
                VotingType = i.VotingType,
                RequiresVote = i.RequiresVote
            }).ToList()
        });
        return CreatedAtAction(nameof(Get), new { buildingId, assemblyId = details.Id }, details);
    }

    [HttpPost("{assemblyId:guid}/agenda")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> AddAgendaItem(string buildingId, Guid assemblyId, [FromBody] AgendaItemDraft item) =>
        Mutate(buildingId, assemblyId, g => g.AddAgendaItemAsync(item));

    [HttpDelete("{assemblyId:guid}/agenda/{itemId:guid}")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> RemoveAgendaItem(string buildingId, Guid assemblyId, Guid itemId) =>
        Mutate(buildingId, assemblyId, g => g.RemoveAgendaItemAsync(itemId));

    [HttpPost("{assemblyId:guid}/notice")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> MarkNoticeSent(string buildingId, Guid assemblyId, [FromBody] NoticeRequest request) =>
        Mutate(buildingId, assemblyId, g => g.MarkNoticeSentAsync(request.PhysicallyPosted));

    [HttpPost("{assemblyId:guid}/start")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> Start(string buildingId, Guid assemblyId) =>
        Mutate(buildingId, assemblyId, g => g.StartAsync());

    [HttpPut("{assemblyId:guid}/attendance")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> RecordAttendance(string buildingId, Guid assemblyId, [FromBody] AttendanceDraft attendance) =>
        Mutate(buildingId, assemblyId, g => g.RecordAttendanceAsync(attendance));

    /// <summary>The unit goes in the query string: unit numbers may contain characters awkward in a path.</summary>
    [HttpDelete("{assemblyId:guid}/attendance")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> RemoveAttendance(string buildingId, Guid assemblyId, [FromQuery, Required] string unit) =>
        Mutate(buildingId, assemblyId, g => g.RemoveAttendanceAsync(unit));

    [HttpPut("{assemblyId:guid}/agenda/{itemId:guid}/vote")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> RecordVote(string buildingId, Guid assemblyId, Guid itemId, [FromBody] VoteRequest vote) =>
        Mutate(buildingId, assemblyId, g => g.RecordVoteAsync(itemId, vote.UnitNumber, vote.Choice));

    [HttpPost("{assemblyId:guid}/agenda/{itemId:guid}/finalize")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> FinalizeAgendaItem(string buildingId, Guid assemblyId, Guid itemId) =>
        Mutate(buildingId, assemblyId, g => g.FinalizeAgendaItemAsync(itemId));

    [HttpPut("{assemblyId:guid}/notes")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> SetNotes(string buildingId, Guid assemblyId, [FromBody] NotesRequest request) =>
        Mutate(buildingId, assemblyId, g => g.SetNotesAsync(request.Notes));

    [HttpPost("{assemblyId:guid}/complete")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> Complete(string buildingId, Guid assemblyId) =>
        Mutate(buildingId, assemblyId, g => g.CompleteAsync());

    [HttpPost("{assemblyId:guid}/cancel")]
    [Authorize(Policy = AuthPolicies.ManageOnly)]
    public Task<AssemblyDetails> Cancel(string buildingId, Guid assemblyId) =>
        Mutate(buildingId, assemblyId, g => g.CancelAsync());

    /// <summary>The formal notice, printable for physical posting (UC-007).</summary>
    [HttpGet("{assemblyId:guid}/notice/pdf")]
    public async Task<IActionResult> NoticePdf(string buildingId, Guid assemblyId)
    {
        var (building, assembly) = await AssemblyWithBuildingAsync(buildingId, assemblyId);
        return File(_reports.NoticePdf(building, assembly), "application/pdf",
            $"assembly-notice-{assembly.Date:yyyy-MM-dd}.pdf");
    }

    /// <summary>The minutes as a PDF (UC-006); printable for signature and archiving.</summary>
    [HttpGet("{assemblyId:guid}/minutes/pdf")]
    public async Task<IActionResult> MinutesPdf(string buildingId, Guid assemblyId)
    {
        var (building, assembly) = await AssemblyWithBuildingAsync(buildingId, assemblyId);
        return File(_reports.MinutesPdf(building, assembly), "application/pdf",
            $"assembly-minutes-{assembly.Date:yyyy-MM-dd}.pdf");
    }

    private async Task<(BuildingSummary Building, AssemblyDetails Assembly)> AssemblyWithBuildingAsync(
        string buildingId, Guid assemblyId)
    {
        var building = await _grainFactory.GetGrain<IBuildingGrain>(buildingId).GetSummaryAsync();
        return (building, await Grain(buildingId, assemblyId).GetAsync());
    }

    private async Task<AssemblyDetails> ScheduleNew(string buildingId, ScheduleAssemblyRequest request)
    {
        // The assembly needs an initialized building for its units and shares.
        await _grainFactory.GetGrain<IBuildingGrain>(buildingId).GetSummaryAsync();

        var details = await Grain(buildingId, Guid.NewGuid()).ScheduleAsync(request with
        {
            CreatedBy = User.FindFirst("email")?.Value
        });

        // UC-007: members are told in-app that an assembly is scheduled
        // (email/SMS delivery of the formal notice is a later phase).
        var session = details.SessionType == SessionType.Repeated ? "Repeated assembly" : "Owners' assembly";
        await _notifications.NotifyBuildingMembersAsync(buildingId, NotificationType.Assembly,
            $"{session} on {details.Date.ToLocalTime():ddd, d MMM yyyy, HH:mm}",
            details.Location, "/assemblies", User.FindFirst("email")?.Value);

        return details;
    }

    private async Task<AssemblyDetails> Mutate(string buildingId, Guid assemblyId, Func<IAssemblyGrain, Task> action)
    {
        var grain = Grain(buildingId, assemblyId);
        await action(grain);
        return await grain.GetAsync();
    }
}

public record RepeatAssemblyRequest
{
    /// <summary>Date and time of the repeated session (UTC).</summary>
    public DateTime Date { get; init; }

    /// <summary>Defaults to the first session's location.</summary>
    [MaxLength(200)]
    public string? Location { get; init; }

    public bool AllowShortNotice { get; init; }
}

public record NoticeRequest
{
    /// <summary>The notice was also posted in the building (legal proof).</summary>
    public bool PhysicallyPosted { get; init; }
}

public record VoteRequest
{
    [Required, MaxLength(20)]
    public string UnitNumber { get; init; } = "";

    public VoteChoice Choice { get; init; }
}

public record NotesRequest
{
    [MaxLength(10_000)]
    public string? Notes { get; init; }
}
