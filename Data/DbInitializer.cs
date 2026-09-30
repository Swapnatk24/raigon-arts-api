using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(RaigonDbContext context)
    {
        // Ensure database exists and schema is up to date
        await context.Database.EnsureCreatedAsync();

        // 1. Seed Initial Admin User (only created on fresh database setup)
        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin@raigonarts.com" || u.Username == "admin");
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = "usr_01",
                Username = "admin@raigonarts.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("raigon@2026"),
                DisplayName = "Workshop Manager",
                Role = "ADMIN",
                RegisteredPhone = "+91 7012160065",
                PermissionsJson = "[\"READ\", \"WRITE\", \"DELETE\", \"EXPORT\", \"SETTINGS\"]",
                CreatedAt = DateTime.UtcNow
            };
            await context.Users.AddAsync(adminUser);
        }


        // 2. Seed Workshop Settings
        var existingSettings = await context.WorkshopSettings.FirstOrDefaultAsync();
        if (existingSettings == null)
        {
            var settings = new WorkshopSetting
            {
                Id = 1,
                WorkshopName = "Raigon Arts",
                Subtitle = "Custom Photo Framing & Studio Workshop",
                Phone = "+91 7012160065",
                WhatsappPhone = "+91 7012160065",
                Address = "Workshop St, Art District, Trivandrum, Kerala 695001",
                Currency = "₹",
                AdminUsername = "admin@raigonarts.com",
                TaxRate = 5,
                RegisteredPhone = "+91 7012160065",
                UpdatedAt = DateTime.UtcNow
            };
            await context.WorkshopSettings.AddAsync(settings);
        }
        else
        {
            existingSettings.AdminUsername = "admin@raigonarts.com";
            context.WorkshopSettings.Update(existingSettings);
        }

        // 3. Seed Frame Sizes
        if (!await context.FrameSizes.AnyAsync())
        {
            var frameSizes = new List<FrameSize>
            {
                new() { Id = "f1", Code = "FS-01", Name = "4 × 6 inch", Width = 4, Height = 6, Unit = "inch", Category = "Standard Photo", ActiveOrdersCount = 142, Status = "Active" },
                new() { Id = "f2", Code = "FS-02", Name = "5 × 7 inch", Width = 5, Height = 7, Unit = "inch", Category = "Standard Photo", ActiveOrdersCount = 98, Status = "Active" },
                new() { Id = "f3", Code = "FS-03", Name = "8 × 10 inch", Width = 8, Height = 10, Unit = "inch", Category = "Medium Portrait", ActiveOrdersCount = 210, Status = "Active" },
                new() { Id = "f4", Code = "FS-04", Name = "8 × 12 inch", Width = 8, Height = 12, Unit = "inch", Category = "Medium Portrait", ActiveOrdersCount = 320, Status = "Active" },
                new() { Id = "f5", Code = "FS-05", Name = "12 × 18 inch", Width = 12, Height = 18, Unit = "inch", Category = "Large Gallery", ActiveOrdersCount = 455, Status = "Active" },
                new() { Id = "f6", Code = "FS-06", Name = "16 × 20 inch", Width = 16, Height = 20, Unit = "inch", Category = "Large Gallery", ActiveOrdersCount = 184, Status = "Active" },
                new() { Id = "f7", Code = "FS-07", Name = "20 × 30 inch", Width = 20, Height = 30, Unit = "inch", Category = "Exhibition Wall Art", ActiveOrdersCount = 92, Status = "Active" },
            };
            await context.FrameSizes.AddRangeAsync(frameSizes);
        }

        // 4. Seed Customers
        if (!await context.Customers.AnyAsync())
        {
            var customers = new List<Customer>
            {
                new()
                {
                    Id = "cust_101",
                    Name = "Arun Kumar",
                    Phone = "+91 7934567843",
                    AltPhone = "+91 9447000000",
                    City = "Trivandrum",
                    Address = "Villa 42, Palm Meadows, Kowdiar",
                    Pincode = "695003",
                    CreatedAt = DateTime.UtcNow.AddDays(-14)
                },
                new()
                {
                    Id = "cust_102",
                    Name = "Meera Nair",
                    Phone = "+91 9847123456",
                    AltPhone = "+91 8904357692",
                    City = "Kochi",
                    Address = "4B Skyline Horizon, Marine Drive",
                    Pincode = "682031",
                    CreatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new()
                {
                    Id = "cust_103",
                    Name = "Rahul Raj",
                    Phone = "+91 9446554433",
                    AltPhone = "+91 9846001122",
                    City = "Kollam",
                    Address = "Rose Villa, Beach Road",
                    Pincode = "691001",
                    CreatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new()
                {
                    Id = "cust_104",
                    Name = "Ananya Sreedhar",
                    Phone = "+91 9447889900",
                    AltPhone = "+91 9447112233",
                    City = "Calicut",
                    Address = "22 Greenfield Estate, Mavoor Road",
                    Pincode = "673004",
                    CreatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };
            await context.Customers.AddRangeAsync(customers);
            await context.SaveChangesAsync();

            // 5. Seed Orders
            var orders = new List<Order>
            {
                new()
                {
                    Id = "ord_1001",
                    OrderNumber = "RA-1001",
                    CustomerId = "cust_102",
                    OrderDate = DateTime.UtcNow.AddDays(-6),
                    DeliveryDate = DateTime.UtcNow.AddDays(2),
                    TotalAmount = 4500,
                    AdvancePaid = 0,
                    BalanceAmount = 4500,
                    PaymentStatus = "Unpaid",
                    OrderStatus = "Cancelled",
                    ConfigMode = "same",
                    CommonSpecsJson = "{\"frameSize\":\"12 × 18 inch\",\"unit\":\"inch\",\"frameType\":\"Wooden Frame\",\"frameMaterial\":\"Teak Wood Moulding\",\"frameColor\":\"Walnut Brown\",\"orientation\":\"Landscape\",\"quantity\":2,\"notes\":\"Cancelled on client request\"}",
                    CreatedAt = DateTime.UtcNow.AddDays(-6)
                },
                new()
                {
                    Id = "ord_1003",
                    OrderNumber = "RA-1003",
                    CustomerId = "cust_103",
                    OrderDate = DateTime.UtcNow.AddDays(-4),
                    DeliveryDate = DateTime.UtcNow.AddDays(-1),
                    TotalAmount = 12000,
                    AdvancePaid = 12000,
                    BalanceAmount = 0,
                    PaymentStatus = "Paid",
                    OrderStatus = "Completed",
                    ConfigMode = "same",
                    CommonSpecsJson = "{\"frameSize\":\"20 × 30 inch\",\"unit\":\"inch\",\"frameType\":\"Premium Frame\",\"frameMaterial\":\"Gold Leaf Carved\",\"frameColor\":\"Antique Gold\",\"orientation\":\"Portrait\",\"quantity\":1,\"notes\":\"Fully settled and delivered\"}",
                    CreatedAt = DateTime.UtcNow.AddDays(-4)
                },
                new()
                {
                    Id = "ord_1006",
                    OrderNumber = "RA-1006",
                    CustomerId = "cust_101",
                    OrderDate = DateTime.UtcNow.AddDays(-1),
                    DeliveryDate = DateTime.UtcNow.AddDays(6),
                    TotalAmount = 2500,
                    AdvancePaid = 1000,
                    BalanceAmount = 1500,
                    PaymentStatus = "Partial",
                    OrderStatus = "In Progress",
                    ConfigMode = "same",
                    CommonSpecsJson = "{\"frameSize\":\"12 × 18 inch\",\"unit\":\"inch\",\"frameType\":\"Wooden Frame\",\"frameMaterial\":\"Teak Wood Moulding\",\"frameColor\":\"Walnut Brown\",\"orientation\":\"Landscape\",\"quantity\":1,\"notes\":\"Anti-glare glass coating with back mounting hook\"}",
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
            await context.Orders.AddRangeAsync(orders);
            await context.SaveChangesAsync();

            // 6. Seed Order Photos
            var photos = new List<OrderPhoto>
            {
                new()
                {
                    Id = "p1",
                    OrderId = "ord_1001",
                    CustomerId = "cust_102",
                    PhotoName = "Family_Portrait_Kowdiar.jpg",
                    PhotoUrl = "https://assets.raigonarts.com/photos/family_kowdiar.jpg",
                    FrameSize = "12 × 18 inch",
                    Unit = "inch",
                    FrameType = "Wooden Frame",
                    FrameMaterial = "Teak Wood Moulding",
                    FrameColor = "Walnut Brown",
                    Orientation = "Landscape",
                    Quantity = 2,
                    UploadedAt = DateTime.UtcNow.AddDays(-6)
                },
                new()
                {
                    Id = "p2",
                    OrderId = "ord_1003",
                    CustomerId = "cust_103",
                    PhotoName = "Studio_Portraits.jpg",
                    PhotoUrl = "https://assets.raigonarts.com/photos/studio_portraits.jpg",
                    FrameSize = "20 × 30 inch",
                    Unit = "inch",
                    FrameType = "Premium Frame",
                    FrameMaterial = "Gold Leaf Carved",
                    FrameColor = "Antique Gold",
                    Orientation = "Portrait",
                    Quantity = 1,
                    UploadedAt = DateTime.UtcNow.AddDays(-4)
                },
                new()
                {
                    Id = "p6",
                    OrderId = "ord_1006",
                    CustomerId = "cust_101",
                    PhotoName = "Gallery_Memory.jpg",
                    PhotoUrl = "https://assets.raigonarts.com/photos/gallery_memory.jpg",
                    FrameSize = "12 × 18 inch",
                    Unit = "inch",
                    FrameType = "Wooden Frame",
                    FrameMaterial = "Teak Wood Moulding",
                    FrameColor = "Walnut Brown",
                    Orientation = "Landscape",
                    Quantity = 1,
                    UploadedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
            await context.OrderPhotos.AddRangeAsync(photos);
        }

        // 7. Seed Notifications
        if (!await context.Notifications.AnyAsync())
        {
            var notifications = new List<Notification>
            {
                new()
                {
                    Id = "n1",
                    Title = "New Order Received",
                    Message = "Order #RA-1006 created for Arun Kumar",
                    TimeAgo = "10m ago",
                    IsRead = false,
                    Type = "order",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-10)
                },
                new()
                {
                    Id = "n2",
                    Title = "Payment Updated",
                    Message = "Advance paid ₹1000 for Order #RA-1006",
                    TimeAgo = "1h ago",
                    IsRead = false,
                    Type = "order",
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                },
                new()
                {
                    Id = "n3",
                    Title = "Customer Profile Created",
                    Message = "Ananya Sreedhar added from Calicut",
                    TimeAgo = "2h ago",
                    IsRead = true,
                    Type = "customer",
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                }
            };
            await context.Notifications.AddRangeAsync(notifications);
        }

        // 8. Align any existing legacy or mismatched customer IDs with their authoritative display format
        await AlignExistingCustomerIdsAsync(context);

        await context.SaveChangesAsync();
    }

    private static async Task AlignExistingCustomerIdsAsync(RaigonDbContext context)
    {
        var orders = await context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Photos)
            .ToListAsync();

        foreach (var order in orders)
        {
            if (string.IsNullOrWhiteSpace(order.OrderNumber)) continue;
            var targetCustId = order.OrderNumber;

            if (order.CustomerId != targetCustId)
            {
                var targetCustomer = await context.Customers.FirstOrDefaultAsync(c => c.Id == targetCustId);
                if (targetCustomer == null && order.Customer != null)
                {
                    var oldCustomer = order.Customer;
                    targetCustomer = new Customer
                    {
                        Id = targetCustId,
                        Name = oldCustomer.Name,
                        Phone = oldCustomer.Phone,
                        AltPhone = oldCustomer.AltPhone,
                        City = oldCustomer.City,
                        Address = oldCustomer.Address,
                        Pincode = oldCustomer.Pincode,
                        CreatedAt = oldCustomer.CreatedAt
                    };
                    context.Customers.Add(targetCustomer);
                    await context.SaveChangesAsync();

                    order.CustomerId = targetCustId;
                    foreach (var photo in order.Photos)
                    {
                        photo.CustomerId = targetCustId;
                    }
                    await context.SaveChangesAsync();

                    var hasOtherOrders = await context.Orders.AnyAsync(o => o.Id != order.Id && o.CustomerId == oldCustomer.Id);
                    if (!hasOtherOrders)
                    {
                        context.Customers.Remove(oldCustomer);
                        await context.SaveChangesAsync();
                    }
                }
                else if (targetCustomer != null)
                {
                    order.CustomerId = targetCustId;
                    foreach (var photo in order.Photos)
                    {
                        photo.CustomerId = targetCustId;
                    }
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
