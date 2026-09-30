using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/settings")]
public class SettingsController : ControllerBase
{
    private readonly RaigonDbContext _context;

    public SettingsController(RaigonDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<WorkshopSettingsDto>>> GetSettings()
    {
        var settings = await _context.WorkshopSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new WorkshopSetting
            {
                Id = 1,
                WorkshopName = "Raigon Arts",
                Subtitle = "Custom Photo Framing & Studio Workshop",
                Phone = "+91 7012160065",
                WhatsappPhone = "+91 7012160065",
                Address = "Workshop St, Art District, Trivandrum, Kerala 695001",
                Currency = "₹",
                AdminUsername = "admin",
                TaxRate = 5,
                RegisteredPhone = "+91 7012160065"
            };
            _context.WorkshopSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        var dto = new WorkshopSettingsDto
        {
            WorkshopName = settings.WorkshopName,
            Subtitle = settings.Subtitle,
            Phone = settings.Phone,
            WhatsappPhone = settings.WhatsappPhone,
            Address = settings.Address,
            Currency = settings.Currency,
            AdminUsername = settings.AdminUsername,
            TaxRate = settings.TaxRate,
            RegisteredPhone = settings.RegisteredPhone
        };

        return Ok(ApiResponse<WorkshopSettingsDto>.Ok(dto));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<WorkshopSettingsDto>>> UpdateSettings([FromBody] UpdateWorkshopSettingsRequest request)
    {
        var settings = await _context.WorkshopSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new WorkshopSetting { Id = 1 };
            _context.WorkshopSettings.Add(settings);
        }

        settings.WorkshopName = request.WorkshopName.Trim();
        if (request.Subtitle != null) settings.Subtitle = request.Subtitle.Trim();
        settings.Phone = request.Phone.Trim();
        if (request.WhatsappPhone != null) settings.WhatsappPhone = request.WhatsappPhone.Trim();
        settings.Address = request.Address.Trim();
        if (!string.IsNullOrWhiteSpace(request.AdminUsername)) settings.AdminUsername = request.AdminUsername.Trim();
        if (!string.IsNullOrWhiteSpace(request.RegisteredPhone)) settings.RegisteredPhone = request.RegisteredPhone.Trim();
        if (request.TaxRate.HasValue) settings.TaxRate = request.TaxRate.Value;
        if (!string.IsNullOrWhiteSpace(request.Currency)) settings.Currency = request.Currency.Trim();
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var dto = new WorkshopSettingsDto
        {
            WorkshopName = settings.WorkshopName,
            Subtitle = settings.Subtitle,
            Phone = settings.Phone,
            WhatsappPhone = settings.WhatsappPhone,
            Address = settings.Address,
            Currency = settings.Currency,
            AdminUsername = settings.AdminUsername,
            TaxRate = settings.TaxRate,
            RegisteredPhone = settings.RegisteredPhone
        };

        return Ok(ApiResponse<WorkshopSettingsDto>.Ok(dto, "Workshop settings saved successfully."));
    }
}
