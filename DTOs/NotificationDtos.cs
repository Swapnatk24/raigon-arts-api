using System.Text.Json.Serialization;

namespace RaigonArts.Api.DTOs;

public class NotificationDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("time")]
    public string Time { get; set; } = string.Empty;

    [JsonPropertyName("isRead")]
    public bool IsRead { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "order";
}

public class NotificationListResponseData
{
    [JsonPropertyName("unreadCount")]
    public int UnreadCount { get; set; }

    [JsonPropertyName("notifications")]
    public List<NotificationDto> Notifications { get; set; } = new();
}
