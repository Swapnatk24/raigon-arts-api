using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public interface ICustomerService
{
    Task<CustomerListResponseData> GetCustomersAsync(string? search, int page = 1, int limit = 50);
    Task<CustomerItemDto> CreateCustomerAsync(CreateCustomerRequest request);
    Task<CustomerDetailResponseData> GetCustomerByIdAsync(string id);
    Task<CustomerItemDto> UpdateCustomerAsync(string id, UpdateCustomerRequest request);
    Task DeleteCustomerAsync(string id);
}

public class CustomerService : ICustomerService
{
    private readonly RaigonDbContext _context;

    public CustomerService(RaigonDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerListResponseData> GetCustomersAsync(string? search, int page = 1, int limit = 50)
    {
        var query = _context.Customers
            .Include(c => c.Orders)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(s) ||
                c.Phone.ToLower().Contains(s) ||
                c.Id.ToLower().Contains(s) ||
                c.City.ToLower().Contains(s) ||
                c.Address.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(c => new CustomerItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Phone = c.Phone,
                AltPhone = c.AltPhone,
                City = c.City,
                Address = c.Address,
                Pincode = c.Pincode,
                CreatedAt = c.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                TotalOrdersCount = c.Orders.Count,
                TotalSpent = c.Orders.Sum(o => o.TotalAmount)
            })
            .ToListAsync();

        return new CustomerListResponseData
        {
            Total = total,
            Page = page,
            Limit = limit,
            Customers = customers
        };
    }

    public async Task<CustomerItemDto> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var cleanPhone = request.Phone.Trim();

        // Check if customer already exists with this phone
        var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == cleanPhone);
        if (existing != null)
        {
            throw new ApiException(409, "CONFLICT", $"A customer with phone number {cleanPhone} already exists.");
        }

        var newId = await GenerateNextCustomerIdAsync();

        var customer = new Customer
        {
            Id = newId,
            Name = request.Name.Trim(),
            Phone = cleanPhone,
            AltPhone = request.AltPhone?.Trim(),
            City = request.City.Trim(),
            Address = request.Address.Trim(),
            Pincode = request.Pincode.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return new CustomerItemDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            AltPhone = customer.AltPhone,
            City = customer.City,
            Address = customer.Address,
            Pincode = customer.Pincode,
            CreatedAt = customer.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            TotalOrdersCount = 0,
            TotalSpent = 0
        };
    }

    public async Task<CustomerDetailResponseData> GetCustomerByIdAsync(string id)
    {
        var cleanId = (id ?? "").Trim();
        var customer = await _context.Customers
            .Include(c => c.Orders)
                .ThenInclude(o => o.Photos)
            .FirstOrDefaultAsync(c => c.Id == cleanId || c.Orders.Any(o => o.OrderNumber == cleanId || o.Id == cleanId));

        if (customer == null)
        {
            throw new ApiException(404, "NOT_FOUND", $"Customer with ID '{id}' was not found.");
        }

        var customerDto = new CustomerItemDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            AltPhone = customer.AltPhone,
            City = customer.City,
            Address = customer.Address,
            Pincode = customer.Pincode,
            CreatedAt = customer.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            TotalOrdersCount = customer.Orders.Count,
            TotalSpent = customer.Orders.Sum(o => o.TotalAmount)
        };

        var ordersDto = customer.Orders
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new CustomerOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                DeliveryDate = o.DeliveryDate?.ToString("yyyy-MM-dd"),
                TotalAmount = o.TotalAmount,
                AdvancePaid = o.AdvancePaid,
                BalanceAmount = o.BalanceAmount,
                PaymentStatus = o.PaymentStatus,
                OrderStatus = o.OrderStatus,
                Photos = ExpandCustomerOrderPhotos(o.Photos)
            })
            .ToList();

        return new CustomerDetailResponseData
        {
            Customer = customerDto,
            Orders = ordersDto
        };
    }

    private static List<CustomerOrderPhotoDto> ExpandCustomerOrderPhotos(IEnumerable<OrderPhoto> photos)
    {
        var result = new List<CustomerOrderPhotoDto>();
        if (photos == null) return result;

        foreach (var p in photos)
        {
            if (string.IsNullOrWhiteSpace(p.PhotoName) && string.IsNullOrWhiteSpace(p.PhotoUrl))
                continue;

            var names = (p.PhotoName ?? "").Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries);
            var urls = (p.PhotoUrl ?? "").Split(new[] { " || ", "||", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            var count = Math.Max(names.Length, urls.Length);
            if (count <= 1)
            {
                result.Add(new CustomerOrderPhotoDto
                {
                    Id = p.Id,
                    PhotoUrl = !string.IsNullOrWhiteSpace(p.PhotoUrl) ? p.PhotoUrl : "assets/images/sample_frame_1.jpg",
                    PhotoName = !string.IsNullOrWhiteSpace(p.PhotoName) ? p.PhotoName : "photo.jpg",
                    FrameSize = p.FrameSize,
                    FrameType = p.FrameType
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var name = i < names.Length ? names[i].Trim() : (names.Length > 0 ? names[0].Trim() : $"photo_{i + 1}.jpg");
                    var url = i < urls.Length ? urls[i].Trim() : (urls.Length > 0 ? urls[0].Trim() : "assets/images/sample_frame_1.jpg");

                    result.Add(new CustomerOrderPhotoDto
                    {
                        Id = $"{p.Id}_{i + 1}",
                        PhotoUrl = url,
                        PhotoName = name,
                        FrameSize = p.FrameSize,
                        FrameType = p.FrameType
                    });
                }
            }
        }

        return result;
    }

    public async Task<CustomerItemDto> UpdateCustomerAsync(string id, UpdateCustomerRequest request)
    {
        var cleanId = (id ?? "").Trim();
        var cleanPhone = request.Phone.Trim();

        var customer = await _context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == cleanId || c.Orders.Any(o => o.OrderNumber == cleanId || o.Id == cleanId) || c.Phone == cleanPhone);

        if (customer == null)
        {
            var custIdToUse = !string.IsNullOrWhiteSpace(cleanId) ? cleanId : await GenerateNextCustomerIdAsync();
            customer = new Customer
            {
                Id = custIdToUse,
                Name = request.Name.Trim(),
                Phone = cleanPhone,
                AltPhone = request.AltPhone?.Trim(),
                City = request.City.Trim(),
                Address = request.Address.Trim(),
                Pincode = request.Pincode.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
        }
        else
        {
            customer.Name = request.Name.Trim();
            customer.Phone = cleanPhone;
            customer.AltPhone = request.AltPhone?.Trim();
            customer.City = request.City.Trim();
            customer.Address = request.Address.Trim();
            customer.Pincode = request.Pincode.Trim();
        }

        await _context.SaveChangesAsync();

        return new CustomerItemDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Phone = customer.Phone,
            AltPhone = customer.AltPhone,
            City = customer.City,
            Address = customer.Address,
            Pincode = customer.Pincode,
            CreatedAt = customer.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            TotalOrdersCount = customer.Orders.Count,
            TotalSpent = customer.Orders.Sum(o => o.TotalAmount)
        };
    }

    public async Task DeleteCustomerAsync(string id)
    {
        var cleanId = (id ?? "").Trim();
        var customer = await _context.Customers
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == cleanId || c.Orders.Any(o => o.OrderNumber == cleanId || o.Id == cleanId));

        if (customer == null)
        {
            throw new ApiException(404, "NOT_FOUND", $"Customer with ID '{id}' was not found.");
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync();
    }

    private async Task<string> GenerateNextCustomerIdAsync()
    {
        var customerIds = await _context.Customers.Select(c => c.Id).ToListAsync();
        var orderCustomerIds = await _context.Orders.Select(o => o.CustomerId).ToListAsync();
        var orderNumbers = await _context.Orders.Select(o => o.OrderNumber).ToListAsync();
        var allIds = customerIds.Concat(orderCustomerIds).Concat(orderNumbers).Distinct();

        int maxNum = 1000;
        foreach (var id in allIds)
        {
            if (string.IsNullOrWhiteSpace(id)) continue;

            if (id.StartsWith("RA-", StringComparison.OrdinalIgnoreCase))
            {
                var numStr = id.Substring(3);
                if (int.TryParse(numStr, out var num) && num >= 1000 && num < 100000)
                {
                    if (num > maxNum)
                    {
                        maxNum = num;
                    }
                }
            }
        }

        int nextNum = maxNum + 1;
        var nextId = $"RA-{nextNum}";
        while (await _context.Customers.AnyAsync(c => c.Id == nextId))
        {
            nextNum++;
            nextId = $"RA-{nextNum}";
        }

        return nextId;
    }
}
