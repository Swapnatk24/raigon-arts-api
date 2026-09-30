using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RaigonArts.Api.DTOs;
using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        HttpClient httpClient,
        IOptions<WhatsAppSettings> options,
        ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<bool> SendOrderConfirmationAsync(
        Order order,
        Customer customer,
        ICollection<OrderPhoto>? photos = null)
    {
        try
        {
            if (!_settings.EnableWhatsAppNotifications)
            {
                _logger.LogInformation("WhatsApp notifications are disabled in configuration. Skipping for order {OrderNumber}.", order.OrderNumber);
                return true;
            }

            // 1. Validate & Normalize Phone Number
            var normalizedPhone = NormalizePhoneNumber(customer.Phone, _settings.DefaultCountryCode);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                _logger.LogWarning("Customer '{CustomerName}' has no valid phone number ({RawPhone}). Skipping WhatsApp message for order {OrderNumber}.",
                    customer.Name, customer.Phone, order.OrderNumber);
                return false;
            }

            // 2. Validate Credentials
            if (string.IsNullOrWhiteSpace(_settings.PhoneNumberId) || string.IsNullOrWhiteSpace(_settings.AccessToken))
            {
                _logger.LogWarning("Meta WhatsApp credentials not fully configured (PhoneNumberId or AccessToken missing). Skipping WhatsApp message for order {OrderNumber}.",
                    order.OrderNumber);
                return false;
            }

            // 3. Prepare Frame Specs
            var photoList = photos?.ToList() ?? order.Photos?.ToList() ?? new List<OrderPhoto>();
            CommonSpecsDto? commonSpecs = null;
            if (!string.IsNullOrWhiteSpace(order.CommonSpecsJson))
            {
                try
                {
                    commonSpecs = JsonSerializer.Deserialize<CommonSpecsDto>(order.CommonSpecsJson);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse CommonSpecsJson for order {OrderNumber}", order.OrderNumber);
                }
            }

            // 4. Construct Request Payload
            var requestUrl = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";
            
            WhatsAppMessageRequest messagePayload;

            if (_settings.UseTemplate && !string.IsNullOrWhiteSpace(_settings.TemplateName))
            {
                messagePayload = BuildTemplateRequest(normalizedPhone, order, customer, photoList, commonSpecs);
            }
            else
            {
                var formattedText = FormatOrderMessage(order, customer, photoList);
                messagePayload = new WhatsAppMessageRequest
                {
                    MessagingProduct = "whatsapp",
                    RecipientType = "individual",
                    To = normalizedPhone,
                    Type = "text",
                    Text = new WhatsAppTextPayload
                    {
                        PreviewUrl = false,
                        Body = formattedText
                    }
                };
            }

            var json = JsonSerializer.Serialize(messagePayload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending WhatsApp order confirmation for order {OrderNumber} to {Phone} via Meta API...",
                order.OrderNumber, MaskPhone(normalizedPhone));

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResp = JsonSerializer.Deserialize<WhatsAppApiResponse>(responseContent);
                var messageId = apiResp?.Messages?.FirstOrDefault()?.Id ?? "Unknown";
                _logger.LogInformation("WhatsApp order confirmation sent successfully for order {OrderNumber}. Meta Message ID: {MessageId}",
                    order.OrderNumber, messageId);
                return true;
            }
            else
            {
                _logger.LogError("Meta WhatsApp Cloud API error for order {OrderNumber}. HTTP {StatusCode}: {Response}",
                    order.OrderNumber, (int)response.StatusCode, responseContent);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP connection error while sending WhatsApp message for order {OrderNumber}", order.OrderNumber);
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout occurred while sending WhatsApp message for order {OrderNumber}", order.OrderNumber);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred in WhatsAppService for order {OrderNumber}", order.OrderNumber);
            return false;
        }
    }

    public async Task<bool> SendTextMessageAsync(string toPhone, string textMessage)
    {
        try
        {
            if (!_settings.EnableWhatsAppNotifications)
            {
                _logger.LogInformation("WhatsApp notifications are disabled in configuration. Skipping outbound message to {Phone}.", MaskPhone(toPhone));
                return true;
            }

            var normalizedPhone = NormalizePhoneNumber(toPhone, _settings.DefaultCountryCode);
            if (string.IsNullOrWhiteSpace(normalizedPhone))
            {
                _logger.LogWarning("Invalid phone number ({RawPhone}). Skipping WhatsApp text message.", toPhone);
                return false;
            }

            if (string.IsNullOrWhiteSpace(_settings.PhoneNumberId) || string.IsNullOrWhiteSpace(_settings.AccessToken))
            {
                _logger.LogWarning("Meta WhatsApp credentials not fully configured (PhoneNumberId or AccessToken missing).");
                return false;
            }

            var requestUrl = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";

            var messagePayload = new WhatsAppMessageRequest
            {
                MessagingProduct = "whatsapp",
                RecipientType = "individual",
                To = normalizedPhone,
                Type = "text",
                Text = new WhatsAppTextPayload
                {
                    PreviewUrl = false,
                    Body = textMessage
                }
            };

            var json = JsonSerializer.Serialize(messagePayload, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Sending outbound WhatsApp text message to {Phone} via Meta API...", MaskPhone(normalizedPhone));

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResp = JsonSerializer.Deserialize<WhatsAppApiResponse>(responseContent);
                var messageId = apiResp?.Messages?.FirstOrDefault()?.Id ?? "Unknown";
                _logger.LogInformation("WhatsApp text message sent successfully to {Phone}. Meta Message ID: {MessageId}",
                    MaskPhone(normalizedPhone), messageId);
                return true;
            }
            else
            {
                _logger.LogError("Meta WhatsApp Cloud API error for outbound message to {Phone}. HTTP {StatusCode}: {Response}",
                    MaskPhone(normalizedPhone), (int)response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp text message to {Phone}", MaskPhone(toPhone));
            return false;
        }
    }

    public string FormatOrderMessage(Order order, Customer customer, ICollection<OrderPhoto>? photos = null)
    {
        var photoList = photos?.ToList() ?? order.Photos?.ToList() ?? new List<OrderPhoto>();
        var orderDateStr = order.OrderDate.ToString("yyyy-MM-dd");
        var deliveryDateStr = order.DeliveryDate.HasValue ? order.DeliveryDate.Value.ToString("yyyy-MM-dd") : "To be confirmed";
        var workshopAddress = !string.IsNullOrWhiteSpace(_settings.WorkshopAddress) ? _settings.WorkshopAddress : "Main Workshop, MG Road, Trivandrum";
        var workshopContact = !string.IsNullOrWhiteSpace(_settings.WorkshopContact) ? _settings.WorkshopContact : "+91 70121 60065";

        var sb = new StringBuilder();
        sb.AppendLine("## RAIGON ARTS WORKSHOP - Order Confirmation");
        sb.AppendLine();
        sb.AppendLine($"Dear {customer.Name},");
        sb.AppendLine();
        sb.AppendLine("Thank you for your framing order with Raigon Arts! Here are your complete order details:");
        sb.AppendLine();
        sb.AppendLine($"🔹 Order ID: {order.OrderNumber}");
        sb.AppendLine($"🔹 Order Date: {orderDateStr}");
        sb.AppendLine($"🔹 Expected Delivery Date: {deliveryDateStr}");
        sb.AppendLine();

        if (photoList.Count > 1)
        {
            sb.AppendLine($"🔹 Frame Items ({photoList.Count} items):");
            int idx = 1;
            foreach (var p in photoList)
            {
                var pSize = !string.IsNullOrWhiteSpace(p.FrameSize) ? p.FrameSize : "12 × 18 inch";
                var pType = !string.IsNullOrWhiteSpace(p.FrameType) ? p.FrameType : "Wooden Frame";
                var pMat = !string.IsNullOrWhiteSpace(p.FrameMaterial) ? p.FrameMaterial : "Teak Wood Moulding";
                var pCol = !string.IsNullOrWhiteSpace(p.FrameColor) ? p.FrameColor : "Walnut Brown";
                var pOri = !string.IsNullOrWhiteSpace(p.Orientation) ? p.Orientation : "Landscape";
                sb.AppendLine($"  {idx++}. {pSize} ({pType} - {pMat}) | Color: {pCol} | {pOri} | Qty: {p.Quantity}");
            }
            var totalQty = photoList.Sum(p => p.Quantity);
            sb.AppendLine($"🔹 Total Quantity: {totalQty}");
        }
        else
        {
            var firstPhoto = photoList.FirstOrDefault();
            string frameSize = firstPhoto?.FrameSize ?? "12 × 18 inch";
            string frameType = firstPhoto?.FrameType ?? "Wooden Frame";
            string frameMaterial = firstPhoto?.FrameMaterial ?? "Teak Wood Moulding";
            string colorFinish = firstPhoto?.FrameColor ?? "Walnut Brown";
            string orientation = firstPhoto?.Orientation ?? "Landscape";
            int quantity = firstPhoto?.Quantity ?? 1;

            if (firstPhoto == null && !string.IsNullOrWhiteSpace(order.CommonSpecsJson))
            {
                try
                {
                    var specs = JsonSerializer.Deserialize<CommonSpecsDto>(order.CommonSpecsJson);
                    if (specs != null)
                    {
                        if (!string.IsNullOrWhiteSpace(specs.FrameSize)) frameSize = specs.FrameSize;
                        if (!string.IsNullOrWhiteSpace(specs.FrameType)) frameType = specs.FrameType;
                        if (!string.IsNullOrWhiteSpace(specs.FrameMaterial)) frameMaterial = specs.FrameMaterial;
                        if (!string.IsNullOrWhiteSpace(specs.FrameColor)) colorFinish = specs.FrameColor;
                        if (!string.IsNullOrWhiteSpace(specs.Orientation)) orientation = specs.Orientation;
                        if (specs.Quantity > 0) quantity = specs.Quantity;
                    }
                }
                catch { }
            }

            sb.AppendLine($"🔹 Frame Details: {frameSize} ({frameType} - {frameMaterial})");
            sb.AppendLine($"🔹 Color Finish: {colorFinish} | Orientation: {orientation}");
            sb.AppendLine($"🔹 Quantity: {quantity}");
        }

        sb.AppendLine();
        sb.AppendLine($"🔹 TOTAL AMOUNT: ₹{order.TotalAmount:0.00}");
        sb.AppendLine($"🔹 Advance Paid: ₹{order.AdvancePaid:0.00}");
        sb.AppendLine($"🔹 BALANCE DUE: ₹{order.BalanceAmount:0.00}");
        sb.AppendLine($"🔹 Payment Status: {order.PaymentStatus}");
        sb.AppendLine($"🔹 Order Status: {order.OrderStatus}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"🔹 Workshop Address: {workshopAddress}");
        sb.AppendLine($"🔹 Contact: {workshopContact}");
        sb.AppendLine();
        sb.AppendLine("Thank you for choosing Raigon Arts! 🙏");

        return sb.ToString();
    }

    public string? NormalizePhoneNumber(string? rawPhone, string defaultCountryCode = "91")
    {
        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            return null;
        }

        // Remove all non-digit characters
        var digitsOnly = Regex.Replace(rawPhone.Trim(), @"[^\d]", "");

        if (string.IsNullOrWhiteSpace(digitsOnly))
        {
            return null;
        }

        // Remove leading zeroes
        digitsOnly = digitsOnly.TrimStart('0');

        if (string.IsNullOrWhiteSpace(digitsOnly))
        {
            return null;
        }

        // Handle standard 10-digit phone numbers by prepending country code (default: 91 for India)
        if (digitsOnly.Length == 10)
        {
            digitsOnly = $"{defaultCountryCode}{digitsOnly}";
        }

        // Meta requires international number (E.164 without '+'), typically 10-15 digits
        if (digitsOnly.Length < 10 || digitsOnly.Length > 15)
        {
            return null;
        }

        return digitsOnly;
    }

    private WhatsAppMessageRequest BuildTemplateRequest(
        string normalizedPhone,
        Order order,
        Customer customer,
        List<OrderPhoto> photoList,
        CommonSpecsDto? commonSpecs)
    {
        var orderDateStr = order.OrderDate.ToString("yyyy-MM-dd");
        var deliveryDateStr = order.DeliveryDate.HasValue ? order.DeliveryDate.Value.ToString("yyyy-MM-dd") : "To be confirmed";

        string frameDetails;
        string colorFinish;
        string orientation;
        string quantity;

        if (photoList.Count > 1)
        {
            var summaries = photoList.Select((p, idx) => $"{idx + 1}) {p.FrameSize} ({p.FrameType})").Take(3);
            frameDetails = string.Join(", ", summaries);
            colorFinish = string.Join(", ", photoList.Select(p => p.FrameColor).Distinct().Take(2));
            orientation = string.Join(", ", photoList.Select(p => p.Orientation).Distinct().Take(2));
            quantity = photoList.Sum(p => p.Quantity).ToString();
        }
        else
        {
            var firstPhoto = photoList.FirstOrDefault();
            var size = firstPhoto?.FrameSize ?? commonSpecs?.FrameSize ?? "12 × 18 inch";
            var type = firstPhoto?.FrameType ?? commonSpecs?.FrameType ?? "Wooden Frame";
            var mat = firstPhoto?.FrameMaterial ?? commonSpecs?.FrameMaterial ?? "Teak Wood Moulding";
            
            frameDetails = $"{size} ({type} - {mat})";
            colorFinish = firstPhoto?.FrameColor ?? commonSpecs?.FrameColor ?? "Walnut Brown";
            orientation = firstPhoto?.Orientation ?? commonSpecs?.Orientation ?? "Landscape";
            quantity = (firstPhoto?.Quantity ?? commonSpecs?.Quantity ?? 1).ToString();
        }

        var parameters = new List<WhatsAppParameter>
        {
            new() { Type = "text", Text = SanitizeParameter(customer.Name, "Valued Customer") },     // {{1}} CustomerName
            new() { Type = "text", Text = SanitizeParameter(order.OrderNumber, "N/A") },             // {{2}} OrderId
            new() { Type = "text", Text = SanitizeParameter(orderDateStr, "N/A") },                  // {{3}} OrderDate
            new() { Type = "text", Text = SanitizeParameter(deliveryDateStr, "To be confirmed") },   // {{4}} ExpectedDeliveryDate
            new() { Type = "text", Text = SanitizeParameter(frameDetails, "Custom Framing") },        // {{5}} FrameDetails
            new() { Type = "text", Text = SanitizeParameter(colorFinish, "Standard") },              // {{6}} ColorFinish
            new() { Type = "text", Text = SanitizeParameter(orientation, "Landscape") },             // {{7}} Orientation
            new() { Type = "text", Text = SanitizeParameter(quantity, "1") },                        // {{8}} Quantity
            new() { Type = "text", Text = order.TotalAmount.ToString("0.00") },                      // {{9}} TotalAmount
            new() { Type = "text", Text = order.AdvancePaid.ToString("0.00") },                      // {{10}} AdvancePaid
            new() { Type = "text", Text = order.BalanceAmount.ToString("0.00") },                    // {{11}} BalanceDue
            new() { Type = "text", Text = SanitizeParameter(order.PaymentStatus, "Unpaid") },        // {{12}} PaymentStatus
            new() { Type = "text", Text = SanitizeParameter(order.OrderStatus, "In Progress") }      // {{13}} OrderStatus
        };

        return new WhatsAppMessageRequest
        {
            MessagingProduct = "whatsapp",
            RecipientType = "individual",
            To = normalizedPhone,
            Type = "template",
            Template = new WhatsAppTemplatePayload
            {
                Name = _settings.TemplateName,
                Language = new WhatsAppTemplateLanguage
                {
                    Code = _settings.TemplateLanguage
                },
                Components = new List<WhatsAppTemplateComponent>
                {
                    new()
                    {
                        Type = "body",
                        Parameters = parameters
                    }
                }
            }
        };
    }

    private static string SanitizeParameter(string? val, string fallback)
    {
        if (string.IsNullOrWhiteSpace(val)) return fallback;
        // Meta template parameters cannot contain newlines or tabs
        return val.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ").Trim();
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length <= 4) return "****";
        return string.Concat(phone.AsSpan(0, 2), "****", phone.AsSpan(phone.Length - 4));
    }
}
