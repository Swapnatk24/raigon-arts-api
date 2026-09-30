using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

// --- 1. Products & Sizes DTOs ---
public class AiProductItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public decimal Width { get; set; }

    [JsonPropertyName("height")]
    public decimal Height { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "inch";

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; } = true;

    [JsonPropertyName("estimatedStartingPrice")]
    public decimal EstimatedStartingPrice { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "₹";

    [JsonPropertyName("leadTime")]
    public string LeadTime { get; set; } = "3-5 working days";
}

public class AiProductsResponseDto
{
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = new();

    [JsonPropertyName("products")]
    public List<AiProductItemDto> Products { get; set; } = new();
}

// --- 2. Materials DTOs ---
public class AiMaterialItemDto
{
    [JsonPropertyName("frameType")]
    public string FrameType { get; set; } = string.Empty;

    [JsonPropertyName("materialName")]
    public string MaterialName { get; set; } = string.Empty;

    [JsonPropertyName("availableColors")]
    public List<string> AvailableColors { get; set; } = new();

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("bestSuitedFor")]
    public string BestSuitedFor { get; set; } = string.Empty;

    [JsonPropertyName("priceTier")]
    public string PriceTier { get; set; } = "Standard"; // Economy, Standard, Premium, Luxury

    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; } = true;
}

public class AiMaterialsResponseDto
{
    [JsonPropertyName("materials")]
    public List<AiMaterialItemDto> Materials { get; set; } = new();

    [JsonPropertyName("supportedGlassTypes")]
    public List<string> SupportedGlassTypes { get; set; } = new();

    [JsonPropertyName("supportedMounts")]
    public List<string> SupportedMounts { get; set; } = new();
}

// --- 3. Price Calculation DTOs ---
public class AiPriceCalculationRequest
{
    [Required(ErrorMessage = "Width is required.")]
    [Range(1, 200, ErrorMessage = "Width must be between 1 and 200.")]
    [JsonPropertyName("width")]
    public decimal Width { get; set; }

    [Required(ErrorMessage = "Height is required.")]
    [Range(1, 200, ErrorMessage = "Height must be between 1 and 200.")]
    [JsonPropertyName("height")]
    public decimal Height { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "inch";

    [JsonPropertyName("frameType")]
    public string? FrameType { get; set; } = "Wooden Frame";

    [JsonPropertyName("frameMaterial")]
    public string? FrameMaterial { get; set; } = "Teak Wood Moulding";

    [JsonPropertyName("glassType")]
    public string? GlassType { get; set; } = "Clear Float Glass";

    [JsonPropertyName("hasMount")]
    public bool HasMount { get; set; } = false;

    [Range(1, 100, ErrorMessage = "Quantity must be at least 1.")]
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
}

public class AiPriceBreakdownDto
{
    [JsonPropertyName("mouldingCost")]
    public decimal MouldingCost { get; set; }

    [JsonPropertyName("glassCost")]
    public decimal GlassCost { get; set; }

    [JsonPropertyName("mountCost")]
    public decimal MountCost { get; set; }

    [JsonPropertyName("backingAndAssembly")]
    public decimal BackingAndAssembly { get; set; }
}

public class AiPriceCalculationResponseDto
{
    [JsonPropertyName("dimensions")]
    public string Dimensions { get; set; } = string.Empty;

    [JsonPropertyName("frameType")]
    public string FrameType { get; set; } = string.Empty;

    [JsonPropertyName("frameMaterial")]
    public string FrameMaterial { get; set; } = string.Empty;

    [JsonPropertyName("glassType")]
    public string GlassType { get; set; } = string.Empty;

    [JsonPropertyName("hasMount")]
    public bool HasMount { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("taxRatePercent")]
    public decimal TaxRatePercent { get; set; }

    [JsonPropertyName("taxAmount")]
    public decimal TaxAmount { get; set; }

    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "₹";

    [JsonPropertyName("breakdown")]
    public AiPriceBreakdownDto Breakdown { get; set; } = new();

    [JsonPropertyName("calculationNote")]
    public string CalculationNote { get; set; } = string.Empty;
}

// --- 4. Availability Check DTOs ---
public class AiCheckAvailabilityRequest
{
    [Required(ErrorMessage = "Size or dimensions are required (e.g. '12x18', 'FS-05', '8x10 inch').")]
    [JsonPropertyName("sizeOrDimensions")]
    public string SizeOrDimensions { get; set; } = string.Empty;

    [JsonPropertyName("frameMaterial")]
    public string? FrameMaterial { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
}

public class AiAvailabilityResponseDto
{
    [JsonPropertyName("isAvailable")]
    public bool IsAvailable { get; set; }

    [JsonPropertyName("matchedSize")]
    public string MatchedSize { get; set; } = string.Empty;

    [JsonPropertyName("dimensions")]
    public string Dimensions { get; set; } = string.Empty;

    [JsonPropertyName("material")]
    public string Material { get; set; } = string.Empty;

    [JsonPropertyName("stockStatus")]
    public string StockStatus { get; set; } = "In Stock"; // In Stock, Made to Order, Unavailable

    [JsonPropertyName("estimatedLeadDays")]
    public int EstimatedLeadDays { get; set; }

    [JsonPropertyName("estimatedReadyDate")]
    public string EstimatedReadyDate { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// --- 5. Order Creation DTOs ---
public class AiCreateOrderRequest
{
    [Required(ErrorMessage = "Customer name is required.")]
    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer phone is required.")]
    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("customerAltPhone")]
    public string? CustomerAltPhone { get; set; }

    [JsonPropertyName("customerCity")]
    public string CustomerCity { get; set; } = string.Empty;

    [JsonPropertyName("customerAddress")]
    public string CustomerAddress { get; set; } = string.Empty;

    [JsonPropertyName("customerPincode")]
    public string CustomerPincode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Frame size is required (e.g. '12 × 18 inch').")]
    [JsonPropertyName("frameSize")]
    public string FrameSize { get; set; } = "12 × 18 inch";

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "inch";

    [JsonPropertyName("frameType")]
    public string FrameType { get; set; } = "Wooden Frame";

    [JsonPropertyName("frameMaterial")]
    public string FrameMaterial { get; set; } = "Teak Wood Moulding";

    [JsonPropertyName("frameColor")]
    public string FrameColor { get; set; } = "Walnut Brown";

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
}

public class AiCreateOrderResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("frameDetails")]
    public string FrameDetails { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; }

    [JsonPropertyName("balanceDue")]
    public decimal BalanceDue { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("deliveryDate")]
    public string DeliveryDate { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "₹";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

// --- 6. Order Status DTOs ---
public class AiOrderSummaryDto
{
    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("orderDate")]
    public string OrderDate { get; set; } = string.Empty;

    [JsonPropertyName("deliveryDate")]
    public string? DeliveryDate { get; set; }

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; }

    [JsonPropertyName("balanceDue")]
    public decimal BalanceDue { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "₹";

    [JsonPropertyName("frameSummary")]
    public string FrameSummary { get; set; } = string.Empty;

    [JsonPropertyName("statusDescription")]
    public string StatusDescription { get; set; } = string.Empty;
}

public class AiOrderStatusResponseDto
{
    [JsonPropertyName("found")]
    public bool Found { get; set; }

    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("orders")]
    public List<AiOrderSummaryDto> Orders { get; set; } = new();
}

// --- 7. Transfer to Human DTOs ---
public class AiTransferToHumanRequest
{
    [Required(ErrorMessage = "Customer name is required.")]
    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer phone number is required.")]
    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reason for escalation is required.")]
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    [JsonPropertyName("channel")]
    public string Channel { get; set; } = "WhatsApp"; // WhatsApp, Instagram, Web

    [JsonPropertyName("chatSummary")]
    public string? ChatSummary { get; set; }
}

public class AiTransferToHumanResponseDto
{
    [JsonPropertyName("transferred")]
    public bool Transferred { get; set; }

    [JsonPropertyName("notificationId")]
    public string NotificationId { get; set; } = string.Empty;

    [JsonPropertyName("escalationTime")]
    public string EscalationTime { get; set; } = string.Empty;

    [JsonPropertyName("workshopContact")]
    public string WorkshopContact { get; set; } = string.Empty;

    [JsonPropertyName("workshopAddress")]
    public string WorkshopAddress { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
