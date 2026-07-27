using AgentPlatform.Api.DTOs;
using AgentPlatform.Api.Exceptions;
using AgentPlatform.Api.Repositories;
using AgentPlatform.Api.Services.Notifications;

namespace AgentPlatform.Api.Services;

public interface ILeadService
{
    Task<List<LeadResponseDto>> GetAllAsync(
        Guid tenantId,
        string? status,
        CancellationToken ct = default);
    Task<LeadResponseDto> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);
    Task<LeadResponseDto> UpdateStatusAsync(
        Guid id,
        string status,
        Guid tenantId,
        CancellationToken ct = default);
    Task<LeadResponseDto> UpdateNotesAsync(
        Guid id,
        string notes,
        Guid tenantId,
        CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default);
}

public class LeadService : ILeadService
{
    public static readonly string[] AllowedStatuses =
        ["new", "contacted", "qualified", "lost"];

    private readonly ILeadRepository _leads;
    private readonly INotificationService _notifications;

    public LeadService(ILeadRepository leads, INotificationService notifications)
    {
        _leads = leads;
        _notifications = notifications;
    }

    public async Task<List<LeadResponseDto>> GetAllAsync(
        Guid tenantId,
        string? status,
        CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(status) && !AllowedStatuses.Contains(status))
        {
            throw ValidationException.ForField(
                "status",
                $"Status invalid. Acceptate: {string.Join(", ", AllowedStatuses)}.");
        }

        var leads = await _leads.GetAllAsync(tenantId, status, ct);
        return leads.Select(lead => LeadResponseDto.From(lead)).ToList();
    }

    public async Task<LeadResponseDto> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var lead = await _leads.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Lead();

        // Detaliul include conversatia, ca drawerul din frontend sa aiba istoricul
        return LeadResponseDto.From(lead, includeConversation: true);
    }

    public async Task<LeadResponseDto> UpdateStatusAsync(
        Guid id,
        string status,
        Guid tenantId,
        CancellationToken ct = default)
    {
        if (!AllowedStatuses.Contains(status))
        {
            throw ValidationException.ForField(
                "status",
                $"Status invalid. Acceptate: {string.Join(", ", AllowedStatuses)}.");
        }

        var lead = await _leads.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Lead();

        // Retinut inainte de scriere: dupa UpdateAsync nu mai putem sti daca
        // statusul chiar s-a schimbat, deci am notifica la fiecare salvare.
        var wasQualified = lead.Status == "qualified";

        lead.Status = status;
        var updated = await _leads.UpdateAsync(lead, ct);

        if (status == "qualified" && !wasQualified)
        {
            await _notifications.NotifyAsync(
                tenantId,
                NotificationTypes.LeadQualified,
                "Lead calificat",
                $"{updated.Name ?? updated.Phone} a fost marcat drept calificat.",
                "/dashboard/leads",
                "success",
                ct);
        }

        return LeadResponseDto.From(updated);
    }

    public async Task<LeadResponseDto> UpdateNotesAsync(
        Guid id,
        string notes,
        Guid tenantId,
        CancellationToken ct = default)
    {
        var lead = await _leads.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Lead();

        lead.Notes = notes;
        var updated = await _leads.UpdateAsync(lead, ct);
        return LeadResponseDto.From(updated);
    }

    public async Task DeleteAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var lead = await _leads.GetByIdAsync(id, tenantId, ct)
            ?? throw NotFoundException.Lead();

        await _leads.DeleteAsync(lead, ct);
    }
}
