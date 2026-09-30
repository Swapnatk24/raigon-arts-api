using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class WhatsAppWebhookPayload
{
    [JsonPropertyName("object")]
    public string Object { get; set; } = string.Empty;

    [JsonPropertyName("entry")]
    public List<WhatsAppWebhookEntry>? Entry { get; set; }
}

public class WhatsAppWebhookEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("changes")]
    public List<WhatsAppWebhookChange>? Changes { get; set; }
}

public class WhatsAppWebhookChange
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public WhatsAppWebhookValue? Value { get; set; }
}

public class WhatsAppWebhookValue
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = "whatsapp";

    [JsonPropertyName("metadata")]
    public WhatsAppWebhookMetadata? Metadata { get; set; }

    [JsonPropertyName("contacts")]
    public List<WhatsAppWebhookContact>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WhatsAppWebhookMessage>? Messages { get; set; }

    [JsonPropertyName("statuses")]
    public List<WhatsAppWebhookStatus>? Statuses { get; set; }
}

public class WhatsAppWebhookMetadata
{
    [JsonPropertyName("display_phone_number")]
    public string? DisplayPhoneNumber { get; set; }

    [JsonPropertyName("phone_number_id")]
    public string? PhoneNumberId { get; set; }
}

public class WhatsAppWebhookContact
{
    [JsonPropertyName("profile")]
    public WhatsAppWebhookProfile? Profile { get; set; }

    [JsonPropertyName("wa_id")]
    public string WaId { get; set; } = string.Empty;
}

public class WhatsAppWebhookProfile
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class WhatsAppWebhookMessage
{
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public WhatsAppWebhookText? Text { get; set; }

    [JsonPropertyName("image")]
    public WhatsAppWebhookMedia? Image { get; set; }

    [JsonPropertyName("document")]
    public WhatsAppWebhookMedia? Document { get; set; }
}

public class WhatsAppWebhookText
{
    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class WhatsAppWebhookMedia
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("mime_type")]
    public string? MimeType { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
}

public class WhatsAppWebhookStatus
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // "sent", "delivered", "read", "failed"

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("recipient_id")]
    public string RecipientId { get; set; } = string.Empty;
}
