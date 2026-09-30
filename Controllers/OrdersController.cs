using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<OrderListResponseData>>> GetOrders(
        [FromQuery] string? status,
        [FromQuery] string? paymentStatus,
        [FromQuery] string? search,
        [FromQuery] int? page,
        [FromQuery] int? limit)
    {
        var result = await _orderService.GetOrdersAsync(status, paymentStatus, search, page, limit);
        return Ok(ApiResponse<OrderListResponseData>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CreateOrderResponseData>>> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var result = await _orderService.CreateOrderAsync(request);
        return StatusCode(201, ApiResponse<CreateOrderResponseData>.Ok(
            result,
            $"Customer profile and frame order #{result.OrderNumber} created successfully.",
            201
        ));
    }

    [HttpPatch("{id}/status")]
    public async Task<ActionResult<ApiResponse<UpdateOrderStatusResponseData>>> UpdateOrderStatus(
        string id,
        [FromBody] UpdateOrderStatusRequest request)
    {
        var result = await _orderService.UpdateOrderStatusAsync(id, request);
        return Ok(ApiResponse<UpdateOrderStatusResponseData>.Ok(
            result,
            $"Order status updated to '{result.OrderStatus}'."
        ));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<UpdateOrderResponseData>>> UpdateOrder(
        string id,
        [FromBody] UpdateOrderRequest request)
    {
        var result = await _orderService.UpdateOrderAsync(id, request);
        return Ok(ApiResponse<UpdateOrderResponseData>.Ok(
            result,
            $"Order #{result.OrderNumber} updated successfully."
        ));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteOrder(string id)
    {
        await _orderService.DeleteOrderAsync(id);
        return Ok(ApiResponse.Ok($"Order #{id} and associated attachments removed."));
    }
}
