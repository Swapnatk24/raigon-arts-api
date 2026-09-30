using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks/whatsapp")]
[Route("api/webhooks/whatsapp")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IAiSupportService _aiSupportService;
    private readonly IAiSessionStore _sessionStore;
    private readonly IWhatsAppService _whatsAppService;
    private readonly WhatsAppSettings _whatsAppSettings;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    // Thread-safe in-memory deduplication cache for Meta message IDs (retains for 10 minutes)
    private static readonly ConcurrentDictionary<string, DateTime> ProcessedMessageIds = new();

    public WhatsAppWebhookController(
        IAiSupportService aiSupportService,
        IAiSessionStore sessionStore,
        IWhatsAppService whatsAppService,
        IOptions<WhatsAppSettings> whatsAppOptions,
        ILogger<WhatsAppWebhookController> logger)
    {
        _aiSupportService = aiSupportService;
        _sessionStore = sessionStore;
        _whatsAppService = whatsAppService;
        _whatsAppSettings = whatsAppOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Meta WhatsApp Cloud API Webhook Verification Endpoint.
    /// Handles GET verification handshake from Meta Developers dashboard.
    /// </summary>
    [HttpGet]
    public IActionResult VerifyWebhook(
        [FromQuery(Name = "hub.mode")] string? mode,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken,
        [FromQuery(Name = "hub.challenge")] string? challenge)
    {
        _logger.LogInformation("Received Meta WhatsApp Webhook verification request. Mode: {Mode}", mode);

        if (string.Equals(mode, "subscribe", StringComparison.OrdinalIgnoreCase))
        {
            var configuredToken = _whatsAppSettings.VerifyToken?.Trim();

            if (!string.IsNullOrEmpty(configuredToken) && string.Equals(verifyToken, configuredToken, StringComparison.Ordinal))
            {
                _logger.LogInformation("Meta Webhook verification succeeded. Returning hub.challenge.");
                return Content(challenge ?? string.Empty, "text/plain");
            }

            _logger.LogWarning("Meta Webhook verification failed. Invalid verify token received.");
            return StatusCode(403, "Verification token mismatch.");
        }

        return BadRequest("Invalid webhook verification parameters.");
    }

    /// <summary>
    /// Meta WhatsApp Cloud API Inbound Events Webhook Endpoint.
    /// Receives customer incoming messages, parses payload, checks human takeover status,
    /// dispatches to AI Support Orchestrator, and transmits AI replies back via WhatsApp.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement rawPayload)
    {
        // Meta expects a prompt 200 OK acknowledgment
        try
        {
            // Clean up stale message IDs older than 10 minutes
            CleanupStaleMessageIds();

            var payloadString = rawPayload.GetRawText();
            var payload = JsonSerializer.Deserialize<WhatsAppWebhookPayload>(payloadString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (payload?.Entry == null || payload.Entry.Count == 0)
            {
                return Ok();
            }

            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;

                foreach (var change in entry.Changes)
                {
                    var val = change.Value;
                    if (val == null) continue;

                    // Log status updates (e.g. sent, delivered, read)
                    if (val.Statuses != null && val.Statuses.Count > 0)
                    {
                        foreach (var st in val.Statuses)
                        {
                            _logger.LogDebug("WhatsApp message status update: ID={Id}, Status={Status}", st.Id, st.Status);
                        }
                    }

                    // Process incoming customer messages
                    if (val.Messages != null && val.Messages.Count > 0)
                    {
                        // Map contact names if provided by Meta
                        var contactMap = val.Contacts?.ToDictionary(c => c.WaId, c => c.Profile?.Name) 
                                         ?? new Dictionary<string, string?>();

                        foreach (var msg in val.Messages)
                        {
                            await ProcessIncomingMessageAsync(msg, contactMap);
                        }
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing incoming WhatsApp webhook payload.");
            // Always return 200 OK to Meta to avoid infinite retry loops on malformed payloads
            return Ok();
        }
    }

    private async Task ProcessIncomingMessageAsync(
        WhatsAppWebhookMessage message,
        Dictionary<string, string?> contactMap)
    {
        var messageId = message.Id;
        var fromPhone = message.From;

        // 1. Deduplication check: Ignore already processed messages
        if (!string.IsNullOrWhiteSpace(messageId))
        {
            if (ProcessedMessageIds.ContainsKey(messageId))
            {
                _logger.LogInformation("Ignoring duplicate WhatsApp message ID: {MessageId}", messageId);
                return;
            }
            ProcessedMessageIds.TryAdd(messageId, DateTime.UtcNow);
        }

        // 2. Extract message text
        string? textBody = null;
        if (message.Type == "text" && message.Text != null)
        {
            textBody = message.Text.Body?.Trim();
        }
        else if (message.Type == "image" && message.Image != null)
        {
            textBody = !string.IsNullOrWhiteSpace(message.Image.Caption) 
                ? message.Image.Caption.Trim() 
                : "Photo uploaded for custom framing";
        }
        else
        {
            _logger.LogInformation("Received non-text WhatsApp message of type '{Type}' from {Phone}. Ignored safely.",
                message.Type, MaskPhone(fromPhone));
            return;
        }

        if (string.IsNullOrWhiteSpace(textBody))
        {
            return;
        }

        contactMap.TryGetValue(fromPhone, out var customerName);
        var maskedPhone = MaskPhone(fromPhone);

        _logger.LogInformation("Processing incoming WhatsApp message from {Phone} (Name: {Name}): '{Text}'",
            maskedPhone, customerName ?? "Unknown", textBody.Length > 50 ? textBody[..50] + "..." : textBody);

        // 3. Check Session & Human Takeover status
        var existingSession = _sessionStore.GetSession(fromPhone);
        if (existingSession != null && existingSession.IsHumanTakeover)
        {
            _logger.LogInformation("Session for {Phone} is currently under human takeover. Suppressing AI response.", maskedPhone);
            // Record message in history for staff review
            _sessionStore.AddMessage(fromPhone, AiChatMessage.User(textBody, customerName));
            return;
        }

        // 4. Pass message to AI Support Orchestrator
        var chatRequest = new AiChatRequestDto
        {
            CustomerPhone = fromPhone,
            CustomerName = customerName,
            Message = textBody,
            Channel = "WhatsApp"
        };

        var aiResponse = await _aiSupportService.ProcessCustomerMessageAsync(chatRequest);

        // 5. Send AI Reply back to Customer WhatsApp via Meta Cloud API
        if (!string.IsNullOrWhiteSpace(aiResponse.ReplyMessage))
        {
            _logger.LogInformation("Transmitting AI response back to customer {Phone} via WhatsApp Cloud API...", maskedPhone);
            var sent = await _whatsAppService.SendTextMessageAsync(fromPhone, aiResponse.ReplyMessage);

            if (sent)
            {
                _logger.LogInformation("AI response successfully delivered to customer {Phone}.", maskedPhone);
            }
            else
            {
                _logger.LogWarning("Failed to deliver AI response to customer {Phone} via Meta API.", maskedPhone);
            }
        }
    }

    private static void CleanupStaleMessageIds()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        foreach (var kvp in ProcessedMessageIds)
        {
            if (kvp.Value < cutoff)
            {
                ProcessedMessageIds.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return "****";
        if (phone.Length <= 4) return "****";
        return string.Concat(phone.AsSpan(0, 2), "****", phone.AsSpan(phone.Length - 4));
    }
}
