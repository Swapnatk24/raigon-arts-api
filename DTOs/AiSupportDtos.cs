using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

// --- 1. Chat Message & History Models ---
public class AiChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user"; // "system", "user", "assistant", "tool"

    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; set; }

    [JsonPropertyName("toolCalls")]
    public List<AiToolCallInfo>? ToolCalls { get; set; }

    [JsonPropertyName("rawModelPartsJson")]
    public string? RawModelPartsJson { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static AiChatMessage System(string content) => new() { Role = "system", Content = content };
    public static AiChatMessage User(string content, string? name = null) => new() { Role = "user", Content = content, Name = name };
    public static AiChatMessage Assistant(string? content, List<AiToolCallInfo>? toolCalls = null, string? rawModelPartsJson = null) => new() { Role = "assistant", Content = content, ToolCalls = toolCalls, RawModelPartsJson = rawModelPartsJson };
    public static AiChatMessage Tool(string content, string toolCallId, string? name = null) => new() { Role = "tool", Content = content, ToolCallId = toolCallId, Name = name };
}

public class AiToolCallInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public AiFunctionCallDetail Function { get; set; } = new();
}

public class AiFunctionCallDetail
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("arguments")]
    public string Arguments { get; set; } = "{}";
}

// --- 2. Order Draft Context (Maintained Across Chat Turns) ---
public class AiOrderDraft
{
    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customerPhone")]
    public string? CustomerPhone { get; set; }

    [JsonPropertyName("customerAltPhone")]
    public string? CustomerAltPhone { get; set; }

    [JsonPropertyName("customerCity")]
    public string? CustomerCity { get; set; }

    [JsonPropertyName("customerAddress")]
    public string? CustomerAddress { get; set; }

    [JsonPropertyName("customerPincode")]
    public string? CustomerPincode { get; set; }

    [JsonPropertyName("width")]
    public decimal? Width { get; set; }

    [JsonPropertyName("height")]
    public decimal? Height { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "inch";

    [JsonPropertyName("frameSize")]
    public string? FrameSize { get; set; }

    [JsonPropertyName("frameType")]
    public string FrameType { get; set; } = "Wooden Frame";

    [JsonPropertyName("frameMaterial")]
    public string FrameMaterial { get; set; } = "Teak Wood Moulding";

    [JsonPropertyName("frameColor")]
    public string FrameColor { get; set; } = "Walnut Brown";

    [JsonPropertyName("glassType")]
    public string GlassType { get; set; } = "Clear Float Glass";

    [JsonPropertyName("hasMount")]
    public bool HasMount { get; set; } = false;

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = "Landscape";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; set; }

    [JsonPropertyName("photoName")]
    public string? PhotoName { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; } = 0;

    [JsonPropertyName("deliveryDate")]
    public string? DeliveryDate { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("quotedUnitPrice")]
    public decimal? QuotedUnitPrice { get; set; }

    [JsonPropertyName("quotedTaxAmount")]
    public decimal? QuotedTaxAmount { get; set; }

    [JsonPropertyName("quotedTotalPrice")]
    public decimal? QuotedTotalPrice { get; set; }

    [JsonPropertyName("estimatedLeadTime")]
    public string? EstimatedLeadTime { get; set; }

    [JsonPropertyName("isPriceCalculated")]
    public bool IsPriceCalculated { get; set; } = false;

    [JsonPropertyName("isDetailsConfirmedByCustomer")]
    public bool IsDetailsConfirmedByCustomer { get; set; } = false;

    [JsonPropertyName("isOrderCreated")]
    public bool IsOrderCreated { get; set; } = false;

    [JsonPropertyName("createdOrderNumber")]
    public string? CreatedOrderNumber { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if minimum required fields for order creation are present.
    /// </summary>
    public bool HasMinimumOrderDetails(out List<string> missingFields)
    {
        missingFields = new List<string>();
        if (string.IsNullOrWhiteSpace(CustomerName)) missingFields.Add("Customer Name");
        if (string.IsNullOrWhiteSpace(CustomerPhone)) missingFields.Add("Customer Phone");
        if (string.IsNullOrWhiteSpace(FrameSize) && (!Width.HasValue || !Height.HasValue)) missingFields.Add("Frame Size/Dimensions");
        if (Quantity <= 0) missingFields.Add("Quantity (minimum 1)");

        return missingFields.Count == 0;
    }
}

// --- 3. Chat Session State ---
public class AiChatSession
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "WhatsApp";

    [JsonPropertyName("messages")]
    public List<AiChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("draft")]
    public AiOrderDraft Draft { get; set; } = new();

    [JsonPropertyName("isHumanTakeover")]
    public bool IsHumanTakeover { get; set; } = false;

    [JsonPropertyName("humanTakeoverReason")]
    public string? HumanTakeoverReason { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("lastActivityAt")]
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
}

// --- 4. Chat Request / Response DTOs ---
public class AiChatRequestDto
{
    [Required(ErrorMessage = "Customer phone number is required.")]
    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message text is required.")]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "WhatsApp";

    [JsonPropertyName("photoUrl")]
    public string? PhotoUrl { get; set; }

    [JsonPropertyName("photoName")]
    public string? PhotoName { get; set; }
}

public class AiChatResponseDto
{
    [JsonPropertyName("replyMessage")]
    public string ReplyMessage { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("isHumanTakeover")]
    public bool IsHumanTakeover { get; set; }

    [JsonPropertyName("toolsExecuted")]
    public List<AiToolCallExecutionDto> ToolsExecuted { get; set; } = new();

    [JsonPropertyName("orderDraft")]
    public AiOrderDraft? OrderDraft { get; set; }

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("isProviderConfigured")]
    public bool IsProviderConfigured { get; set; }

    [JsonPropertyName("providerMessage")]
    public string? ProviderMessage { get; set; }
}

public class AiToolCallExecutionDto
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("argumentsJson")]
    public string ArgumentsJson { get; set; } = string.Empty;

    [JsonPropertyName("resultJson")]
    public string ResultJson { get; set; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

public class AiToolCallResultDto
{
    [JsonPropertyName("toolName")]
    public string ToolName { get; set; } = string.Empty;

    [JsonPropertyName("toolCallId")]
    public string ToolCallId { get; set; } = string.Empty;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("resultJson")]
    public string ResultJson { get; set; } = string.Empty;

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    public static AiToolCallResultDto Ok(string toolName, string toolCallId, string resultJson) =>
        new() { ToolName = toolName, ToolCallId = toolCallId, Success = true, ResultJson = resultJson };

    public static AiToolCallResultDto Error(string toolName, string toolCallId, string errorMessage) =>
        new() { ToolName = toolName, ToolCallId = toolCallId, Success = false, ResultJson = "{}", ErrorMessage = errorMessage };
}

// --- 5. Tool Calling Schema Definitions (OpenAI / Gemini format) ---
public class AiToolDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "function";

    [JsonPropertyName("function")]
    public AiToolFunctionDefinition Function { get; set; } = new();
}

public class AiToolFunctionDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public AiToolParametersDefinition Parameters { get; set; } = new();
}

public class AiToolParametersDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";

    [JsonPropertyName("properties")]
    public Dictionary<string, AiToolPropertyDefinition> Properties { get; set; } = new();

    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class AiToolPropertyDefinition
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("enum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Enum { get; set; }
}

public class AiTestMessageRequest
{
    [JsonPropertyName("fromPhone")]
    public string? FromPhone { get; set; }

    [JsonPropertyName("customerPhone")]
    public string? CustomerPhone { get; set; }

    [Required(ErrorMessage = "Message text is required.")]
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string? CustomerName { get; set; }
}

