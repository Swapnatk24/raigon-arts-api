using System.ComponentModel.DataAnnotations;

namespace RaigonArts.Api.Models;

public class PasswordResetOtp
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string OtpCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ResetToken { get; set; }

    public DateTime ExpiresAt { get; set; }
    public DateTime? ResetTokenExpiresAt { get; set; }

    public bool IsVerified { get; set; } = false;
    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
