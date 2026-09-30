using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/frames")]
public class FramesController : ControllerBase
{
    private readonly RaigonDbContext _context;

    public FramesController(RaigonDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<FrameSizeDto>>>> GetFrames([FromQuery] string? search)
    {
        var query = _context.FrameSizes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(f =>
                f.Code.ToLower().Contains(s) ||
                f.Name.ToLower().Contains(s) ||
                f.Category.ToLower().Contains(s) ||
                f.Unit.ToLower().Contains(s));
        }

        var frames = await query
            .OrderBy(f => f.Width * f.Height)
            .Select(f => new FrameSizeDto
            {
                Id = f.Id,
                Code = f.Code,
                Name = f.Name,
                Width = f.Width,
                Height = f.Height,
                Unit = f.Unit,
                Category = f.Category,
                ActiveOrdersCount = f.ActiveOrdersCount,
                Status = f.Status
            })
            .ToListAsync();

        return Ok(ApiResponse<List<FrameSizeDto>>.Ok(frames));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<FrameSizeDto>>> CreateFrame([FromBody] CreateFrameSizeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ApiException(400, "BAD_REQUEST", "Size Name / Label is required and cannot be empty.");
        }

        // Auto-generate code if empty (e.g. FS-08)
        var code = request.Code?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            var count = await _context.FrameSizes.CountAsync();
            code = $"FS-{(count + 1):D2}";
            var counter = 1;
            while (await _context.FrameSizes.AnyAsync(f => f.Code == code))
            {
                code = $"FS-{(count + 1 + counter):D2}";
                counter++;
            }
        }
        else
        {
            var existing = await _context.FrameSizes.FirstOrDefaultAsync(f => f.Code == code);
            if (existing != null)
            {
                throw new ApiException(409, "CONFLICT", $"Frame size with code '{code}' already exists.");
            }
        }

        // Auto-parse width and height if <= 0
        var width = request.Width;
        var height = request.Height;
        if (width <= 0 || height <= 0)
        {
            var match = Regex.Match(request.Name, @"(\d+(?:\.\d+)?)\s*[xX×*]\s*(\d+(?:\.\d+)?)");
            if (match.Success)
            {
                if (width <= 0 && decimal.TryParse(match.Groups[1].Value, out var parsedW)) width = parsedW;
                if (height <= 0 && decimal.TryParse(match.Groups[2].Value, out var parsedH)) height = parsedH;
            }
            if (width <= 0) width = 12;
            if (height <= 0) height = 18;
        }

        var totalCount = await _context.FrameSizes.CountAsync();
        var id = $"f_size_{totalCount + 1:D2}";
        while (await _context.FrameSizes.AnyAsync(f => f.Id == id))
        {
            id = $"f_{Guid.NewGuid().ToString("N")[..6]}";
        }

        var frame = new FrameSize
        {
            Id = id,
            Code = code,
            Name = request.Name.Trim(),
            Width = width,
            Height = height,
            Unit = string.IsNullOrWhiteSpace(request.Unit) ? "inch" : request.Unit.Trim(),
            Category = string.IsNullOrWhiteSpace(request.Category) ? "Standard Photo" : request.Category.Trim(),
            ActiveOrdersCount = 0,
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        _context.FrameSizes.Add(frame);
        await _context.SaveChangesAsync();

        var dto = new FrameSizeDto
        {
            Id = frame.Id,
            Code = frame.Code,
            Name = frame.Name,
            Width = frame.Width,
            Height = frame.Height,
            Unit = frame.Unit,
            Category = frame.Category,
            ActiveOrdersCount = frame.ActiveOrdersCount,
            Status = frame.Status
        };

        return StatusCode(201, ApiResponse<FrameSizeDto>.Ok(dto, $"Frame size '{frame.Name}' created.", 201));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<FrameSizeDto>>> UpdateFrame(string id, [FromBody] UpdateFrameSizeRequest request)
    {
        var frame = await _context.FrameSizes.FirstOrDefaultAsync(f => f.Id == id || f.Code == id);
        if (frame == null)
        {
            throw new ApiException(404, "NOT_FOUND", $"Frame size '{id}' was not found.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ApiException(400, "BAD_REQUEST", "Size Name / Label is required.");
        }

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim();
            var codeExists = await _context.FrameSizes.AnyAsync(f => f.Code == code && f.Id != frame.Id);
            if (codeExists)
            {
                throw new ApiException(409, "CONFLICT", $"Frame size with code '{code}' already exists.");
            }
            frame.Code = code;
        }

        frame.Name = request.Name.Trim();
        if (request.Width > 0) frame.Width = request.Width;
        if (request.Height > 0) frame.Height = request.Height;
        if (!string.IsNullOrWhiteSpace(request.Unit)) frame.Unit = request.Unit.Trim();
        if (!string.IsNullOrWhiteSpace(request.Category)) frame.Category = request.Category.Trim();
        if (!string.IsNullOrWhiteSpace(request.Status)) frame.Status = request.Status.Trim();

        await _context.SaveChangesAsync();

        var dto = new FrameSizeDto
        {
            Id = frame.Id,
            Code = frame.Code,
            Name = frame.Name,
            Width = frame.Width,
            Height = frame.Height,
            Unit = frame.Unit,
            Category = frame.Category,
            ActiveOrdersCount = frame.ActiveOrdersCount,
            Status = frame.Status
        };

        return Ok(ApiResponse<FrameSizeDto>.Ok(dto, "Frame size updated successfully."));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteFrame(string id)
    {
        var frame = await _context.FrameSizes.FirstOrDefaultAsync(f => f.Id == id || f.Code == id);
        if (frame == null)
        {
            throw new ApiException(404, "NOT_FOUND", $"Frame size '{id}' was not found.");
        }

        _context.FrameSizes.Remove(frame);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse.Ok("Frame size deleted successfully."));
    }
}