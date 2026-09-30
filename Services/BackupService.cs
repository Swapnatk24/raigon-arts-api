using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public interface IBackupService
{
    Task<BackupExportDto> ExportBackupAsync();
    Task<BackupRestoreResponseData> RestoreBackupAsync(BackupRestoreRequest request);
}

public class BackupService : IBackupService
{
    private readonly RaigonDbContext _context;

    public BackupService(RaigonDbContext context)
    {
        _context = context;
    }

    public async Task<BackupExportDto> ExportBackupAsync()
    {
        var customers = await _context.Customers.AsNoTracking().ToListAsync();
        var orders = await _context.Orders.Include(o => o.Photos).AsNoTracking().ToListAsync();
        var frames = await _context.FrameSizes.AsNoTracking().ToListAsync();
        var settings = await _context.WorkshopSettings.AsNoTracking().FirstOrDefaultAsync();

        return new BackupExportDto
        {
            BackupVersion = "1.0",
            ExportedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            Workshop = settings?.WorkshopName ?? "Raigon Arts",
            Customers = customers,
            Orders = orders,
            Frames = frames,
            Settings = settings
        };
    }

    public async Task<BackupRestoreResponseData> RestoreBackupAsync(BackupRestoreRequest request)
    {
        int restoredCustomers = 0;
        int restoredOrders = 0;
        int restoredFrames = 0;

        if (request.Customers != null && request.Customers.Count > 0)
        {
            foreach (var cust in request.Customers)
            {
                var existing = await _context.Customers.FirstOrDefaultAsync(c => c.Id == cust.Id);
                if (existing != null)
                {
                    existing.Name = cust.Name;
                    existing.Phone = cust.Phone;
                    existing.AltPhone = cust.AltPhone;
                    existing.City = cust.City;
                    existing.Address = cust.Address;
                    existing.Pincode = cust.Pincode;
                }
                else
                {
                    _context.Customers.Add(cust);
                }
                restoredCustomers++;
            }
            await _context.SaveChangesAsync();
        }

        if (request.Orders != null && request.Orders.Count > 0)
        {
            foreach (var ord in request.Orders)
            {
                var existing = await _context.Orders.Include(o => o.Photos).FirstOrDefaultAsync(o => o.Id == ord.Id);
                if (existing != null)
                {
                    existing.OrderNumber = ord.OrderNumber;
                    existing.CustomerId = ord.CustomerId;
                    existing.OrderDate = ord.OrderDate;
                    existing.DeliveryDate = ord.DeliveryDate;
                    existing.TotalAmount = ord.TotalAmount;
                    existing.AdvancePaid = ord.AdvancePaid;
                    existing.BalanceAmount = ord.BalanceAmount;
                    existing.PaymentStatus = ord.PaymentStatus;
                    existing.OrderStatus = ord.OrderStatus;
                    existing.ConfigMode = ord.ConfigMode;
                    existing.CommonSpecsJson = ord.CommonSpecsJson;
                    existing.Remarks = ord.Remarks;
                }
                else
                {
                    _context.Orders.Add(ord);
                }
                restoredOrders++;
            }
            await _context.SaveChangesAsync();
        }

        if (request.Frames != null && request.Frames.Count > 0)
        {
            foreach (var frame in request.Frames)
            {
                var existing = await _context.FrameSizes.FirstOrDefaultAsync(f => f.Id == frame.Id || f.Code == frame.Code);
                if (existing != null)
                {
                    existing.Code = frame.Code;
                    existing.Name = frame.Name;
                    existing.Width = frame.Width;
                    existing.Height = frame.Height;
                    existing.Unit = frame.Unit;
                    existing.Category = frame.Category;
                    existing.ActiveOrdersCount = frame.ActiveOrdersCount;
                    existing.Status = frame.Status;
                }
                else
                {
                    _context.FrameSizes.Add(frame);
                }
                restoredFrames++;
            }
            await _context.SaveChangesAsync();
        }

        if (request.Settings != null)
        {
            var existingSettings = await _context.WorkshopSettings.FirstOrDefaultAsync();
            if (existingSettings != null)
            {
                existingSettings.WorkshopName = request.Settings.WorkshopName;
                existingSettings.Subtitle = request.Settings.Subtitle;
                existingSettings.Phone = request.Settings.Phone;
                existingSettings.WhatsappPhone = request.Settings.WhatsappPhone;
                existingSettings.Address = request.Settings.Address;
                existingSettings.AdminUsername = request.Settings.AdminUsername;
                existingSettings.RegisteredPhone = request.Settings.RegisteredPhone;
                existingSettings.Currency = request.Settings.Currency;
                existingSettings.TaxRate = request.Settings.TaxRate;
                existingSettings.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.WorkshopSettings.Add(request.Settings);
            }
            await _context.SaveChangesAsync();
        }

        return new BackupRestoreResponseData
        {
            RestoredCustomers = restoredCustomers,
            RestoredOrders = restoredOrders,
            RestoredFrames = restoredFrames
        };
    }
}
