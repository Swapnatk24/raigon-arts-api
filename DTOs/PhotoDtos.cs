using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class PhotoGalleryItemDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("photoName")]
    public string PhotoName { get; set; } = string.Empty;

    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; } = string.Empty;

    [JsonPropertyName("frameSize")]
    public string FrameSize { get; set; } = string.Empty;

    [JsonPropertyName("orientation")]
    public string Orientation { get; set; } = string.Empty;

    [JsonPropertyName("uploadedAt")]
    public string UploadedAt { get; set; } = string.Empty;
}

public class DimensionsDto
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }
}

public class PhotoUploadResponseDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("photoName")]
    public string PhotoName { get; set; } = string.Empty;

    [JsonPropertyName("photoUrl")]
    public string PhotoUrl { get; set; } = string.Empty;

    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [JsonPropertyName("dimensions")]
    public DimensionsDto Dimensions { get; set; } = new();

    [JsonPropertyName("mimeType")]
    public string MimeType { get; set; } = "image/jpeg";
}
