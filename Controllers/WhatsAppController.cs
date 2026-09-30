using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/whatsapp")]
[Route("api/v1/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        IWhatsAppService whatsAppService,
        ILogger<WhatsAppController> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    /// <summary>
    /// Sends an order confirmation message to the customer via Meta WhatsApp Cloud API.
    /// Endpoint: POST /api/whatsapp/send-order-confirmation (also accessible at POST /api/v1/whatsapp/send-order-confirmation)
    /// </summary>
    /// <param name="request">Existing CreateOrderRequest containing customer and order details.</param>
    /// <returns>Standard ApiResponse indicating success or failure status.</returns>
    [HttpPost("send-order-confirmation")]
    public async Task<ActionResult<ApiResponse>> SendOrderConfirmation([FromBody] CreateOrderRequest request)
    {
        if (request == null || request.Customer == null || request.Order == null)
        {
            return BadRequest(ApiErrorResponse.Create(
                400,
                "BAD_REQUEST",
                "Customer and order information are required."
            ));
        }

        var customer = new Customer
        {
            Id = request.Customer.Id ?? string.Empty,
            Name = request.Customer.Name?.Trim() ?? string.Empty,
            Phone = request.Customer.Phone?.Trim() ?? string.Empty,
            AltPhone = request.Customer.AltPhone?.Trim(),
            City = request.Customer.City?.Trim() ?? string.Empty,
            Address = request.Customer.Address?.Trim() ?? string.Empty,
            Pincode = request.Customer.Pincode?.Trim() ?? string.Empty
        };

        var orderDate = DateTime.TryParse(request.Order.OrderDate, out var od)
            ? od.ToUniversalTime()
            : DateTime.UtcNow;

        DateTime? deliveryDate = DateTime.TryParse(request.Order.DeliveryDate, out var dd)
            ? dd.ToUniversalTime()
            : null;

        var commonSpecsJson = request.Order.CommonSpecs != null
            ? JsonSerializer.Serialize(request.Order.CommonSpecs)
            : null;

        var balance = request.Order.TotalAmount - request.Order.AdvancePaid;

        var order = new Order
        {
            OrderNumber = "N/A",
            CustomerId = customer.Id,
            OrderDate = orderDate,
            DeliveryDate = deliveryDate,
            TotalAmount = request.Order.TotalAmount,
            AdvancePaid = request.Order.AdvancePaid,
            BalanceAmount = balance,
            PaymentStatus = string.IsNullOrWhiteSpace(request.Order.PaymentStatus) ? "Unpaid" : request.Order.PaymentStatus,
            OrderStatus = string.IsNullOrWhiteSpace(request.Order.OrderStatus) ? "In Progress" : request.Order.OrderStatus,
            ConfigMode = string.IsNullOrWhiteSpace(request.Order.ConfigMode) ? "same" : request.Order.ConfigMode,
            CommonSpecsJson = commonSpecsJson
        };

        var photos = new List<OrderPhoto>();
        if (request.Order.Photos != null && request.Order.Photos.Count > 0)
        {
            foreach (var p in request.Order.Photos)
            {
                if (string.IsNullOrWhiteSpace(p.PhotoName) && string.IsNullOrWhiteSpace(p.PhotoUrl))
                    continue;

                photos.Add(new OrderPhoto
                {
                    PhotoName = p.PhotoName ?? string.Empty,
                    PhotoUrl = p.PhotoUrl ?? string.Empty,
                    FrameSize = p.FrameSize ?? (request.Order.CommonSpecs?.FrameSize ?? "12 × 18 inch"),
                    Unit = p.Unit ?? (request.Order.CommonSpecs?.Unit ?? "inch"),
                    FrameType = p.FrameType ?? (request.Order.CommonSpecs?.FrameType ?? "Wooden Frame"),
                    FrameMaterial = p.FrameMaterial ?? (request.Order.CommonSpecs?.FrameMaterial ?? "Teak Wood Moulding"),
                    FrameColor = p.FrameColor ?? (request.Order.CommonSpecs?.FrameColor ?? "Walnut Brown"),
                    Orientation = p.Orientation ?? (request.Order.CommonSpecs?.Orientation ?? "Landscape"),
                    Quantity = p.Quantity > 0 ? p.Quantity : (request.Order.CommonSpecs?.Quantity ?? 1)
                });
            }
        }

        var isSent = await _whatsAppService.SendOrderConfirmationAsync(order, customer, photos);

        if (isSent)
        {
            return Ok(ApiResponse.Ok("WhatsApp order confirmation sent successfully."));
        }

        return StatusCode(502, ApiErrorResponse.Create(
            502,
            "WHATSAPP_SEND_FAILED",
            "Failed to send WhatsApp order confirmation via Meta Cloud API. Please verify the customer's phone number and WhatsApp API configuration."
        ));
    }

    /// <summary>
    /// Development test endpoint to verify WhatsApp message content in the .NET console.
    /// Endpoint: POST /api/whatsapp/send-test (also accessible at POST /api/v1/whatsapp/send-test)
    /// </summary>
    /// <param name="request">Request containing target phone number and test message text.</param>
    /// <returns>Standard ApiResponse indicating test status.</returns>
    [HttpPost("send-test")]
    public ActionResult<ApiResponse> SendTestMessage([FromBody] SendWhatsAppTestRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ToPhone) || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(ApiErrorResponse.Create(
                400,
                "BAD_REQUEST",
                "Both 'toPhone' and 'message' are required."
            ));
        }

        // Development-only console test mode: Display message in .NET terminal without calling Meta API
        Console.WriteLine("========== WHATSAPP TEST ==========");
        Console.WriteLine($"To: {request.ToPhone}");
        Console.WriteLine($"Message: {request.Message}");
        Console.WriteLine("===================================");

        _logger.LogInformation("WhatsApp Console Test: To={ToPhone}, Message={Message}", request.ToPhone, request.Message);

        return Ok(ApiResponse.Ok("WhatsApp test message verified and printed to console successfully."));
    }
}


