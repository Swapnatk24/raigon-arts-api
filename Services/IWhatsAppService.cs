using RaigonArts.Api.Models;

namespace RaigonArts.Api.Services;

public interface IWhatsAppService
{
    Task<bool> SendOrderConfirmationAsync(Order order, Customer customer, ICollection<OrderPhoto>? photos = null);
    Task<bool> SendTextMessageAsync(string toPhone, string textMessage);
    string FormatOrderMessage(Order order, Customer customer, ICollection<OrderPhoto>? photos = null);
    string? NormalizePhoneNumber(string? rawPhone, string defaultCountryCode = "91");
}
