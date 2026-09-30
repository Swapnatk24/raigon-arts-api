using RaigonArts.Api.DTOs;

namespace RaigonArts.Api.Services;

public interface IAiSupportService
{
    /// <summary>
    /// Processes an incoming customer message, manages conversation context, invokes AI business tools,
    /// adheres strictly to safety/confirmation guidelines, and generates an automated or handover response.
    /// </summary>
    Task<AiChatResponseDto> ProcessCustomerMessageAsync(AiChatRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes one of the 8 registered AI business tools against the PostgreSQL database.
    /// </summary>
    Task<AiToolCallResultDto> ExecuteToolCallAsync(string toolName, string argumentsJson, AiChatSession session, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves current conversational memory session for a customer.
    /// </summary>
    Task<AiChatSession> GetSessionAsync(string customerPhone);

    /// <summary>
    /// Resets the conversation session and draft order context for a customer.
    /// </summary>
    Task<bool> ResetSessionAsync(string customerPhone);

    /// <summary>
    /// Escalates conversation to workshop staff and stops automated AI replies.
    /// </summary>
    Task<AiTransferToHumanResponseDto> EscalateToHumanAsync(string customerPhone, string customerName, string reason, string? channel = "WhatsApp");

    /// <summary>
    /// Returns the standardized JSON Schema definitions for the 8 business tools.
    /// </summary>
    IReadOnlyList<AiToolDefinition> GetToolDefinitions();

    /// <summary>
    /// Generates the strict AI system instructions for business role, framing guidelines, safety rules, and tools.
    /// </summary>
    string GetSystemPrompt();
}
