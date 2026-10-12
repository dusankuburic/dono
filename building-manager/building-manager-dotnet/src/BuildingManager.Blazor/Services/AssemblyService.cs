using System.Net.Http;
using System.Net.Http.Json;
using BuildingManager.Core.Enums;
using BuildingManager.Orleans.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingManager.Blazor.Services;

public interface IAssemblyService
{
    Task<List<AssemblySummary>> GetAssembliesAsync(string buildingId);
    Task<AssemblyDetails> GetAssemblyAsync(string buildingId, Guid assemblyId);
    Task<AssemblyDetails> ScheduleAsync(string buildingId, ScheduleAssemblyRequest request);
    Task<AssemblyDetails> ScheduleRepeatAsync(string buildingId, Guid firstSessionId, DateTime dateUtc, string? location, bool allowShortNotice);
    Task<AssemblyDetails> AddAgendaItemAsync(string buildingId, Guid assemblyId, AgendaItemDraft item);
    Task<AssemblyDetails> RemoveAgendaItemAsync(string buildingId, Guid assemblyId, Guid itemId);
    Task<AssemblyDetails> MarkNoticeSentAsync(string buildingId, Guid assemblyId, bool physicallyPosted);
    Task<AssemblyDetails> StartAsync(string buildingId, Guid assemblyId);
    Task<AssemblyDetails> RecordAttendanceAsync(string buildingId, Guid assemblyId, AttendanceDraft attendance);
    Task<AssemblyDetails> RemoveAttendanceAsync(string buildingId, Guid assemblyId, string unitNumber);
    Task<AssemblyDetails> RecordVoteAsync(string buildingId, Guid assemblyId, Guid itemId, string unitNumber, VoteChoice choice);
    Task<AssemblyDetails> FinalizeAgendaItemAsync(string buildingId, Guid assemblyId, Guid itemId);
    Task<AssemblyDetails> SetNotesAsync(string buildingId, Guid assemblyId, string? notes);
    Task<AssemblyDetails> CompleteAsync(string buildingId, Guid assemblyId);
    Task<AssemblyDetails> CancelAsync(string buildingId, Guid assemblyId);
}

public class AssemblyService : IAssemblyService
{
    private readonly HttpClient _httpClient;

    public AssemblyService([FromKeyedServices("ApiHttpClient")] HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    private static string Base(string buildingId) => $"/api/buildings/{Uri.EscapeDataString(buildingId)}/assemblies";
    private static string One(string buildingId, Guid assemblyId) => $"{Base(buildingId)}/{assemblyId}";

    public async Task<List<AssemblySummary>> GetAssembliesAsync(string buildingId)
    {
        var response = await _httpClient.GetAsync(Base(buildingId));
        await ApiErrors.EnsureSuccessAsync(response, "Could not load the assemblies.");
        return await response.Content.ReadFromJsonAsync<List<AssemblySummary>>() ?? new();
    }

    public async Task<AssemblyDetails> GetAssemblyAsync(string buildingId, Guid assemblyId) =>
        await Read(await _httpClient.GetAsync(One(buildingId, assemblyId)), "Could not load the assembly.");

    public async Task<AssemblyDetails> ScheduleAsync(string buildingId, ScheduleAssemblyRequest request) =>
        await Read(await _httpClient.PostAsJsonAsync(Base(buildingId), request), "Could not schedule the assembly.");

    public async Task<AssemblyDetails> ScheduleRepeatAsync(
        string buildingId, Guid firstSessionId, DateTime dateUtc, string? location, bool allowShortNotice) =>
        await Read(await _httpClient.PostAsJsonAsync($"{One(buildingId, firstSessionId)}/repeat",
            new { Date = dateUtc, Location = location, AllowShortNotice = allowShortNotice }),
            "Could not schedule the repeated session.");

    public async Task<AssemblyDetails> AddAgendaItemAsync(string buildingId, Guid assemblyId, AgendaItemDraft item) =>
        await Read(await _httpClient.PostAsJsonAsync($"{One(buildingId, assemblyId)}/agenda", item),
            "Could not add the agenda item.");

    public async Task<AssemblyDetails> RemoveAgendaItemAsync(string buildingId, Guid assemblyId, Guid itemId) =>
        await Read(await _httpClient.DeleteAsync($"{One(buildingId, assemblyId)}/agenda/{itemId}"),
            "Could not remove the agenda item.");

    public async Task<AssemblyDetails> MarkNoticeSentAsync(string buildingId, Guid assemblyId, bool physicallyPosted) =>
        await Read(await _httpClient.PostAsJsonAsync($"{One(buildingId, assemblyId)}/notice",
            new { PhysicallyPosted = physicallyPosted }), "Could not record the notice.");

    public async Task<AssemblyDetails> StartAsync(string buildingId, Guid assemblyId) =>
        await Read(await _httpClient.PostAsync($"{One(buildingId, assemblyId)}/start", null),
            "Could not start the meeting.");

    public async Task<AssemblyDetails> RecordAttendanceAsync(string buildingId, Guid assemblyId, AttendanceDraft attendance) =>
        await Read(await _httpClient.PutAsJsonAsync($"{One(buildingId, assemblyId)}/attendance", attendance),
            "Could not record attendance.");

    public async Task<AssemblyDetails> RemoveAttendanceAsync(string buildingId, Guid assemblyId, string unitNumber) =>
        await Read(await _httpClient.DeleteAsync(
            $"{One(buildingId, assemblyId)}/attendance?unit={Uri.EscapeDataString(unitNumber)}"),
            "Could not remove the attendance.");

    public async Task<AssemblyDetails> RecordVoteAsync(
        string buildingId, Guid assemblyId, Guid itemId, string unitNumber, VoteChoice choice) =>
        await Read(await _httpClient.PutAsJsonAsync($"{One(buildingId, assemblyId)}/agenda/{itemId}/vote",
            new { UnitNumber = unitNumber, Choice = choice }), "Could not record the vote.");

    public async Task<AssemblyDetails> FinalizeAgendaItemAsync(string buildingId, Guid assemblyId, Guid itemId) =>
        await Read(await _httpClient.PostAsync($"{One(buildingId, assemblyId)}/agenda/{itemId}/finalize", null),
            "Could not decide the item.");

    public async Task<AssemblyDetails> SetNotesAsync(string buildingId, Guid assemblyId, string? notes) =>
        await Read(await _httpClient.PutAsJsonAsync($"{One(buildingId, assemblyId)}/notes", new { Notes = notes }),
            "Could not save the notes.");

    public async Task<AssemblyDetails> CompleteAsync(string buildingId, Guid assemblyId) =>
        await Read(await _httpClient.PostAsync($"{One(buildingId, assemblyId)}/complete", null),
            "Could not complete the assembly.");

    public async Task<AssemblyDetails> CancelAsync(string buildingId, Guid assemblyId) =>
        await Read(await _httpClient.PostAsync($"{One(buildingId, assemblyId)}/cancel", null),
            "Could not cancel the assembly.");

    private static async Task<AssemblyDetails> Read(HttpResponseMessage response, string fallbackMessage)
    {
        await ApiErrors.EnsureSuccessAsync(response, fallbackMessage);
        return await response.Content.ReadFromJsonAsync<AssemblyDetails>()
            ?? throw new InvalidOperationException(fallbackMessage);
    }
}
