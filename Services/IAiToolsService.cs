using RaigonArts.Api.DTOs;

namespace RaigonArts.Api.Services;

public interface IAiToolsService
{
    /// <summary>
    /// 1. get_products: Retrieves framing product categories and catalog based on active FrameSizes in DB.
    /// </summary>
    Task<AiProductsResponseDto> GetProductsAsync(string? category = null, string? search = null);

    /// <summary>
    /// 2. get_sizes: Retrieves all supported frame dimensions and aspect ratios from DB.
    /// </summary>
    Task<AiProductsResponseDto> GetSizesAsync(string? search = null);

    /// <summary>
    /// 3. get_materials: Retrieves the actual frame types, moulding materials, finishes, and glass options.
    /// </summary>
    Task<AiMaterialsResponseDto> GetMaterialsAsync(string? frameType = null);

    /// <summary>
    /// 4. get_price: Computes deterministic framing price from dimensions, material, glass type, and DB tax rate.
    /// </summary>
    Task<AiPriceCalculationResponseDto> GetPriceAsync(AiPriceCalculationRequest request);

    /// <summary>
    /// 5. check_availability: Verifies active frame size in DB and computes accurate workshop turnaround.
    /// </summary>
    Task<AiAvailabilityResponseDto> CheckAvailabilityAsync(AiCheckAvailabilityRequest request);

    /// <summary>
    /// 6. create_order: Delegates to existing OrderService to persist order and trigger customer notification.
    /// </summary>
    Task<AiCreateOrderResponseDto> CreateOrderAsync(AiCreateOrderRequest request);

    /// <summary>
    /// 7. get_order_status: Retrieves live order details from PostgreSQL by Order Number or Phone Number.
    /// </summary>
    Task<AiOrderStatusResponseDto> GetOrderStatusAsync(string orderNumberOrPhone);

    /// <summary>
    /// 8. transfer_to_human: Creates an escalation notification in PostgreSQL and prepares human takeover.
    /// </summary>
    Task<AiTransferToHumanResponseDto> TransferToHumanAsync(AiTransferToHumanRequest request);
}
