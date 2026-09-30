using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Route("api/v1/ai")]
public class AiSupportController : ControllerBase
{
    private readonly IAiSupportService _aiSupportService;
    private readonly IAiSessionStore _sessionStore;

    public AiSupportController(IAiSupportService aiSupportService, IAiSessionStore sessionStore)
    {
        _aiSupportService = aiSupportService;
        _sessionStore = sessionStore;
    }

    /// <summary>
    /// Development test endpoint to simulate an incoming customer question and print the AI-generated reply to the .NET console.
    /// Endpoint: POST /api/ai/test-message (also accessible at POST /api/v1/ai/test-message)
    /// </summary>
    /// <param name="request">Request containing target customer phone and question/message text.</param>
    /// <returns>Standard ApiResponse containing generated AI response.</returns>
    [HttpPost("test-message")]
    public async Task<ActionResult<ApiResponse<AiChatResponseDto>>> TestMessage([FromBody] AiTestMessageRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(ApiErrorResponse.Create(
                400,
                "BAD_REQUEST",
                "Message text is required."
            ));
        }

        var phone = !string.IsNullOrWhiteSpace(request.FromPhone)
            ? request.FromPhone.Trim()
            : (!string.IsNullOrWhiteSpace(request.CustomerPhone) ? request.CustomerPhone.Trim() : "919876543210");

        var chatRequest = new AiChatRequestDto
        {
            CustomerPhone = phone,
            Message = request.Message.Trim(),
            CustomerName = request.CustomerName?.Trim(),
            Channel = "TestConsole"
        };

        var result = await _aiSupportService.ProcessCustomerMessageAsync(chatRequest);

        Console.WriteLine("========== AI CUSTOMER MESSAGE ==========");
        Console.WriteLine();
        Console.WriteLine($"From: {phone}");
        Console.WriteLine();
        Console.WriteLine("Customer:");
        Console.WriteLine(request.Message.Trim());
        Console.WriteLine();
        Console.WriteLine("AI Reply:");
        Console.WriteLine(result.ReplyMessage);
        Console.WriteLine();
        Console.WriteLine("=========================================");

        return Ok(ApiResponse<AiChatResponseDto>.Ok(result, "AI response generated and printed to console successfully."));
    }

    /// <summary>
    /// Processes an incoming customer message through the AI Orchestration layer,
    /// maintains session state, invokes real PostgreSQL tools, and generates safe responses.
    /// </summary>
    [HttpPost("chat")]
    public async Task<ActionResult<ApiResponse<AiChatResponseDto>>> Chat([FromBody] AiChatRequestDto request)
    {
        var result = await _aiSupportService.ProcessCustomerMessageAsync(request);
        return Ok(ApiResponse<AiChatResponseDto>.Ok(result, "AI response generated successfully."));
    }


    /// <summary>
    /// Retrieves current session memory, conversation history, and active draft order for a customer.
    /// </summary>
    [HttpGet("chat/session/{phone}")]
    public async Task<ActionResult<ApiResponse<AiChatSession>>> GetSession([FromRoute] string phone)
    {
        var session = await _aiSupportService.GetSessionAsync(phone);
        return Ok(ApiResponse<AiChatSession>.Ok(session, "Session retrieved successfully."));
    }

    /// <summary>
    /// Resets the conversation session and order draft memory for a customer.
    /// </summary>
    [HttpPost("chat/session/{phone}/reset")]
    public async Task<ActionResult<ApiResponse<bool>>> ResetSession([FromRoute] string phone)
    {
        var reset = await _aiSupportService.ResetSessionAsync(phone);
        return Ok(ApiResponse<bool>.Ok(reset, reset ? "Session reset successfully." : "Session not found or already reset."));
    }

    /// <summary>
    /// Manually sets or toggles human handover for a customer conversation.
    /// </summary>
    [HttpPost("chat/session/{phone}/handover")]
    public ActionResult<ApiResponse<bool>> SetHandover([FromRoute] string phone, [FromQuery] bool takeover = true, [FromQuery] string? reason = null)
    {
        var success = _sessionStore.SetHumanTakeover(phone, takeover, reason ?? "Manual admin takeover toggle");
        return Ok(ApiResponse<bool>.Ok(success, $"Human takeover state set to {takeover}."));
    }

    /// <summary>
    /// Returns standardized OpenAPI/JSON Schema function definitions for the 8 registered AI tools.
    /// </summary>
    [HttpGet("tools/definitions")]
    public ActionResult<ApiResponse<IReadOnlyList<AiToolDefinition>>> GetToolDefinitions()
    {
        var definitions = _aiSupportService.GetToolDefinitions();
        return Ok(ApiResponse<IReadOnlyList<AiToolDefinition>>.Ok(definitions, "Tool definitions retrieved successfully."));
    }

    /// <summary>
    /// Returns the active system prompt containing assistant role and safety rules.
    /// </summary>
    [HttpGet("prompt")]
    public ActionResult<ApiResponse<string>> GetSystemPrompt()
    {
        var prompt = _aiSupportService.GetSystemPrompt();
        return Ok(ApiResponse<string>.Ok(prompt, "System prompt retrieved successfully."));
    }
}
