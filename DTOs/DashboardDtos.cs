using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class DashboardStatsResponseData
{
    [JsonPropertyName("totalOrdersCount")]
    public int TotalOrdersCount { get; set; }

    [JsonPropertyName("totalOrdersGrowth")]
    public string TotalOrdersGrowth { get; set; } = "+12%";

    [JsonPropertyName("inProgressCount")]
    public int InProgressCount { get; set; }

    [JsonPropertyName("inProgressStatus")]
    public string InProgressStatus { get; set; } = "Active in workshop";

    [JsonPropertyName("completedOrdersCount")]
    public int CompletedOrdersCount { get; set; }

    [JsonPropertyName("completedStatus")]
    public string CompletedStatus { get; set; } = "Ready for delivery";

    [JsonPropertyName("pendingOrdersCount")]
    public int PendingOrdersCount { get; set; }

    [JsonPropertyName("pendingStatus")]
    public string PendingStatus { get; set; } = "Urgent action needed";

    [JsonPropertyName("totalRevenue")]
    public decimal TotalRevenue { get; set; }

    [JsonPropertyName("revenueGrowth")]
    public string RevenueGrowth { get; set; } = "+28%";

    [JsonPropertyName("totalCustomersCount")]
    public int TotalCustomersCount { get; set; }

    [JsonPropertyName("totalFramesInProduction")]
    public int TotalFramesInProduction { get; set; }
}

public class RecentOrderDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("customerPhone")]
    public string CustomerPhone { get; set; } = string.Empty;

    [JsonPropertyName("customerCity")]
    public string CustomerCity { get; set; } = string.Empty;

    [JsonPropertyName("frameSize")]
    public string FrameSize { get; set; } = string.Empty;

    [JsonPropertyName("frameType")]
    public string FrameType { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; }

    [JsonPropertyName("balanceAmount")]
    public decimal BalanceAmount { get; set; }

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("orderDate")]
    public string OrderDate { get; set; } = string.Empty;

    [JsonPropertyName("deliveryDate")]
    public string DeliveryDate { get; set; } = string.Empty;
}
