using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public interface IOrderService
{
    Task<OrderListResponseData> GetOrdersAsync(string? status, string? paymentStatus, string? search, int? page, int? limit);
    Task<CreateOrderResponseData> CreateOrderAsync(CreateOrderRequest request);
    Task<UpdateOrderStatusResponseData> UpdateOrderStatusAsync(string id, UpdateOrderStatusRequest request);
    Task<UpdateOrderResponseData> UpdateOrderAsync(string id, UpdateOrderRequest request);
    Task DeleteOrderAsync(string id);
}

public class OrderService : IOrderService
{
    private readonly RaigonDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        RaigonDbContext context,
        IWhatsAppService whatsAppService,
        ILogger<OrderService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<OrderListResponseData> GetOrdersAsync(string? status, string? paymentStatus, string? search, int? page, int? limit)
    {
        var query = _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Photos)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.OrderStatus.ToLower() == status.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(paymentStatus) && !paymentStatus.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.PaymentStatus.ToLower() == paymentStatus.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(o =>
                o.OrderNumber.ToLower().Contains(s) ||
                (o.Customer != null && o.Customer.Name.ToLower().Contains(s)) ||
                (o.Customer != null && o.Customer.Phone.ToLower().Contains(s)));
        }

        var total = await query.CountAsync();

        if (page.HasValue && limit.HasValue && page.Value > 0 && limit.Value > 0)
        {
            query = query.Skip((page.Value - 1) * limit.Value).Take(limit.Value);
        }

        var dbOrders = await query
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .ToListAsync();

        var orders = dbOrders.Select(o => new OrderDetailDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer != null ? o.Customer.Name : "Walk-in Customer",
            CustomerPhone = o.Customer != null ? o.Customer.Phone : "",
            CustomerAltPhone = o.Customer != null ? o.Customer.AltPhone : "",
            CustomerCity = o.Customer != null ? o.Customer.City : "",
            CustomerAddress = o.Customer != null ? o.Customer.Address : "",
            CustomerPincode = o.Customer != null ? o.Customer.Pincode : "",
            OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
            DeliveryDate = o.DeliveryDate.HasValue ? o.DeliveryDate.Value.ToString("yyyy-MM-dd") : null,
            TotalAmount = o.TotalAmount,
            AdvancePaid = o.AdvancePaid,
            BalanceAmount = o.BalanceAmount,
            PaymentStatus = o.PaymentStatus,
            OrderStatus = o.OrderStatus,
            ConfigMode = o.ConfigMode,
            CreatedAt = o.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Photos = ExpandOrderPhotosToDtoList(o.Photos),
            CommonSpecs = !string.IsNullOrEmpty(o.CommonSpecsJson)
                ? JsonSerializer.Deserialize<CommonSpecsDto>(o.CommonSpecsJson, (JsonSerializerOptions?)null)
                : null
        }).ToList();

        return new OrderListResponseData
        {
            Total = total,
            Orders = orders
        };
    }

    public async Task<CreateOrderResponseData> CreateOrderAsync(CreateOrderRequest request)
    {
        // 1. Resolve or create customer
        Customer? customer = null;
        if (!string.IsNullOrWhiteSpace(request.Customer.Id))
        {
            var requestedCustId = request.Customer.Id.Trim();
            customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == requestedCustId);
        }

        if (customer == null && !string.IsNullOrWhiteSpace(request.Customer.Phone))
        {
            var phone = request.Customer.Phone.Trim();
            customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
        }

        if (customer == null)
        {
            var newCustId = !string.IsNullOrWhiteSpace(request.Customer.Id) && !await _context.Customers.AnyAsync(c => c.Id == request.Customer.Id.Trim())
                ? request.Customer.Id.Trim()
                : await GenerateNextCustomerIdAsync();

            customer = new Customer
            {
                Id = newCustId,
                Name = request.Customer.Name.Trim(),
                Phone = request.Customer.Phone.Trim(),
                AltPhone = request.Customer.AltPhone?.Trim(),
                City = request.Customer.City.Trim(),
                Address = request.Customer.Address.Trim(),
                Pincode = request.Customer.Pincode.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(request.Customer.Name)) customer.Name = request.Customer.Name.Trim();
            if (!string.IsNullOrWhiteSpace(request.Customer.City)) customer.City = request.Customer.City.Trim();
            if (!string.IsNullOrWhiteSpace(request.Customer.Address)) customer.Address = request.Customer.Address.Trim();
            if (!string.IsNullOrWhiteSpace(request.Customer.Pincode)) customer.Pincode = request.Customer.Pincode.Trim();
            if (request.Customer.AltPhone != null) customer.AltPhone = request.Customer.AltPhone.Trim();
            await _context.SaveChangesAsync();
        }

        // 2. Generate Order Number
        var orderCount = await _context.Orders.CountAsync();
        var nextOrderNum = 1000 + orderCount + 1;
        var orderNumber = $"RA-{nextOrderNum}";
        while (await _context.Orders.AnyAsync(o => o.OrderNumber == orderNumber))
        {
            nextOrderNum++;
            orderNumber = $"RA-{nextOrderNum}";
        }

        var orderId = $"ord_{nextOrderNum}";
        while (await _context.Orders.AnyAsync(o => o.Id == orderId))
        {
            orderId = $"ord_{Guid.NewGuid().ToString("N")[..6]}";
        }

        var orderDate = DateTime.TryParse(request.Order.OrderDate, out var od) ? od.ToUniversalTime() : DateTime.UtcNow;
        DateTime? deliveryDate = DateTime.TryParse(request.Order.DeliveryDate, out var dd) ? dd.ToUniversalTime() : null;

        var balance = request.Order.TotalAmount - request.Order.AdvancePaid;
        var paymentStatus = request.Order.PaymentStatus;
        if (string.IsNullOrWhiteSpace(paymentStatus))
        {
            if (request.Order.AdvancePaid >= request.Order.TotalAmount && request.Order.TotalAmount > 0)
                paymentStatus = "Paid";
            else if (request.Order.AdvancePaid > 0)
                paymentStatus = "Partial";
            else
                paymentStatus = "Unpaid";
        }

        var commonSpecsJson = request.Order.CommonSpecs != null
            ? JsonSerializer.Serialize(request.Order.CommonSpecs)
            : null;

        var order = new Order
        {
            Id = orderId,
            OrderNumber = orderNumber,
            CustomerId = customer.Id,
            OrderDate = orderDate,
            DeliveryDate = deliveryDate,
            TotalAmount = request.Order.TotalAmount,
            AdvancePaid = request.Order.AdvancePaid,
            BalanceAmount = balance,
            PaymentStatus = paymentStatus,
            OrderStatus = string.IsNullOrWhiteSpace(request.Order.OrderStatus) ? "In Progress" : request.Order.OrderStatus,
            ConfigMode = string.IsNullOrWhiteSpace(request.Order.ConfigMode) ? "same" : request.Order.ConfigMode,
            CommonSpecsJson = commonSpecsJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Orders.Add(order);

        // 3. Attach Order Photos (Single row in OrderPhotos table for all photos)
        if (request.Order.Photos != null && request.Order.Photos.Count > 0)
        {
            var validPhotos = request.Order.Photos
                .Where(p => !string.IsNullOrWhiteSpace(p.PhotoName) || !string.IsNullOrWhiteSpace(p.PhotoUrl))
                .ToList();

            if (validPhotos.Count > 0)
            {
                var namesList = new List<string>();
                var urlsList = new List<string>();
                var pIdx = 1;

                foreach (var p in validPhotos)
                {
                    var name = !string.IsNullOrWhiteSpace(p.PhotoName)
                        ? p.PhotoName.Trim()
                        : (!string.IsNullOrWhiteSpace(p.PhotoUrl) ? Path.GetFileName(p.PhotoUrl) : $"Frame_Photo_{pIdx}.jpg");
                    var url = !string.IsNullOrWhiteSpace(p.PhotoUrl)
                        ? p.PhotoUrl.Trim()
                        : "assets/images/sample_frame_1.jpg";

                    namesList.Add(name);
                    urlsList.Add(url);
                    pIdx++;
                }

                var combinedPhotoName = string.Join(", ", namesList);
                var combinedPhotoUrl = string.Join(" || ", urlsList);
                var firstPhoto = validPhotos[0];

                var photo = new OrderPhoto
                {
                    Id = $"p_{orderNumber.Replace("-", "").ToLower()}_1",
                    OrderId = order.Id,
                    CustomerId = customer.Id,
                    PhotoUrl = combinedPhotoUrl,
                    PhotoName = combinedPhotoName,
                    FrameSize = !string.IsNullOrWhiteSpace(firstPhoto.FrameSize) ? firstPhoto.FrameSize : (request.Order.CommonSpecs?.FrameSize ?? "12 × 18 inch"),
                    Unit = !string.IsNullOrWhiteSpace(firstPhoto.Unit) ? firstPhoto.Unit : (request.Order.CommonSpecs?.Unit ?? "inch"),
                    FrameType = !string.IsNullOrWhiteSpace(firstPhoto.FrameType) ? firstPhoto.FrameType : (request.Order.CommonSpecs?.FrameType ?? "Wooden Frame"),
                    FrameMaterial = !string.IsNullOrWhiteSpace(firstPhoto.FrameMaterial) ? firstPhoto.FrameMaterial : (request.Order.CommonSpecs?.FrameMaterial ?? "Teak Wood Moulding"),
                    FrameColor = !string.IsNullOrWhiteSpace(firstPhoto.FrameColor) ? firstPhoto.FrameColor : (request.Order.CommonSpecs?.FrameColor ?? "Walnut Brown"),
                    Orientation = !string.IsNullOrWhiteSpace(firstPhoto.Orientation) ? firstPhoto.Orientation : (request.Order.CommonSpecs?.Orientation ?? "Landscape"),
                    Quantity = request.Order.CommonSpecs?.Quantity > 0 ? request.Order.CommonSpecs.Quantity : validPhotos.Sum(p => p.Quantity > 0 ? p.Quantity : 1),
                    UploadedAt = DateTime.UtcNow
                };
                _context.OrderPhotos.Add(photo);
            }
        }

        // 4. Create Notification
        var notification = new Notification
        {
            Id = $"n_{Guid.NewGuid().ToString("N")[..6]}",
            Title = "New Order Received",
            Message = $"Order #{orderNumber} created for {customer.Name}",
            TimeAgo = "Just now",
            Type = "order",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        // 5. Send Automatic WhatsApp Order Confirmation (Fail-safe: never breaks order creation)
        try
        {
            var savedPhotos = await _context.OrderPhotos.Where(p => p.OrderId == order.Id).ToListAsync();

            var targetPhone = _whatsAppService.NormalizePhoneNumber(customer.Phone) ?? customer.Phone;
            var formattedMessage = _whatsAppService.FormatOrderMessage(order, customer, savedPhotos);

            Console.WriteLine("========== WHATSAPP ORDER CONFIRMATION ==========");
            Console.WriteLine($"Order: {order.OrderNumber}");
            Console.WriteLine($"To: {targetPhone}");
            Console.WriteLine();
            Console.WriteLine(formattedMessage);
            Console.WriteLine("=================================================");

            _logger.LogInformation("WhatsApp Order Confirmation (Console Test): Order {OrderNumber} for {CustomerName} ({Phone})",
                order.OrderNumber, customer.Name, targetPhone);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to output automatic WhatsApp confirmation message for order {OrderNumber}. Order remains saved successfully.", order.OrderNumber);
        }

        return new CreateOrderResponseData
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            TotalAmount = order.TotalAmount,
            AdvancePaid = order.AdvancePaid,
            BalanceAmount = order.BalanceAmount,
            OrderStatus = order.OrderStatus,
            PaymentStatus = order.PaymentStatus,
            DeliveryDate = order.DeliveryDate.HasValue ? order.DeliveryDate.Value.ToString("yyyy-MM-dd") : null
        };
    }

    public async Task<UpdateOrderStatusResponseData> UpdateOrderStatusAsync(string id, UpdateOrderStatusRequest request)
    {
        var cleanId = (id ?? "").Trim();
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == cleanId || o.OrderNumber == cleanId || o.CustomerId == cleanId);
        if (order == null)
        {
            throw new ApiException(404, "NOT_FOUND", $"Order '{id}' was not found.");
        }

        order.OrderStatus = request.OrderStatus;
        if (!string.IsNullOrWhiteSpace(request.Remarks))
        {
            order.Remarks = request.Remarks;
        }
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UpdateOrderStatusResponseData
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            OrderStatus = order.OrderStatus,
            UpdatedAt = order.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        };
    }

    public async Task<UpdateOrderResponseData> UpdateOrderAsync(string id, UpdateOrderRequest request)
    {
        var cleanId = (id ?? "").Trim();
        var order = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Photos)
            .FirstOrDefaultAsync(o => o.Id == cleanId || o.OrderNumber == cleanId || o.CustomerId == cleanId);

        if (order == null)
        {
            // If order not found, find or create the Customer entity, then create the Order
            var custPhone = request.Customer?.Phone ?? request.CustomerPhone ?? "";
            var cleanPhone = custPhone.Trim();
            Customer? customer = null;
            if (!string.IsNullOrWhiteSpace(cleanId))
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == cleanId);
            }
            if (customer == null && !string.IsNullOrWhiteSpace(cleanPhone))
            {
                customer = await _context.Customers.FirstOrDefaultAsync(c => c.Phone == cleanPhone);
            }
            if (customer == null)
            {
                var custIdToUse = !string.IsNullOrWhiteSpace(cleanId) ? cleanId : await GenerateNextCustomerIdAsync();
                customer = new Customer
                {
                    Id = custIdToUse,
                    Name = request.Customer?.Name ?? request.CustomerName ?? "Customer",
                    Phone = cleanPhone,
                    AltPhone = request.Customer?.AltPhone ?? request.CustomerAltPhone,
                    City = request.Customer?.City ?? request.CustomerCity ?? "Trivandrum",
                    Address = request.Customer?.Address ?? request.CustomerAddress ?? "",
                    Pincode = request.Customer?.Pincode ?? request.CustomerPincode ?? "",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            var orderNumber = cleanId.StartsWith("RA-", StringComparison.OrdinalIgnoreCase) ? cleanId : $"RA-{1000 + await _context.Orders.CountAsync() + 1}";
            order = new Order
            {
                Id = $"ord_{Guid.NewGuid().ToString("N")[..6]}",
                OrderNumber = orderNumber,
                CustomerId = customer.Id,
                Customer = customer,
                OrderDate = DateTime.UtcNow,
                TotalAmount = request.TotalAmount,
                AdvancePaid = request.AdvancePaid,
                BalanceAmount = request.TotalAmount - request.AdvancePaid,
                PaymentStatus = request.PaymentStatus,
                OrderStatus = request.OrderStatus,
                ConfigMode = request.ConfigMode ?? "same",
                CommonSpecsJson = request.CommonSpecs != null ? JsonSerializer.Serialize(request.CommonSpecs) : null,
                Remarks = request.Remarks,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Orders.Add(order);
        }
        else
        {
            order.TotalAmount = request.TotalAmount;
            order.AdvancePaid = request.AdvancePaid;
            order.BalanceAmount = request.TotalAmount - request.AdvancePaid;
            order.PaymentStatus = request.PaymentStatus;
            order.OrderStatus = request.OrderStatus;
            if (!string.IsNullOrWhiteSpace(request.ConfigMode))
            {
                order.ConfigMode = request.ConfigMode;
            }
            if (request.CommonSpecs != null)
            {
                order.CommonSpecsJson = JsonSerializer.Serialize(request.CommonSpecs);
            }
            if (!string.IsNullOrWhiteSpace(request.DeliveryDate) && DateTime.TryParse(request.DeliveryDate, out var dd))
            {
                order.DeliveryDate = dd.ToUniversalTime();
            }
            if (request.Remarks != null)
            {
                order.Remarks = request.Remarks;
            }

            // Update associated customer profile
            if (order.Customer != null)
            {
                var custName = request.Customer?.Name ?? request.CustomerName;
                var custPhone = request.Customer?.Phone ?? request.CustomerPhone;
                var custAltPhone = request.Customer?.AltPhone ?? request.CustomerAltPhone;
                var custCity = request.Customer?.City ?? request.CustomerCity;
                var custAddress = request.Customer?.Address ?? request.CustomerAddress;
                var custPincode = request.Customer?.Pincode ?? request.CustomerPincode;

                if (!string.IsNullOrWhiteSpace(custName)) order.Customer.Name = custName.Trim();
                if (!string.IsNullOrWhiteSpace(custPhone)) order.Customer.Phone = custPhone.Trim();
                if (custAltPhone != null) order.Customer.AltPhone = custAltPhone.Trim();
                if (!string.IsNullOrWhiteSpace(custCity)) order.Customer.City = custCity.Trim();
                if (custAddress != null) order.Customer.Address = custAddress.Trim();
                if (custPincode != null) order.Customer.Pincode = custPincode.Trim();
            }
        }

        // Attach or update photos if provided (Single row in OrderPhotos table for all photos)
        if (request.Photos != null && request.Photos.Count > 0)
        {
            var validPhotos = request.Photos
                .Where(p => !string.IsNullOrWhiteSpace(p.PhotoName) || !string.IsNullOrWhiteSpace(p.PhotoUrl))
                .ToList();

            if (validPhotos.Count > 0)
            {
                var namesList = new List<string>();
                var urlsList = new List<string>();
                var pIdx = 1;

                foreach (var p in validPhotos)
                {
                    var name = !string.IsNullOrWhiteSpace(p.PhotoName)
                        ? p.PhotoName.Trim()
                        : (!string.IsNullOrWhiteSpace(p.PhotoUrl) ? Path.GetFileName(p.PhotoUrl) : $"Frame_Photo_{pIdx}.jpg");
                    var url = !string.IsNullOrWhiteSpace(p.PhotoUrl)
                        ? p.PhotoUrl.Trim()
                        : "assets/images/sample_frame_1.jpg";

                    namesList.Add(name);
                    urlsList.Add(url);
                    pIdx++;
                }

                var combinedPhotoName = string.Join(", ", namesList);
                var combinedPhotoUrl = string.Join(" || ", urlsList);
                var firstPhoto = validPhotos[0];

                var existingPhotos = await _context.OrderPhotos.Where(p => p.OrderId == order.Id).ToListAsync();
                if (existingPhotos.Count > 0)
                {
                    var primaryPhoto = existingPhotos[0];
                    primaryPhoto.PhotoName = combinedPhotoName;
                    primaryPhoto.PhotoUrl = combinedPhotoUrl;
                    if (!string.IsNullOrWhiteSpace(firstPhoto.FrameSize)) primaryPhoto.FrameSize = firstPhoto.FrameSize;
                    if (!string.IsNullOrWhiteSpace(firstPhoto.Unit)) primaryPhoto.Unit = firstPhoto.Unit;
                    if (!string.IsNullOrWhiteSpace(firstPhoto.FrameType)) primaryPhoto.FrameType = firstPhoto.FrameType;
                    if (!string.IsNullOrWhiteSpace(firstPhoto.FrameMaterial)) primaryPhoto.FrameMaterial = firstPhoto.FrameMaterial;
                    if (!string.IsNullOrWhiteSpace(firstPhoto.FrameColor)) primaryPhoto.FrameColor = firstPhoto.FrameColor;
                    if (!string.IsNullOrWhiteSpace(firstPhoto.Orientation)) primaryPhoto.Orientation = firstPhoto.Orientation;
                    primaryPhoto.Quantity = validPhotos.Sum(p => p.Quantity > 0 ? p.Quantity : 1);

                    // Clean up any extra redundant rows if previously created
                    if (existingPhotos.Count > 1)
                    {
                        _context.OrderPhotos.RemoveRange(existingPhotos.Skip(1));
                    }
                }
                else
                {
                    var photo = new OrderPhoto
                    {
                        Id = $"p_{order.OrderNumber.Replace("-", "").ToLower()}_1",
                        OrderId = order.Id,
                        CustomerId = order.CustomerId,
                        PhotoUrl = combinedPhotoUrl,
                        PhotoName = combinedPhotoName,
                        FrameSize = !string.IsNullOrWhiteSpace(firstPhoto.FrameSize) ? firstPhoto.FrameSize : "12 × 18 inch",
                        Unit = !string.IsNullOrWhiteSpace(firstPhoto.Unit) ? firstPhoto.Unit : "inch",
                        FrameType = !string.IsNullOrWhiteSpace(firstPhoto.FrameType) ? firstPhoto.FrameType : "Wooden Frame",
                        FrameMaterial = !string.IsNullOrWhiteSpace(firstPhoto.FrameMaterial) ? firstPhoto.FrameMaterial : "Teak Wood Moulding",
                        FrameColor = !string.IsNullOrWhiteSpace(firstPhoto.FrameColor) ? firstPhoto.FrameColor : "Walnut Brown",
                        Orientation = !string.IsNullOrWhiteSpace(firstPhoto.Orientation) ? firstPhoto.Orientation : "Landscape",
                        Quantity = validPhotos.Sum(p => p.Quantity > 0 ? p.Quantity : 1),
                        UploadedAt = DateTime.UtcNow
                    };
                    _context.OrderPhotos.Add(photo);
                }
            }
        }

        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var allOrderPhotos = await _context.OrderPhotos
            .Where(p => p.OrderId == order.Id)
            .OrderBy(p => p.UploadedAt)
            .ToListAsync();

        return new UpdateOrderResponseData
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            AdvancePaid = order.AdvancePaid,
            BalanceAmount = order.BalanceAmount,
            PaymentStatus = order.PaymentStatus,
            OrderStatus = order.OrderStatus,
            Photos = ExpandOrderPhotosToDtoList(allOrderPhotos)
        };
    }

    private static List<OrderPhotoDto> ExpandOrderPhotosToDtoList(IEnumerable<OrderPhoto> photos)
    {
        var result = new List<OrderPhotoDto>();
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
                result.Add(new OrderPhotoDto
                {
                    Id = p.Id,
                    PhotoUrl = !string.IsNullOrWhiteSpace(p.PhotoUrl) ? p.PhotoUrl : "assets/images/sample_frame_1.jpg",
                    PhotoName = !string.IsNullOrWhiteSpace(p.PhotoName) ? p.PhotoName : "photo.jpg",
                    FrameSize = p.FrameSize,
                    Unit = p.Unit,
                    FrameType = p.FrameType,
                    FrameMaterial = p.FrameMaterial,
                    FrameColor = p.FrameColor,
                    Orientation = p.Orientation,
                    Quantity = p.Quantity
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var name = i < names.Length ? names[i].Trim() : (names.Length > 0 ? names[0].Trim() : $"photo_{i + 1}.jpg");
                    var url = i < urls.Length ? urls[i].Trim() : (urls.Length > 0 ? urls[0].Trim() : "assets/images/sample_frame_1.jpg");

                    result.Add(new OrderPhotoDto
                    {
                        Id = $"{p.Id}_{i + 1}",
                        PhotoUrl = url,
                        PhotoName = name,
                        FrameSize = p.FrameSize,
                        Unit = p.Unit,
                        FrameType = p.FrameType,
                        FrameMaterial = p.FrameMaterial,
                        FrameColor = p.FrameColor,
                        Orientation = p.Orientation,
                        Quantity = p.Quantity
                    });
                }
            }
        }

        return result;
    }

    public async Task DeleteOrderAsync(string id)
    {
        var cleanId = (id ?? "").Trim();
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == cleanId || o.OrderNumber == cleanId || o.CustomerId == cleanId);
        if (order == null)
        {
            throw new ApiException(404, "NOT_FOUND", $"Order '{id}' was not found.");
        }

        _context.Orders.Remove(order);
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

