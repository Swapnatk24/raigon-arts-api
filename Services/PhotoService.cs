using Microsoft.EntityFrameworkCore;
using RaigonArts.Api.Common;
using RaigonArts.Api.Data;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public interface IPhotoService
{
    Task<List<PhotoGalleryItemDto>> GetPhotosAsync(string? orientation, string? search);
    Task<List<PhotoUploadResponseDto>> UploadPhotosAsync(IFormFileCollection files, string? orderId, string baseUrl);
}

public class PhotoService : IPhotoService
{
    private readonly RaigonDbContext _context;
    private readonly IWebHostEnvironment _env;

    public PhotoService(RaigonDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    public async Task<List<PhotoGalleryItemDto>> GetPhotosAsync(string? orientation, string? search)
    {
        var query = _context.OrderPhotos
            .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(orientation) && !orientation.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Orientation.ToLower() == orientation.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(p =>
                p.PhotoName.ToLower().Contains(s) ||
                (p.Order != null && p.Order.Customer != null && p.Order.Customer.Name.ToLower().Contains(s)));
        }

        var dbList = await query
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

        var result = new List<PhotoGalleryItemDto>();
        foreach (var p in dbList)
        {
            var names = (p.PhotoName ?? "").Split(new[] { ", ", "," }, StringSplitOptions.RemoveEmptyEntries);
            var urls = (p.PhotoUrl ?? "").Split(new[] { " || ", "||", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var count = Math.Max(names.Length, urls.Length);

            var orderId = p.Order != null ? p.Order.OrderNumber : p.OrderId;
            var custName = p.Order != null && p.Order.Customer != null ? p.Order.Customer.Name : "Raigon Gallery";
            var timeStr = p.UploadedAt.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (count <= 1)
            {
                result.Add(new PhotoGalleryItemDto
                {
                    Id = p.Id,
                    OrderId = orderId,
                    CustomerId = p.CustomerId,
                    CustomerName = custName,
                    PhotoName = !string.IsNullOrWhiteSpace(p.PhotoName) ? p.PhotoName : "photo.jpg",
                    PhotoUrl = !string.IsNullOrWhiteSpace(p.PhotoUrl) ? p.PhotoUrl : "assets/images/sample_frame_1.jpg",
                    FrameSize = p.FrameSize,
                    Orientation = p.Orientation,
                    UploadedAt = timeStr
                });
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    var name = i < names.Length ? names[i].Trim() : (names.Length > 0 ? names[0].Trim() : $"photo_{i + 1}.jpg");
                    var url = i < urls.Length ? urls[i].Trim() : (urls.Length > 0 ? urls[0].Trim() : "assets/images/sample_frame_1.jpg");

                    result.Add(new PhotoGalleryItemDto
                    {
                        Id = $"{p.Id}_{i + 1}",
                        OrderId = orderId,
                        CustomerId = p.CustomerId,
                        CustomerName = custName,
                        PhotoName = name,
                        PhotoUrl = url,
                        FrameSize = p.FrameSize,
                        Orientation = p.Orientation,
                        UploadedAt = timeStr
                    });
                }
            }
        }

        return result;
    }

    public async Task<List<PhotoUploadResponseDto>> UploadPhotosAsync(IFormFileCollection files, string? orderId, string baseUrl)
    {
        if (files == null || files.Count == 0)
        {
            throw new ApiException(400, "BAD_REQUEST", "No image files were provided in the upload request.");
        }

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrEmpty(webRoot))
        {
            webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        }

        var uploadsDir = Path.Combine(webRoot, "uploads", DateTime.UtcNow.ToString("yyyy"), DateTime.UtcNow.ToString("MM"));
        if (!Directory.Exists(uploadsDir))
        {
            Directory.CreateDirectory(uploadsDir);
        }

        var responseList = new List<PhotoUploadResponseDto>();

        foreach (var file in files)
        {
            if (file.Length == 0) continue;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".jfif", ".avif", ".bmp", ".gif" };
            if (!allowedExts.Contains(ext))
            {
                continue;
            }

            var safeName = Path.GetFileNameWithoutExtension(file.FileName).Replace(" ", "_");
            var uniqueFileName = $"{safeName}_{Guid.NewGuid().ToString("N")[..8]}{ext}";
            var fullPath = Path.Combine(uploadsDir, uniqueFileName);

            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativeUrl = $"/uploads/{DateTime.UtcNow:yyyy}/{DateTime.UtcNow:MM}/{uniqueFileName}";
            var fullUrl = $"{baseUrl.TrimEnd('/')}{relativeUrl}";

            var photoUploadId = $"p_upl_{Guid.NewGuid().ToString("N")[..6]}";

            responseList.Add(new PhotoUploadResponseDto
            {
                Id = photoUploadId,
                PhotoName = file.FileName,
                PhotoUrl = fullUrl,
                FileSizeBytes = file.Length,
                Dimensions = new DimensionsDto
                {
                    Width = 3600,
                    Height = 2400
                },
                MimeType = file.ContentType ?? "image/jpeg"
            });
        }

        return responseList;
    }
}
