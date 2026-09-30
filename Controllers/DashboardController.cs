using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly RaigonDbContext _context;

    public DashboardController(RaigonDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsResponseData>>> GetStats()
    {
        var orders = await _context.Orders.ToListAsync();
        var customersCount = await _context.Customers.CountAsync();
        var photosCount = await _context.OrderPhotos.SumAsync(p => p.Quantity > 0 ? p.Quantity : 1);

        var totalOrders = orders.Count;
        var inProgress = orders.Count(o => o.OrderStatus.Equals("In Progress", StringComparison.OrdinalIgnoreCase));
        var completed = orders.Count(o => o.OrderStatus.Equals("Completed", StringComparison.OrdinalIgnoreCase));
        var pending = orders.Count(o => o.OrderStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase));
        var totalRevenue = orders.Sum(o => o.TotalAmount);

        var data = new DashboardStatsResponseData
        {
            TotalOrdersCount = totalOrders,
            TotalOrdersGrowth = "+12%",
            InProgressCount = inProgress,
            InProgressStatus = "Active in workshop",
            CompletedOrdersCount = completed,
            CompletedStatus = "Ready for delivery",
            PendingOrdersCount = pending,
            PendingStatus = "Urgent action needed",
            TotalRevenue = totalRevenue,
            RevenueGrowth = "+28%",
            TotalCustomersCount = customersCount,
            TotalFramesInProduction = photosCount
        };

        return Ok(ApiResponse<DashboardStatsResponseData>.Ok(data));
    }

    [HttpGet("recent-orders")]
    public async Task<ActionResult<ApiResponse<List<RecentOrderDto>>>> GetRecentOrders([FromQuery] int limit = 10)
    {
        if (limit < 1) limit = 10;

        var recentOrders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Photos)
            .OrderByDescending(o => o.OrderDate)
            .Take(limit)
            .Select(o => new RecentOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer != null ? o.Customer.Name : "Walk-in Customer",
                CustomerPhone = o.Customer != null ? o.Customer.Phone : "",
                CustomerCity = o.Customer != null ? o.Customer.City : "",
                FrameSize = o.Photos.Any() ? o.Photos.First().FrameSize : "12 × 18 inch",
                FrameType = o.Photos.Any() ? o.Photos.First().FrameType : "Wooden Frame",
                Quantity = o.Photos.Any() ? o.Photos.First().Quantity : 1,
                TotalAmount = o.TotalAmount,
                AdvancePaid = o.AdvancePaid,
                BalanceAmount = o.BalanceAmount,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                DeliveryDate = o.DeliveryDate.HasValue ? o.DeliveryDate.Value.ToString("yyyy-MM-dd") : ""
            })
            .ToListAsync();

        return Ok(ApiResponse<List<RecentOrderDto>>.Ok(recentOrders));
    }
}
