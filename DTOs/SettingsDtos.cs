using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class WorkshopSettingsDto
{
    [JsonPropertyName("workshopName")]
    public string WorkshopName { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; } = string.Empty;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = string.Empty;

    [JsonPropertyName("whatsappPhone")]
    public string WhatsappPhone { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "₹";

    [JsonPropertyName("adminUsername")]
    public string AdminUsername { get; set; } = "admin";

    [JsonPropertyName("taxRate")]
    public decimal TaxRate { get; set; } = 5;

    [JsonPropertyName("registeredPhone")]
    public string RegisteredPhone { get; set; } = string.Empty;
}

public class UpdateWorkshopSettingsRequest
{
    [Required(ErrorMessage = "Workshop name is required.")]
    public string WorkshopName { get; set; } = string.Empty;

    public string Subtitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone is required.")]
    public string Phone { get; set; } = string.Empty;

    public string WhatsappPhone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    public string Address { get; set; } = string.Empty;

    public string AdminUsername { get; set; } = "admin";
    public string RegisteredPhone { get; set; } = string.Empty;
    public decimal? TaxRate { get; set; }
    public string? Currency { get; set; }
}
