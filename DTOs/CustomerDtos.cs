using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class CustomerItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

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

    [JsonPropertyName("createdAt")]
    public string CreatedAt { get; set; } = string.Empty;

    [JsonPropertyName("totalOrdersCount")]
    public int TotalOrdersCount { get; set; }

    [JsonPropertyName("totalSpent")]
    public decimal TotalSpent { get; set; }
}

public class CustomerListResponseData
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("customers")]
    public List<CustomerItemDto> Customers { get; set; } = new();
}

public class CreateCustomerRequest
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    [RegularExpression(@"^(\+91[\-\s]?)?[0-9]{10}$", ErrorMessage = "Phone number must be a valid 10-digit Indian phone number.")]
    public string Phone { get; set; } = string.Empty;

    public string? AltPhone { get; set; }
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
}

public class UpdateCustomerRequest
{
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 150 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    public string Phone { get; set; } = string.Empty;

    public string? AltPhone { get; set; }
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
}

public class CustomerOrderPhotoDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; } = string.Empty;

    [JsonPropertyName("photoName")]
    public string PhotoName { get; set; } = string.Empty;

    [JsonPropertyName("frameSize")]
    public string FrameSize { get; set; } = string.Empty;

    [JsonPropertyName("frameType")]
    public string FrameType { get; set; } = string.Empty;
}

public class CustomerOrderDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("orderNumber")]
    public string OrderNumber { get; set; } = string.Empty;

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

    [JsonPropertyName("photos")]
    public List<CustomerOrderPhotoDto> Photos { get; set; } = new();
}

public class CustomerDetailResponseData
{
    [JsonPropertyName("customer")]
    public CustomerItemDto Customer { get; set; } = new();

    [JsonPropertyName("orders")]
    public List<CustomerOrderDto> Orders { get; set; } = new();
}
