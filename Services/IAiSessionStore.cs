using RaigonArts.Api.DTOs;

namespace RaigonArts.Api.Services;

public interface IAiSessionStore
{
    AiChatSession GetOrCreateSession(string customerPhone, string? channel = "WhatsApp", string? customerName = null);
    AiChatSession? GetSession(string customerPhone);
    bool ResetSession(string customerPhone);
    bool SetHumanTakeover(string customerPhone, bool isHumanTakeover, string? reason = null);
    void UpdateDraft(string customerPhone, Action<AiOrderDraft> updateAction);
    void AddMessage(string customerPhone, AiChatMessage message);
    IReadOnlyList<AiChatSession> GetAllActiveSessions();
    string NormalizePhoneNumber(string phone);
}
