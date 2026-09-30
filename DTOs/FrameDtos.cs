using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class FrameSizeDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("width")]
    public decimal Width { get; set; }

    [JsonPropertyName("height")]
    public decimal Height { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "inch";

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("activeOrdersCount")]
    public int ActiveOrdersCount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Active";
}

public class CreateFrameSizeRequest
{
    public string? Code { get; set; }

    [Required(ErrorMessage = "Size Name / Label is required.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Width must be a valid positive number.")]
    public decimal Width { get; set; }

    [Range(0, 1000, ErrorMessage = "Height must be a valid positive number.")]
    public decimal Height { get; set; }

    public string Unit { get; set; } = "inch";
    public string Category { get; set; } = "Standard Photo";
}

public class UpdateFrameSizeRequest
{
    public string? Code { get; set; }

    [Required(ErrorMessage = "Size Name / Label is required.")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Width must be a valid positive number.")]
    public decimal Width { get; set; }

    [Range(0, 1000, ErrorMessage = "Height must be a valid positive number.")]
    public decimal Height { get; set; }

    public string Unit { get; set; } = "inch";
    public string Category { get; set; } = "Standard Photo";
    public string Status { get; set; } = "Active";
}