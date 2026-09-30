using System.ComponentModel.DataAnnotations;

namespace RaigonArts.Api.Models;

public class Notification
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(50)]
    public string TimeAgo { get; set; } = "Just now";

    [MaxLength(50)]
    public string Type { get; set; } = "order"; // order, customer, system

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
