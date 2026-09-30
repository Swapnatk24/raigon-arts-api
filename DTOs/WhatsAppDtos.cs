using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class WhatsAppMessageRequest
{
    [JsonPropertyName("messaging_product")]
    public string MessagingProduct { get; set; } = "whatsapp";

    [JsonPropertyName("recipient_type")]
    public string RecipientType { get; set; } = "individual";

    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = "template";

    [JsonPropertyName("template")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppTemplatePayload? Template { get; set; }

    [JsonPropertyName("text")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WhatsAppTextPayload? Text { get; set; }
}

public class WhatsAppTemplatePayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "raigon_order_confirmation";

    [JsonPropertyName("language")]
    public WhatsAppTemplateLanguage Language { get; set; } = new();

    [JsonPropertyName("components")]
    public List<WhatsAppTemplateComponent> Components { get; set; } = new();
}

public class WhatsAppTemplateLanguage
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = "en_US";
}

public class WhatsAppTemplateComponent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "body";

    [JsonPropertyName("parameters")]
    public List<WhatsAppParameter> Parameters { get; set; } = new();
}

public class WhatsAppParameter
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

public class WhatsAppTextPayload
{
    [JsonPropertyName("preview_url")]
    public bool PreviewUrl { get; set; } = false;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public class WhatsAppApiResponse
{
    [JsonPropertyName("messaging_product")]
    public string? MessagingProduct { get; set; }

    [JsonPropertyName("contacts")]
    public List<WhatsAppContactItem>? Contacts { get; set; }

    [JsonPropertyName("messages")]
    public List<WhatsAppMessageItem>? Messages { get; set; }

    [JsonPropertyName("error")]
    public WhatsAppApiError? Error { get; set; }
}

public class WhatsAppContactItem
{
    [JsonPropertyName("input")]
    public string? Input { get; set; }

    [JsonPropertyName("wa_id")]
    public string? WaId { get; set; }
}

public class WhatsAppMessageItem
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("message_status")]
    public string? MessageStatus { get; set; }
}

public class WhatsAppApiError
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("error_subcode")]
    public int? ErrorSubcode { get; set; }

    [JsonPropertyName("fbtrace_id")]
    public string? FbTraceId { get; set; }
}

public class SendWhatsAppTestRequest
{
    [JsonPropertyName("toPhone")]
    public string ToPhone { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

