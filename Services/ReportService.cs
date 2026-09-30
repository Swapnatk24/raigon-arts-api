using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;

namespace RaigonArts.Api.Services;

public interface IReportService
{
    Task<ReportCardsDto> GetReportCardsAsync();
    Task<FinancialReportDto> GetFinancialReportAsync();
    Task<List<ProductionBreakdownItemDto>> GetProductionBreakdownAsync();
    Task<List<FrameSizeSalesBreakdownDto>> GetTopFrameSizesSoldAsync(int? limit = null);
    Task<List<MaterialSalesBreakdownDto>> GetPopularMaterialsAsync(int? limit = null);
    Task<ProductionAnalyticsDto> GetProductionAnalyticsAsync();
    Task<string> GenerateOrdersCsvAsync();
}

public class ReportService : IReportService
{
    private readonly RaigonDbContext _context;

    public ReportService(RaigonDbContext context)
    {
        _context = context;
    }

    public async Task<ReportCardsDto> GetReportCardsAsync()
    {
        var orders = await _context.Orders.ToListAsync();

        var totalOrders = orders.Count;
        var totalRevenue = orders.Sum(o => o.TotalAmount);
        var advanceCollected = orders.Sum(o => o.AdvancePaid);
        var outstandingBalance = orders.Sum(o => o.BalanceAmount);

        var todayUtc = DateTime.UtcNow.Date;
        var todayOrders = orders.Where(o => o.CreatedAt.Date == todayUtc || o.OrderDate.Date == todayUtc).ToList();
        var highestOrderOfDay = todayOrders.Any() ? todayOrders.Max(o => o.TotalAmount) : 0m;

        return new ReportCardsDto
        {
            TotalRevenue = totalRevenue,
            AdvanceCollected = advanceCollected,
            OutstandingBalance = outstandingBalance,
            HighestOrderOfDay = highestOrderOfDay,
            Hod = highestOrderOfDay,
            TotalOrders = totalOrders
        };
    }

    public async Task<FinancialReportDto> GetFinancialReportAsync()
    {
        var orders = await _context.Orders.ToListAsync();

        var totalOrders = orders.Count;
        var totalBilled = orders.Sum(o => o.TotalAmount);
        var totalCollected = orders.Sum(o => o.AdvancePaid);
        var totalOutstanding = orders.Sum(o => o.BalanceAmount);

        var todayUtc = DateTime.UtcNow.Date;
        var todayOrders = orders.Where(o => o.CreatedAt.Date == todayUtc || o.OrderDate.Date == todayUtc).ToList();
        var highestOrderOfDay = todayOrders.Any() ? todayOrders.Max(o => o.TotalAmount) : 0m;

        var paidCount = orders.Count(o => o.PaymentStatus.Equals("Paid", StringComparison.OrdinalIgnoreCase));
        var partialCount = orders.Count(o => o.PaymentStatus.Equals("Partial", StringComparison.OrdinalIgnoreCase));
        var unpaidCount = orders.Count(o => o.PaymentStatus.Equals("Unpaid", StringComparison.OrdinalIgnoreCase));

        var paidPct = totalOrders > 0 ? (int)Math.Round((double)paidCount * 100 / totalOrders) : 0;
        var partialPct = totalOrders > 0 ? (int)Math.Round((double)partialCount * 100 / totalOrders) : 0;
        var unpaidPct = totalOrders > 0 ? (int)Math.Round((double)unpaidCount * 100 / totalOrders) : 0;

        return new FinancialReportDto
        {
            TotalBilled = totalBilled,
            TotalRevenue = totalBilled,
            TotalCollected = totalCollected,
            AdvanceCollected = totalCollected,
            TotalOutstanding = totalOutstanding,
            OutstandingBalance = totalOutstanding,
            HighestOrderOfDay = highestOrderOfDay,
            Hod = highestOrderOfDay,
            TotalOrders = totalOrders,
            SettlementStats = new SettlementStatsDto
            {
                PaidCount = paidCount,
                PaidPercentage = paidPct,
                PartialCount = partialCount,
                PartialPercentage = partialPct,
                UnpaidCount = unpaidCount,
                UnpaidPercentage = unpaidPct
            }
        };
    }

    public async Task<List<ProductionBreakdownItemDto>> GetProductionBreakdownAsync()
    {
        var items = await GetAllOrderItemSpecificationsAsync();
        var totalFrames = items.Sum(i => i.Quantity);

        if (totalFrames == 0)
        {
            return new List<ProductionBreakdownItemDto>
            {
                new() { Type = "Wooden Frame", Count = 0, Percentage = 0 },
                new() { Type = "Premium Frame", Count = 0, Percentage = 0 },
                new() { Type = "Classic Frame", Count = 0, Percentage = 0 },
                new() { Type = "Canvas Float", Count = 0, Percentage = 0 },
                new() { Type = "Box Frame", Count = 0, Percentage = 0 }
            };
        }

        var groups = items
            .GroupBy(i => i.FrameType)
            .Select(g => new ProductionBreakdownItemDto
            {
                Type = g.Key,
                Count = g.Sum(i => i.Quantity),
                Percentage = (int)Math.Round((double)g.Sum(i => i.Quantity) * 100 / totalFrames)
            })
            .OrderByDescending(g => g.Count)
            .ToList();

        return groups;
    }

    public async Task<List<FrameSizeSalesBreakdownDto>> GetTopFrameSizesSoldAsync(int? limit = null)
    {
        var items = await GetAllOrderItemSpecificationsAsync();
        var totalVolume = items.Sum(i => i.Quantity);

        if (totalVolume == 0)
        {
            return new List<FrameSizeSalesBreakdownDto>();
        }

        var query = items
            .GroupBy(i => i.FrameSize)
            .Select(g => new FrameSizeSalesBreakdownDto
            {
                Size = g.Key,
                Count = g.Sum(i => i.Quantity),
                Percentage = (int)Math.Round((double)g.Sum(i => i.Quantity) * 100 / totalVolume)
            })
            .OrderByDescending(x => x.Count)
            .AsEnumerable();

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        return query.ToList();
    }

    public async Task<List<MaterialSalesBreakdownDto>> GetPopularMaterialsAsync(int? limit = null)
    {
        var items = await GetAllOrderItemSpecificationsAsync();
        var totalVolume = items.Sum(i => i.Quantity);

        if (totalVolume == 0)
        {
            return new List<MaterialSalesBreakdownDto>();
        }

        var query = items
            .GroupBy(i => i.FrameMaterial)
            .Select(g => new MaterialSalesBreakdownDto
            {
                Material = g.Key,
                Count = g.Sum(i => i.Quantity),
                Percentage = (int)Math.Round((double)g.Sum(i => i.Quantity) * 100 / totalVolume)
            })
            .OrderByDescending(x => x.Count)
            .AsEnumerable();

        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }

        return query.ToList();
    }

    public async Task<ProductionAnalyticsDto> GetProductionAnalyticsAsync()
    {
        var items = await GetAllOrderItemSpecificationsAsync();
        var totalVolume = items.Sum(i => i.Quantity);

        if (totalVolume == 0)
        {
            return new ProductionAnalyticsDto();
        }

        var topSizes = items
            .GroupBy(i => i.FrameSize)
            .Select(g => new FrameSizeSalesBreakdownDto
            {
                Size = g.Key,
                Count = g.Sum(i => i.Quantity),
                Percentage = (int)Math.Round((double)g.Sum(i => i.Quantity) * 100 / totalVolume)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var popularMaterials = items
            .GroupBy(i => i.FrameMaterial)
            .Select(g => new MaterialSalesBreakdownDto
            {
                Material = g.Key,
                Count = g.Sum(i => i.Quantity),
                Percentage = (int)Math.Round((double)g.Sum(i => i.Quantity) * 100 / totalVolume)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var frameTypes = items
            .GroupBy(i => i.FrameType)
            .Select(g => new ProductionBreakdownItemDto
            {
                Type = g.Key,
                Count = g.Sum(i => i.Quantity),
                Percentage = (int)Math.Round((double)g.Sum(i => i.Quantity) * 100 / totalVolume)
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new ProductionAnalyticsDto
        {
            TopFrameSizes = topSizes,
            PopularMaterials = popularMaterials,
            FrameTypes = frameTypes,
            TotalVolume = totalVolume
        };
    }

    public async Task<string> GenerateOrdersCsvAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Customer)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Order Number,Customer Name,Phone,City,Order Date,Delivery Date,Total Amount,Advance Paid,Balance,Payment Status,Order Status");

        foreach (var o in orders)
        {
            var custName = o.Customer?.Name ?? "Walk-in Customer";
            var phone = o.Customer?.Phone ?? "";
            var city = o.Customer?.City ?? "";
            var orderDate = o.OrderDate.ToString("yyyy-MM-dd");
            var deliveryDate = o.DeliveryDate.HasValue ? o.DeliveryDate.Value.ToString("yyyy-MM-dd") : "";

            sb.AppendLine($"\"{EscapeCsv(o.OrderNumber)}\",\"{EscapeCsv(custName)}\",\"{EscapeCsv(phone)}\",\"{EscapeCsv(city)}\",\"{orderDate}\",\"{deliveryDate}\",{o.TotalAmount},{o.AdvancePaid},{o.BalanceAmount},\"{EscapeCsv(o.PaymentStatus)}\",\"{EscapeCsv(o.OrderStatus)}\"");
        }

        return sb.ToString();
    }

    private async Task<List<OrderItemSpecification>> GetAllOrderItemSpecificationsAsync()
    {
        var orders = await _context.Orders
            .Include(o => o.Photos)
            .AsNoTracking()
            .ToListAsync();

        var items = new List<OrderItemSpecification>();

        foreach (var o in orders)
        {
            if (o.Photos != null && o.Photos.Count > 0)
            {
                foreach (var p in o.Photos)
                {
                    items.Add(new OrderItemSpecification
                    {
                        FrameSize = NormalizeFrameSize(p.FrameSize),
                        FrameMaterial = NormalizeMaterial(p.FrameMaterial),
                        FrameType = NormalizeFrameType(p.FrameType),
                        Quantity = p.Quantity > 0 ? p.Quantity : 1
                    });
                }
            }
            else if (!string.IsNullOrWhiteSpace(o.CommonSpecsJson))
            {
                try
                {
                    var specs = JsonSerializer.Deserialize<CommonSpecsDto>(o.CommonSpecsJson);
                    if (specs != null)
                    {
                        items.Add(new OrderItemSpecification
                        {
                            FrameSize = NormalizeFrameSize(specs.FrameSize),
                            FrameMaterial = NormalizeMaterial(specs.FrameMaterial),
                            FrameType = NormalizeFrameType(specs.FrameType),
                            Quantity = specs.Quantity > 0 ? specs.Quantity : 1
                        });
                    }
                }
                catch
                {
                    // Ignore malformed json
                }
            }
        }

        return items;
    }

    private static string NormalizeFrameSize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Custom Sizes";
        var s = raw.Replace("Ã—", "×")
                   .Replace("Ã—", "×")
                   .Replace("*", "×")
                   .Replace("x", "×", StringComparison.OrdinalIgnoreCase)
                   .Trim();

        if (s.Contains("custom", StringComparison.OrdinalIgnoreCase) || s.Equals("Customize", StringComparison.OrdinalIgnoreCase))
            return "Custom Sizes";

        var match = Regex.Match(s, @"(\d+(?:\.\d+)?)\s*×\s*(\d+(?:\.\d+)?)");
        if (match.Success)
        {
            if (double.TryParse(match.Groups[1].Value, out var w) && double.TryParse(match.Groups[2].Value, out var h))
            {
                var min = Math.Min(w, h);
                var max = Math.Max(w, h);

                if (min == 4 && max == 6) return "4 × 6 inch (Standard Photo)";
                if (min == 5 && max == 7) return "5 × 7 inch (Standard Photo)";
                if (min == 8 && max == 10) return "8 × 10 inch (Medium Portrait)";
                if (min == 8 && max == 12) return "8 × 12 inch (Medium Portrait)";
                if (min == 12 && max == 18) return "12 × 18 inch (Large Gallery)";
                if (min == 16 && max == 20) return "16 × 20 inch (Wholesale Gallery)";
                if (min == 20 && max == 30) return "20 × 30 inch (Exhibition Wall Art)";
                if (min == 16 && max == 24) return "16 × 24 inch (Large Gallery)";
                return $"{min} × {max} inch (Custom)";
            }
        }

        return s;
    }

    private static string NormalizeMaterial(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Standard Moulding";
        var m = raw.Trim();
        if (m.Contains("teak", StringComparison.OrdinalIgnoreCase)) return "Teak Wood Moulding";
        if (m.Contains("filigree", StringComparison.OrdinalIgnoreCase) || m.Contains("gold", StringComparison.OrdinalIgnoreCase)) return "Gold Filigree Resin";
        if (m.Contains("aluminum", StringComparison.OrdinalIgnoreCase) || m.Contains("aluminium", StringComparison.OrdinalIgnoreCase) || m.Contains("matte black", StringComparison.OrdinalIgnoreCase)) return "Matte Black Aluminum";
        if (m.Contains("natural oak", StringComparison.OrdinalIgnoreCase) || m.Contains("float wood", StringComparison.OrdinalIgnoreCase)) return "Natural Oak Float Wood";
        if (m.Contains("oak", StringComparison.OrdinalIgnoreCase)) return "Oak Wood";
        if (m.Contains("rose wood", StringComparison.OrdinalIgnoreCase)) return "Rose Wood Moulding";
        if (m.Contains("solid wood", StringComparison.OrdinalIgnoreCase)) return "Solid Wood";
        if (m.Contains("synthetic", StringComparison.OrdinalIgnoreCase)) return "Synthetic Molded";
        if (m.Contains("metallic", StringComparison.OrdinalIgnoreCase)) return "Metallic";
        if (m.Contains("glass", StringComparison.OrdinalIgnoreCase)) return "Glass Mat";
        return m;
    }

    private static string NormalizeFrameType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Wooden Frame";
        var t = raw.Trim();
        if (t.Contains("wooden", StringComparison.OrdinalIgnoreCase)) return "Wooden Frame";
        if (t.Contains("premium", StringComparison.OrdinalIgnoreCase)) return "Premium Frame";
        if (t.Contains("canvas", StringComparison.OrdinalIgnoreCase)) return "Canvas Float";
        if (t.Contains("box", StringComparison.OrdinalIgnoreCase)) return "Box Frame";
        if (t.Contains("classic", StringComparison.OrdinalIgnoreCase)) return "Classic Frame";
        if (t.Contains("tabletop", StringComparison.OrdinalIgnoreCase)) return "Tabletop Frame";
        return t;
    }

    private static string EscapeCsv(string value)
    {
        return value.Replace("\"", "\"\"");
    }

    private class OrderItemSpecification
    {
        public string FrameSize { get; set; } = string.Empty;
        public string FrameMaterial { get; set; } = string.Empty;
        public string FrameType { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }
}
