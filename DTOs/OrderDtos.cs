using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class CommonSpecsDto
{
    [JsonPropertyName("frameSize")]
    public string? FrameSize { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("customWidth")]
    public decimal? CustomWidth { get; set; }

    [JsonPropertyName("customHeight")]
    public decimal? CustomHeight { get; set; }

    [JsonPropertyName("frameType")]
    public string? FrameType { get; set; }

    [JsonPropertyName("frameMaterial")]
    public string? FrameMaterial { get; set; }

    [JsonPropertyName("frameColor")]
    public string? FrameColor { get; set; }

    [JsonPropertyName("orientation")]
    public string? Orientation { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public class OrderPhotoDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; } = string.Empty;

    [JsonPropertyName("photoName")]
    public string PhotoName { get; set; } = string.Empty;

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
}

public class OrderDetailDto
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

    [JsonPropertyName("customerAltPhone")]
    public string? CustomerAltPhone { get; set; }

    [JsonPropertyName("customerCity")]
    public string CustomerCity { get; set; } = string.Empty;

    [JsonPropertyName("customerAddress")]
    public string? CustomerAddress { get; set; }

    [JsonPropertyName("customerPincode")]
    public string? CustomerPincode { get; set; }

    [JsonPropertyName("orderDate")]
    public string OrderDate { get; set; } = string.Empty;

    [JsonPropertyName("deliveryDate")]
    public string? DeliveryDate { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; }

    [JsonPropertyName("balanceAmount")]
    public decimal BalanceAmount { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("configMode")]
    public string ConfigMode { get; set; } = "same";

    [JsonPropertyName("photos")]
    public List<OrderPhotoDto> Photos { get; set; } = new();

    [JsonPropertyName("commonSpecs")]
    public CommonSpecsDto? CommonSpecs { get; set; }

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;
}

public class OrderListResponseData
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("orders")]
    public List<OrderDetailDto> Orders { get; set; } = new();
}

public class CustomerInOrderRequest
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [Required(ErrorMessage = "Customer name is required.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Customer phone is required.")]
    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("altPhone")]
    public string? AltPhone { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("pincode")]
    public string Pincode { get; set; } = string.Empty;
}

public class OrderInCreateRequest
{
    [JsonPropertyName("configMode")]
    public string ConfigMode { get; set; } = "same";

    [JsonPropertyName("orderDate")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("deliveryDate")]
    public string? DeliveryDate { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = "Partial";

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = "In Progress";

    [JsonPropertyName("commonSpecs")]
    public CommonSpecsDto? CommonSpecs { get; set; }

    [JsonPropertyName("photos")]
    public List<OrderPhotoDto> Photos { get; set; } = new();
}

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Customer details are required.")]
    [JsonPropertyName("customer")]
    public CustomerInOrderRequest Customer { get; set; } = new();

    [Required(ErrorMessage = "Order details are required.")]
    [JsonPropertyName("order")]
    public OrderInCreateRequest Order { get; set; } = new();
}

public class CreateOrderResponseData
{
    [JsonPropertyName("orderId")]
    public string OrderId { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

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

    [JsonPropertyName("deliveryDate")]
    public string? DeliveryDate { get; set; }
}

public class UpdateOrderStatusRequest
{
    [Required(ErrorMessage = "Order status is required.")]
    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

public class UpdateOrderStatusResponseData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("updatedAt")]
    public string UpdatedAt { get; set; } = string.Empty;
}

public class UpdateOrderRequest
{
    [JsonPropertyName("customer")]
    public CustomerInOrderRequest? Customer { get; set; }

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

    [JsonPropertyName("configMode")]
    public string? ConfigMode { get; set; }

    [JsonPropertyName("commonSpecs")]
    public CommonSpecsDto? CommonSpecs { get; set; }

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = "Paid";

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = "Completed";

    [JsonPropertyName("deliveryDate")]
    public string? DeliveryDate { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("photos")]
    public List<OrderPhotoDto>? Photos { get; set; }
}

public class UpdateOrderResponseData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

    [JsonPropertyName("totalAmount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("advancePaid")]
    public decimal AdvancePaid { get; set; }

    [JsonPropertyName("balanceAmount")]
    public decimal BalanceAmount { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string PaymentStatus { get; set; } = string.Empty;

    [JsonPropertyName("orderStatus")]
    public string OrderStatus { get; set; } = string.Empty;

    [JsonPropertyName("photos")]
    public List<OrderPhotoDto> Photos { get; set; } = new();
}
