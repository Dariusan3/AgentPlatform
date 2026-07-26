using AgentPlatform.Api.Data;
using AgentPlatform.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgentPlatform.Api.Repositories;

public interface IVoiceCallRepository
{
    Task<VoiceCall> CreateAsync(VoiceCall call, CancellationToken ct = default);
    Task<VoiceCall> UpdateAsync(VoiceCall call, CancellationToken ct = default);

    /// <summary>
    /// Fara filtru de tenant: stream-ul WebSocket si webhookul de status nu au
    /// JWT, iar CallSid e unic global si vine semnat de Twilio.
    /// </summary>
    Task<VoiceCall?> GetByCallSidAsync(string callSid, CancellationToken ct = default);

    Task<List<VoiceCall>> GetByAgentAsync(
        Guid agentId,
        Guid tenantId,
        CancellationToken ct = default);

    Task<VoiceCall?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default);

    Task<List<VoiceMessage>> GetMessagesAsync(Guid callId, CancellationToken ct = default);
    Task<VoiceMessage> AddMessageAsync(VoiceMessage message, CancellationToken ct = default);
}

public class VoiceCallRepository : IVoiceCallRepository
{
    private readonly AppDbContext _db;

    public VoiceCallRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<VoiceCall> CreateAsync(VoiceCall call, CancellationToken ct = default)
    {
        _db.VoiceCalls.Add(call);
        await _db.SaveChangesAsync(ct);
        return call;
    }

    public async Task<VoiceCall> UpdateAsync(VoiceCall call, CancellationToken ct = default)
    {
        _db.VoiceCalls.Update(call);
        await _db.SaveChangesAsync(ct);
        return call;
    }

    public Task<VoiceCall?> GetByCallSidAsync(string callSid, CancellationToken ct = default) =>
        _db.VoiceCalls
            .IgnoreQueryFilters()
            .Include(c => c.VoiceAgent)
            .FirstOrDefaultAsync(c => c.CallSid == callSid, ct);

    public Task<List<VoiceCall>> GetByAgentAsync(
        Guid agentId,
        Guid tenantId,
        CancellationToken ct = default) =>
        _db.VoiceCalls
            .Where(c => c.VoiceAgentId == agentId && c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<VoiceCall?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default) =>
        _db.VoiceCalls
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);

    public Task<List<VoiceMessage>> GetMessagesAsync(
        Guid callId,
        CancellationToken ct = default) =>
        _db.VoiceMessages
            .IgnoreQueryFilters()
            .Where(m => m.VoiceCallId == callId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<VoiceMessage> AddMessageAsync(
        VoiceMessage message,
        CancellationToken ct = default)
    {
        _db.VoiceMessages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }
}
