using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/ai")]
public class AiToolsController : ControllerBase
{
    private readonly IAiToolsService _aiToolsService;

    public AiToolsController(IAiToolsService aiToolsService)
    {
        _aiToolsService = aiToolsService;
    }

    /// <summary>
    /// AI Function: get_products - Retrieves framing product categories and catalog.
    /// </summary>
    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<AiProductsResponseDto>>> GetProducts(
        [FromQuery] string? category,
        [FromQuery] string? search)
    {
        var result = await _aiToolsService.GetProductsAsync(category, search);
        return Ok(ApiResponse<AiProductsResponseDto>.Ok(result, "Products retrieved successfully."));
    }

    /// <summary>
    /// AI Function: get_sizes - Retrieves all supported frame dimensions and aspect ratios.
    /// </summary>
    [HttpGet("sizes")]
    public async Task<ActionResult<ApiResponse<AiProductsResponseDto>>> GetSizes([FromQuery] string? search)
    {
        var result = await _aiToolsService.GetSizesAsync(search);
        return Ok(ApiResponse<AiProductsResponseDto>.Ok(result, "Frame sizes retrieved successfully."));
    }

    /// <summary>
    /// AI Function: get_materials - Retrieves frame types, moulding materials, finishes, and glass options.
    /// </summary>
    [HttpGet("materials")]
    public async Task<ActionResult<ApiResponse<AiMaterialsResponseDto>>> GetMaterials([FromQuery] string? frameType)
    {
        var result = await _aiToolsService.GetMaterialsAsync(frameType);
        return Ok(ApiResponse<AiMaterialsResponseDto>.Ok(result, "Frame materials retrieved successfully."));
    }

    /// <summary>
    /// AI Function: get_price - Deterministically computes custom framing quote based on dimensions, materials, and DB tax rate.
    /// </summary>
    [HttpPost("price")]
    public async Task<ActionResult<ApiResponse<AiPriceCalculationResponseDto>>> GetPrice([FromBody] AiPriceCalculationRequest request)
    {
        var result = await _aiToolsService.GetPriceAsync(request);
        return Ok(ApiResponse<AiPriceCalculationResponseDto>.Ok(result, "Price calculated successfully."));
    }

    /// <summary>
    /// AI Function: check_availability - Checks frame size status in DB and calculates workshop lead time.
    /// </summary>
    [HttpPost("availability")]
    public async Task<ActionResult<ApiResponse<AiAvailabilityResponseDto>>> CheckAvailability([FromBody] AiCheckAvailabilityRequest request)
    {
        var result = await _aiToolsService.CheckAvailabilityAsync(request);
        return Ok(ApiResponse<AiAvailabilityResponseDto>.Ok(result, "Availability checked successfully."));
    }

    /// <summary>
    /// AI Function: create_order - Persists customer framing order into PostgreSQL and triggers WhatsApp confirmation.
    /// </summary>
    [HttpPost("orders/create")]
    public async Task<ActionResult<ApiResponse<AiCreateOrderResponseDto>>> CreateOrder([FromBody] AiCreateOrderRequest request)
    {
        var result = await _aiToolsService.CreateOrderAsync(request);
        return StatusCode(201, ApiResponse<AiCreateOrderResponseDto>.Ok(result, result.Message, 201));
    }

    /// <summary>
    /// AI Function: get_order_status - Retrieves live order details by Order Number or Phone Number.
    /// </summary>
    [HttpGet("orders/status")]
    public async Task<ActionResult<ApiResponse<AiOrderStatusResponseDto>>> GetOrderStatus([FromQuery] string query)
    {
        var result = await _aiToolsService.GetOrderStatusAsync(query);
        return Ok(ApiResponse<AiOrderStatusResponseDto>.Ok(result, result.Message));
    }

    /// <summary>
    /// AI Function: transfer_to_human - Creates escalation alert in PostgreSQL and prepares human staff takeover.
    /// </summary>
    [HttpPost("support/transfer")]
    public async Task<ActionResult<ApiResponse<AiTransferToHumanResponseDto>>> TransferToHuman([FromBody] AiTransferToHumanRequest request)
    {
        var result = await _aiToolsService.TransferToHumanAsync(request);
        return Ok(ApiResponse<AiTransferToHumanResponseDto>.Ok(result, result.Message));
    }
}
