using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public class AiToolsService : IAiToolsService
{
    private readonly RaigonDbContext _context;
    private readonly IOrderService _orderService;
    private readonly ILogger<AiToolsService> _logger;

    public AiToolsService(
        RaigonDbContext context,
        IOrderService orderService,
        ILogger<AiToolsService> logger)
    {
        _context = context;
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// 1. get_products: Retrieves framing product categories and catalog based on active FrameSizes in DB.
    /// </summary>
    public async Task<AiProductsResponseDto> GetProductsAsync(string? category = null, string? search = null)
    {
        var query = _context.FrameSizes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim().ToLower();
            query = query.Where(f => f.Category.ToLower().Contains(cat));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(f =>
                f.Code.ToLower().Contains(s) ||
                f.Name.ToLower().Contains(s) ||
                f.Category.ToLower().Contains(s) ||
                f.Unit.ToLower().Contains(s));
        }

        var dbSizes = await query
            .OrderBy(f => f.Width * f.Height)
            .ToListAsync();

        var settings = await _context.WorkshopSettings.FirstOrDefaultAsync();
        var currency = settings?.Currency ?? "₹";
        var taxRate = settings?.TaxRate ?? 5;

        var products = dbSizes.Select(f =>
        {
            var startingPrice = CalculateUnitPrice(f.Width, f.Height, "Wooden Frame", "Teak Wood Moulding", "Clear Float Glass", false, taxRate);
            return new AiProductItemDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Width = f.Width,
                Height = f.Height,
                Unit = f.Unit,
                Category = f.Category,
                IsAvailable = f.Status.Equals("Active", StringComparison.OrdinalIgnoreCase),
                EstimatedStartingPrice = startingPrice,
                Currency = currency,
                LeadTime = "3-5 working days"
            };
        }).ToList();

        var categories = await _context.FrameSizes
            .Select(f => f.Category)
            .Distinct()
            .ToListAsync();

        return new AiProductsResponseDto
        {
            TotalCount = products.Count,
            Categories = categories,
            Products = products
        };
    }

    /// <summary>
    /// 2. get_sizes: Retrieves all supported frame dimensions and aspect ratios from DB.
    /// </summary>
    public async Task<AiProductsResponseDto> GetSizesAsync(string? search = null)
    {
        return await GetProductsAsync(category: null, search: search);
    }

    /// <summary>
    /// 3. get_materials: Retrieves actual frame types, moulding materials, finishes, and glass options.
    /// </summary>
    public Task<AiMaterialsResponseDto> GetMaterialsAsync(string? frameType = null)
    {
        var allMaterials = new List<AiMaterialItemDto>
        {
            new()
            {
                FrameType = "Wooden Frame",
                MaterialName = "Teak Wood Moulding",
                AvailableColors = new() { "Walnut Brown", "Natural Teak", "Dark Espresso", "Rosewood Polish" },
                Description = "Authentic seasoned teak wood with rich grain patterns and protective polyurethane coating.",
                BestSuitedFor = "Family portraits, fine art prints, oil paintings, and heritage photos.",
                PriceTier = "Standard",
                IsAvailable = true
            },
            new()
            {
                FrameType = "Synthetic Frame",
                MaterialName = "Black Matte Moulding",
                AvailableColors = new() { "Matte Black", "Satin White", "Charcoal Grey" },
                Description = "High-density polymer moulding with moisture resistance and sleek minimalist aesthetic.",
                BestSuitedFor = "Modern studio portraits, certificate framing, art posters, and graphic prints.",
                PriceTier = "Economy",
                IsAvailable = true
            },
            new()
            {
                FrameType = "Premium Frame",
                MaterialName = "Gold Leaf Carved",
                AvailableColors = new() { "Antique Gold", "Champagne Gold", "Vintage Bronze" },
                Description = "Hand-carved ornate wooden profile embellished with metallic gold leaf foil and vintage patina.",
                BestSuitedFor = "Wedding photography, traditional oil paintings, royal portraits, and luxury gallery walls.",
                PriceTier = "Luxury",
                IsAvailable = true
            },
            new()
            {
                FrameType = "Tabletop Frame",
                MaterialName = "Oak Wood",
                AvailableColors = new() { "Natural Oak", "Warm Honey", "Classic Mahogany" },
                Description = "Compact hardwood frame fitted with foldable rear easel strut and dual-direction wall hooks.",
                BestSuitedFor = "Desk photos, bedside memories, small gift prints (4×6, 5×7, 8×10 inch).",
                PriceTier = "Economy",
                IsAvailable = true
            },
            new()
            {
                FrameType = "Floating Canvas",
                MaterialName = "Deep Shadowbox Wood",
                AvailableColors = new() { "Matte Black", "Raw Pinewood", "Dark Walnut" },
                Description = "3D floating frame profile creating a 10mm gap around stretched canvas for modern museum look.",
                BestSuitedFor = "Stretched canvas artworks, textured acrylic paintings, and contemporary wall art.",
                PriceTier = "Premium",
                IsAvailable = true
            }
        };

        if (!string.IsNullOrWhiteSpace(frameType))
        {
            var ft = frameType.Trim().ToLower();
            allMaterials = allMaterials
                .Where(m => m.FrameType.ToLower().Contains(ft) || m.MaterialName.ToLower().Contains(ft))
                .ToList();
        }

        var response = new AiMaterialsResponseDto
        {
            Materials = allMaterials,
            SupportedGlassTypes = new()
            {
                "Clear Float Glass (Standard high-clarity 2mm sheet)",
                "Anti-Glare Glass (Micro-etched coating to eliminate room reflections)",
                "Museum Quality UV Glass (99% UV radiation blocking with 98% light transmission)",
                "Clear Acrylic Sheet (Shatterproof lightweight glazing for large wall frames)"
            },
            SupportedMounts = new()
            {
                "No Mount (Full bleed direct framing)",
                "Single White Matboard (2-inch acid-free border for gallery depth)",
                "Double Accent Matboard (White border with contrasting inner dark accent bevel)",
                "Back Mounting Hook (Heavy-duty brass D-rings with stainless hanging wire)"
            }
        };

        return Task.FromResult(response);
    }

    /// <summary>
    /// 4. get_price: Computes deterministic framing price from dimensions, material, glass type, and DB tax rate.
    /// </summary>
    public async Task<AiPriceCalculationResponseDto> GetPriceAsync(AiPriceCalculationRequest request)
    {
        if (request.Width <= 0 || request.Height <= 0)
        {
            throw new ApiException(400, "BAD_REQUEST", "Width and height must be positive numbers.");
        }

        var settings = await _context.WorkshopSettings.FirstOrDefaultAsync();
        var taxRate = settings?.TaxRate ?? 5;
        var currency = settings?.Currency ?? "₹";

        var unit = string.IsNullOrWhiteSpace(request.Unit) ? "inch" : request.Unit.Trim();
        var frameType = string.IsNullOrWhiteSpace(request.FrameType) ? "Wooden Frame" : request.FrameType.Trim();
        var frameMaterial = string.IsNullOrWhiteSpace(request.FrameMaterial) ? "Teak Wood Moulding" : request.FrameMaterial.Trim();
        var glassType = string.IsNullOrWhiteSpace(request.GlassType) ? "Clear Float Glass" : request.GlassType.Trim();
        var quantity = request.Quantity > 0 ? request.Quantity : 1;

        var breakdown = ComputePriceBreakdown(request.Width, request.Height, frameType, frameMaterial, glassType, request.HasMount);
        var unitPreTax = breakdown.MouldingCost + breakdown.GlassCost + breakdown.MountCost + breakdown.BackingAndAssembly;
        
        // Round unit price to nearest ₹10 for clean workshop quotation
        var unitPrice = Math.Round(unitPreTax / 10, 0, MidpointRounding.AwayFromZero) * 10;
        var subtotal = unitPrice * quantity;
        var taxAmount = Math.Round(subtotal * (taxRate / 100), 2, MidpointRounding.AwayFromZero);
        var totalPrice = subtotal + taxAmount;

        return new AiPriceCalculationResponseDto
        {
            Dimensions = $"{request.Width:0.##} × {request.Height:0.##} {unit}",
            FrameType = frameType,
            FrameMaterial = frameMaterial,
            GlassType = glassType,
            HasMount = request.HasMount,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = subtotal,
            TaxRatePercent = taxRate,
            TaxAmount = taxAmount,
            TotalPrice = totalPrice,
            Currency = currency,
            Breakdown = breakdown,
            CalculationNote = $"Based on {request.Width * request.Height:0.##} sq. in. area with {frameMaterial}, {glassType}, and {taxRate}% GST."
        };
    }

    /// <summary>
    /// 5. check_availability: Verifies active frame size in DB and computes accurate workshop turnaround.
    /// </summary>
    public async Task<AiAvailabilityResponseDto> CheckAvailabilityAsync(AiCheckAvailabilityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SizeOrDimensions))
        {
            throw new ApiException(400, "BAD_REQUEST", "Size or dimensions parameter is required.");
        }

        var raw = request.SizeOrDimensions.Trim();
        
        // Match against database frame sizes
        var frameSize = await _context.FrameSizes.FirstOrDefaultAsync(f =>
            f.Code.ToLower() == raw.ToLower() ||
            f.Name.ToLower() == raw.ToLower() ||
            f.Name.ToLower().Replace(" ", "") == raw.ToLower().Replace(" ", ""));

        if (frameSize == null)
        {
            // Try extracting dimensions e.g. "12x18"
            var match = Regex.Match(raw, @"(\d+(?:\.\d+)?)\s*[xX×*]\s*(\d+(?:\.\d+)?)");
            if (match.Success && decimal.TryParse(match.Groups[1].Value, out var w) && decimal.TryParse(match.Groups[2].Value, out var h))
            {
                frameSize = await _context.FrameSizes.FirstOrDefaultAsync(f =>
                    (f.Width == w && f.Height == h) || (f.Width == h && f.Height == w));
            }
        }

        var isCustom = frameSize == null;
        var isAvailable = frameSize == null || frameSize.Status.Equals("Active", StringComparison.OrdinalIgnoreCase);
        var leadDays = isCustom ? 5 : (request.Quantity > 5 ? 5 : 3);
        var readyDate = DateTime.UtcNow.AddDays(leadDays).ToString("yyyy-MM-dd");
        var sizeLabel = frameSize != null ? frameSize.Name : raw;
        var material = string.IsNullOrWhiteSpace(request.FrameMaterial) ? "Teak Wood Moulding" : request.FrameMaterial.Trim();

        return new AiAvailabilityResponseDto
        {
            IsAvailable = isAvailable,
            MatchedSize = sizeLabel,
            Dimensions = frameSize != null ? $"{frameSize.Width} × {frameSize.Height} {frameSize.Unit}" : raw,
            Material = material,
            StockStatus = isAvailable ? (isCustom ? "Made to Order (Custom Size)" : "In Stock (Standard Size)") : "Temporarily Unavailable",
            EstimatedLeadDays = leadDays,
            EstimatedReadyDate = readyDate,
            Message = isAvailable
                ? $"Frame size '{sizeLabel}' with {material} is ready to order. Estimated workshop preparation time is {leadDays} working days (Ready by {readyDate})."
                : $"Frame size '{sizeLabel}' is currently inactive in the workshop catalogue."
        };
    }

    /// <summary>
    /// 6. create_order: Delegates to existing OrderService to persist order and trigger customer notification.
    /// </summary>
    public async Task<AiCreateOrderResponseDto> CreateOrderAsync(AiCreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new ApiException(400, "BAD_REQUEST", "Customer name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            throw new ApiException(400, "BAD_REQUEST", "Customer phone number is required.");
        }

        // 1. Calculate deterministic price if not provided
        var width = 12m;
        var height = 18m;
        var match = Regex.Match(request.FrameSize, @"(\d+(?:\.\d+)?)\s*[xX×*]\s*(\d+(?:\.\d+)?)");
        if (match.Success)
        {
            if (decimal.TryParse(match.Groups[1].Value, out var pw)) width = pw;
            if (decimal.TryParse(match.Groups[2].Value, out var ph)) height = ph;
        }

        var priceQuote = await GetPriceAsync(new AiPriceCalculationRequest
        {
            Width = width,
            Height = height,
            Unit = request.Unit,
            FrameType = request.FrameType,
            FrameMaterial = request.FrameMaterial,
            Quantity = request.Quantity > 0 ? request.Quantity : 1
        });

        var totalAmount = priceQuote.TotalPrice;
        var advancePaid = request.AdvancePaid >= 0 ? request.AdvancePaid : 0;
        var deliveryDateStr = !string.IsNullOrWhiteSpace(request.DeliveryDate)
            ? request.DeliveryDate
            : DateTime.UtcNow.AddDays(4).ToString("yyyy-MM-dd");

        // 2. Build standard CreateOrderRequest to reuse existing OrderService
        var createOrderRequest = new CreateOrderRequest
        {
            Customer = new CustomerInOrderRequest
            {
                Name = request.CustomerName.Trim(),
                Phone = request.CustomerPhone.Trim(),
                AltPhone = request.CustomerAltPhone?.Trim(),
                City = !string.IsNullOrWhiteSpace(request.CustomerCity) ? request.CustomerCity.Trim() : "Trivandrum",
                Address = !string.IsNullOrWhiteSpace(request.CustomerAddress) ? request.CustomerAddress.Trim() : "Studio Order",
                Pincode = !string.IsNullOrWhiteSpace(request.CustomerPincode) ? request.CustomerPincode.Trim() : "695001"
            },
            Order = new OrderInCreateRequest
            {
                ConfigMode = "same",
                OrderDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                DeliveryDate = deliveryDateStr,
                TotalAmount = totalAmount,
                AdvancePaid = advancePaid,
                PaymentStatus = advancePaid >= totalAmount ? "Paid" : (advancePaid > 0 ? "Partial" : "Unpaid"),
                OrderStatus = "In Progress",
                CommonSpecs = new CommonSpecsDto
                {
                    FrameSize = request.FrameSize,
                    Unit = request.Unit,
                    FrameType = request.FrameType,
                    FrameMaterial = request.FrameMaterial,
                    FrameColor = request.FrameColor,
                    Orientation = request.Orientation,
                    Quantity = request.Quantity > 0 ? request.Quantity : 1,
                    Notes = request.Notes
                },
                Photos = new List<OrderPhotoDto>
                {
                    new()
                    {
                        PhotoUrl = !string.IsNullOrWhiteSpace(request.PhotoUrl) ? request.PhotoUrl : "assets/images/sample_frame_1.jpg",
                        PhotoName = !string.IsNullOrWhiteSpace(request.PhotoName) ? request.PhotoName : "Frame_Photo.jpg",
                        FrameSize = request.FrameSize,
                        Unit = request.Unit,
                        FrameType = request.FrameType,
                        FrameMaterial = request.FrameMaterial,
                        FrameColor = request.FrameColor,
                        Orientation = request.Orientation,
                        Quantity = request.Quantity > 0 ? request.Quantity : 1
                    }
                }
            }
        };

        // 3. Delegate to existing OrderService
        var result = await _orderService.CreateOrderAsync(createOrderRequest);

        return new AiCreateOrderResponseDto
        {
            Success = true,
            OrderNumber = result.OrderNumber,
            OrderId = result.OrderId,
            CustomerId = result.CustomerId,
            CustomerName = result.CustomerName,
            CustomerPhone = request.CustomerPhone,
            FrameDetails = $"{request.FrameSize} ({request.FrameType} - {request.FrameMaterial})",
            Quantity = request.Quantity > 0 ? request.Quantity : 1,
            TotalAmount = result.TotalAmount,
            AdvancePaid = result.AdvancePaid,
            BalanceDue = result.BalanceAmount,
            PaymentStatus = result.PaymentStatus,
            OrderStatus = result.OrderStatus,
            DeliveryDate = result.DeliveryDate ?? deliveryDateStr,
            Currency = "₹",
            Message = $"Order #{result.OrderNumber} has been created successfully for {result.CustomerName}. Total Amount: ₹{result.TotalAmount:0.00}, Balance Due: ₹{result.BalanceAmount:0.00}."
        };
    }

    /// <summary>
    /// 7. get_order_status: Retrieves live order details from PostgreSQL by Order Number or Phone Number.
    /// </summary>
    public async Task<AiOrderStatusResponseDto> GetOrderStatusAsync(string orderNumberOrPhone)
    {
        if (string.IsNullOrWhiteSpace(orderNumberOrPhone))
        {
            throw new ApiException(400, "BAD_REQUEST", "Order number or phone number is required.");
        }

        var queryStr = orderNumberOrPhone.Trim().ToLower();
        var digitsOnly = Regex.Replace(queryStr, @"[^\d]", "");

        var orders = await _context.Orders
            .Include(o => o.Customer)
            .Include(o => o.Photos)
            .Where(o =>
                o.OrderNumber.ToLower() == queryStr ||
                o.Id.ToLower() == queryStr ||
                (o.Customer != null && (
                    o.Customer.Phone.Contains(digitsOnly) ||
                    o.Customer.Name.ToLower().Contains(queryStr)
                )))
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .ToListAsync();

        if (orders.Count == 0)
        {
            return new AiOrderStatusResponseDto
            {
                Found = false,
                Query = orderNumberOrPhone,
                Message = $"No framing order found matching '{orderNumberOrPhone}'. Please check the order ID or registered phone number.",
                Orders = new()
            };
        }

        var orderSummaries = orders.Select(o =>
        {
            var itemsList = new List<string>();
            if (o.Photos != null && o.Photos.Count > 0)
            {
                itemsList = o.Photos.Select(p => $"{p.FrameSize} ({p.FrameType} - {p.FrameMaterial}, Color: {p.FrameColor}) x{p.Quantity}").ToList();
            }
            else if (!string.IsNullOrWhiteSpace(o.CommonSpecsJson))
            {
                try
                {
                    var specs = JsonSerializer.Deserialize<CommonSpecsDto>(o.CommonSpecsJson);
                    if (specs != null)
                    {
                        itemsList.Add($"{specs.FrameSize} ({specs.FrameType} - {specs.FrameMaterial}, Color: {specs.FrameColor}) x{specs.Quantity}");
                    }
                }
                catch { }
            }

            var frameSummary = itemsList.Count > 0 ? string.Join(", ", itemsList) : "Custom Photo Framing";
            var deliveryStr = o.DeliveryDate.HasValue ? o.DeliveryDate.Value.ToString("yyyy-MM-dd") : "To be confirmed";

            return new AiOrderSummaryDto
            {
                OrderNumber = o.OrderNumber,
                CustomerName = o.Customer?.Name ?? "Customer",
                CustomerPhone = o.Customer?.Phone ?? "",
                OrderDate = o.OrderDate.ToString("yyyy-MM-dd"),
                DeliveryDate = deliveryStr,
                OrderStatus = o.OrderStatus,
                PaymentStatus = o.PaymentStatus,
                TotalAmount = o.TotalAmount,
                AdvancePaid = o.AdvancePaid,
                BalanceDue = o.BalanceAmount,
                Currency = "₹",
                FrameSummary = frameSummary,
                StatusDescription = $"Order #{o.OrderNumber} is currently '{o.OrderStatus}'. Payment is '{o.PaymentStatus}' with balance due of ₹{o.BalanceAmount:0.00}. Expected delivery: {deliveryStr}."
            };
        }).ToList();

        return new AiOrderStatusResponseDto
        {
            Found = true,
            Query = orderNumberOrPhone,
            Message = $"Found {orderSummaries.Count} order(s) matching '{orderNumberOrPhone}'.",
            Orders = orderSummaries
        };
    }

    /// <summary>
    /// 8. transfer_to_human: Creates an escalation notification in PostgreSQL and prepares human takeover.
    /// </summary>
    public async Task<AiTransferToHumanResponseDto> TransferToHumanAsync(AiTransferToHumanRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            throw new ApiException(400, "BAD_REQUEST", "Customer name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.CustomerPhone))
        {
            throw new ApiException(400, "BAD_REQUEST", "Customer phone is required.");
        }
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ApiException(400, "BAD_REQUEST", "Reason for human transfer is required.");
        }

        var notificationId = $"n_esc_{Guid.NewGuid().ToString("N")[..6]}";
        var channel = string.IsNullOrWhiteSpace(request.Channel) ? "WhatsApp" : request.Channel.Trim();

        var notification = new Notification
        {
            Id = notificationId,
            Title = $"Human Support Requested ({channel})",
            Message = $"Customer '{request.CustomerName}' ({request.CustomerPhone}) requested human agent. Reason: {request.Reason}",
            TimeAgo = "Just now",
            Type = "support",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var settings = await _context.WorkshopSettings.FirstOrDefaultAsync();
        var workshopContact = settings?.Phone ?? "+91 7012160065";
        var workshopAddress = settings?.Address ?? "Workshop St, Art District, Trivandrum, Kerala 695001";

        _logger.LogInformation("AI Assistant transferred customer '{CustomerName}' ({Phone}) to human workshop staff. Notification ID: {NotificationId}",
            request.CustomerName, request.CustomerPhone, notificationId);

        return new AiTransferToHumanResponseDto
        {
            Transferred = true,
            NotificationId = notificationId,
            EscalationTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            WorkshopContact = workshopContact,
            WorkshopAddress = workshopAddress,
            Message = $"You have been connected to our workshop team. A human representative will take over this conversation shortly. You can also reach our workshop directly at {workshopContact}."
        };
    }

    // --- Private Calculation Helpers ---
    private static decimal CalculateUnitPrice(
        decimal width,
        decimal height,
        string frameType,
        string frameMaterial,
        string glassType,
        bool hasMount,
        decimal taxRate)
    {
        var breakdown = ComputePriceBreakdown(width, height, frameType, frameMaterial, glassType, hasMount);
        var subtotal = breakdown.MouldingCost + breakdown.GlassCost + breakdown.MountCost + breakdown.BackingAndAssembly;
        var rounded = Math.Round(subtotal / 10, 0, MidpointRounding.AwayFromZero) * 10;
        var tax = rounded * (taxRate / 100);
        return rounded + tax;
    }

    private static AiPriceBreakdownDto ComputePriceBreakdown(
        decimal width,
        decimal height,
        string frameType,
        string frameMaterial,
        string glassType,
        bool hasMount)
    {
        var areaSqIn = width * height;
        var unitedInches = width + height;

        // 1. Moulding Cost based on Material Profile
        decimal ratePerUnitedInch = 45m; // Standard Wooden Frame / Teak
        var matLower = frameMaterial.ToLower();
        var typeLower = frameType.ToLower();

        if (matLower.Contains("synthetic") || matLower.Contains("matte black moulding") || typeLower.Contains("synthetic"))
        {
            ratePerUnitedInch = 25m;
        }
        else if (matLower.Contains("gold leaf") || matLower.Contains("carved") || typeLower.Contains("premium") || typeLower.Contains("luxury"))
        {
            ratePerUnitedInch = 90m;
        }
        else if (matLower.Contains("oak") || matLower.Contains("pinewood") || matLower.Contains("tabletop") || typeLower.Contains("tabletop"))
        {
            ratePerUnitedInch = 35m;
        }
        else if (typeLower.Contains("floating") || typeLower.Contains("canvas"))
        {
            ratePerUnitedInch = 55m;
        }

        var mouldingCost = Math.Max(unitedInches * ratePerUnitedInch, 300m);

        // 2. Glass / Glazing Cost based on Glass Type
        decimal ratePerSqInGlass = 1.5m; // Clear Float Glass
        var glassLower = glassType.ToLower();

        if (glassLower.Contains("anti-glare") || glassLower.Contains("antiglare") || glassLower.Contains("non-reflective"))
        {
            ratePerSqInGlass = 3.0m;
        }
        else if (glassLower.Contains("museum") || glassLower.Contains("uv"))
        {
            ratePerSqInGlass = 6.0m;
        }
        else if (glassLower.Contains("acrylic") || glassLower.Contains("plexiglass"))
        {
            ratePerSqInGlass = 2.5m;
        }

        var glassCost = Math.Max(areaSqIn * ratePerSqInGlass, 150m);

        // 3. Mount / Matboard Cost
        decimal mountCost = hasMount ? Math.Max(areaSqIn * 1.2m, 150m) : 0m;

        // 4. Backing, Hardware & Workshop Assembly
        var backingAndAssembly = 150m + (areaSqIn * 0.4m);

        return new AiPriceBreakdownDto
        {
            MouldingCost = Math.Round(mouldingCost, 2),
            GlassCost = Math.Round(glassCost, 2),
            MountCost = Math.Round(mountCost, 2),
            BackingAndAssembly = Math.Round(backingAndAssembly, 2)
        };
    }
}
