using Microsoft.AspNetCore.Mvc;
using RaigonArts.Api.Common;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Services;

namespace RaigonArts.Api.Controllers;

[ApiController]
[Route("api/v1/photos")]
public class PhotosController : ControllerBase
{
    private readonly IPhotoService _photoService;

    public PhotosController(IPhotoService photoService)
    {
        _photoService = photoService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PhotoGalleryItemDto>>>> GetPhotos(
        [FromQuery] string? orientation,
        [FromQuery] string? search)
    {
        var result = await _photoService.GetPhotosAsync(orientation, search);
        return Ok(ApiResponse<List<PhotoGalleryItemDto>>.Ok(result));
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<List<PhotoUploadResponseDto>>>> UploadPhotos(
        [FromForm] string? orderId)
    {
        var files = Request.Form.Files;
        if (files == null || files.Count == 0)
        {
            throw new ApiException(400, "BAD_REQUEST", "No image files were provided in the upload request.");
        }
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var result = await _photoService.UploadPhotosAsync(files, orderId, baseUrl);
        return StatusCode(201, ApiResponse<List<PhotoUploadResponseDto>>.Ok(result, "Photos uploaded successfully.", 201));
    }
}
