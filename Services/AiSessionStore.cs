using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public class AiSessionStore : IAiSessionStore
{
    private readonly ConcurrentDictionary<string, AiChatSession> _sessions = new();
    private readonly AiSettings _settings;
    private readonly ILogger<AiSessionStore> _logger;

    public AiSessionStore(IOptions<AiSettings> settings, ILogger<AiSessionStore> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

        // Remove all non-digits except +
        var cleaned = Regex.Replace(phone.Trim(), @"[^\d+]", "");
        if (cleaned.StartsWith("+"))
        {
            cleaned = cleaned[1..];
        }

        // If local 10-digit number and country code provided, prepend default country code (e.g. 91)
        if (cleaned.Length == 10 && !string.IsNullOrEmpty(_settings.DefaultCountryCode))
        {
            cleaned = _settings.DefaultCountryCode + cleaned;
        }

        return "+" + cleaned;
    }

    public AiChatSession GetOrCreateSession(string customerPhone, string? channel = "WhatsApp", string? customerName = null)
    {
        var normalizedPhone = NormalizePhoneNumber(customerPhone);

        return _sessions.AddOrUpdate(
            normalizedPhone,
            phoneKey =>
            {
                _logger.LogInformation("Creating new AI chat session for {Phone} on {Channel}", phoneKey, channel);
                return new AiChatSession
                {
                    SessionId = $"sess_{Guid.NewGuid():N}",
                    CustomerPhone = phoneKey,
                    CustomerName = customerName,
                    Channel = channel ?? "WhatsApp",
                    Draft = new AiOrderDraft
                    {
                        CustomerPhone = phoneKey,
                        CustomerName = customerName
                    },
                    CreatedAt = DateTime.UtcNow,
                    LastActivityAt = DateTime.UtcNow
                };
            },
            (phoneKey, existing) =>
            {
                // Check if session has expired
                var timeout = TimeSpan.FromMinutes(_settings.SessionTimeoutMinutes);
                if (DateTime.UtcNow - existing.LastActivityAt > timeout)
                {
                    _logger.LogInformation("Chat session for {Phone} expired after {Timeout}. Refreshing session.", phoneKey, timeout);
                    return new AiChatSession
                    {
                        SessionId = $"sess_{Guid.NewGuid():N}",
                        CustomerPhone = phoneKey,
                        CustomerName = customerName ?? existing.CustomerName,
                        Channel = channel ?? existing.Channel,
                        Draft = new AiOrderDraft
                        {
                            CustomerPhone = phoneKey,
                            CustomerName = customerName ?? existing.CustomerName
                        },
                        CreatedAt = DateTime.UtcNow,
                        LastActivityAt = DateTime.UtcNow
                    };
                }

                existing.LastActivityAt = DateTime.UtcNow;
                if (!string.IsNullOrWhiteSpace(customerName))
                {
                    existing.CustomerName = customerName;
                    existing.Draft.CustomerName = customerName;
                }
                return existing;
            }
        );
    }

    public AiChatSession? GetSession(string customerPhone)
    {
        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        return _sessions.TryGetValue(normalizedPhone, out var session) ? session : null;
    }

    public bool ResetSession(string customerPhone)
    {
        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        var removed = _sessions.TryRemove(normalizedPhone, out _);
        if (removed)
        {
            _logger.LogInformation("Reset AI chat session for {Phone}", normalizedPhone);
        }
        return removed;
    }

    public bool SetHumanTakeover(string customerPhone, bool isHumanTakeover, string? reason = null)
    {
        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        if (_sessions.TryGetValue(normalizedPhone, out var session))
        {
            session.IsHumanTakeover = isHumanTakeover;
            session.HumanTakeoverReason = reason;
            session.LastActivityAt = DateTime.UtcNow;
            _logger.LogInformation("Session {Phone} human takeover updated to {Takeover}. Reason: {Reason}", normalizedPhone, isHumanTakeover, reason);
            return true;
        }
        return false;
    }

    public void UpdateDraft(string customerPhone, Action<AiOrderDraft> updateAction)
    {
        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        if (_sessions.TryGetValue(normalizedPhone, out var session))
        {
            updateAction(session.Draft);
            session.Draft.LastUpdated = DateTime.UtcNow;
            session.LastActivityAt = DateTime.UtcNow;
        }
    }

    public void AddMessage(string customerPhone, AiChatMessage message)
    {
        var normalizedPhone = NormalizePhoneNumber(customerPhone);
        if (_sessions.TryGetValue(normalizedPhone, out var session))
        {
            session.Messages.Add(message);
            session.LastActivityAt = DateTime.UtcNow;

            // Retain last 30 messages in memory to prevent unbounded memory growth
            if (session.Messages.Count > 30)
            {
                session.Messages.RemoveRange(0, session.Messages.Count - 30);
            }
        }
    }

    public IReadOnlyList<AiChatSession> GetAllActiveSessions()
    {
        return _sessions.Values.ToList();
    }
}
