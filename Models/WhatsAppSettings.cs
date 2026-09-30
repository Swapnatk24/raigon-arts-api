namespace RaigonArts.Api.Models;

public class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    public string PhoneNumberId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string? BusinessAccountId { get; set; }
    public string ApiVersion { get; set; } = "v20.0";
    public string TemplateName { get; set; } = "raigon_order_confirmation";
    public string TemplateLanguage { get; set; } = "en_US";
    public string DefaultCountryCode { get; set; } = "91";
    public string WorkshopAddress { get; set; } = "Main Workshop, MG Road, Trivandrum";
    public string WorkshopContact { get; set; } = "+91 70121 60065";
    public bool EnableWhatsAppNotifications { get; set; } = true;
    public bool UseTemplate { get; set; } = true;
    public string VerifyToken { get; set; } = "raigon_arts_wa_verify_2026";
}
