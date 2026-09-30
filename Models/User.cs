using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaigonArts.Api.Models;

public class User
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(100)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Role { get; set; } = "ADMIN";

    [MaxLength(20)]
    public string RegisteredPhone { get; set; } = string.Empty;

    [Column(TypeName = "text")]
    public string PermissionsJson { get; set; } = "[\"READ\", \"WRITE\", \"DELETE\", \"EXPORT\", \"SETTINGS\"]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
