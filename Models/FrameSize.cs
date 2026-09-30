using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaigonArts.Api.Models;

public class FrameSize
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty; // FS-01, FS-02, etc.

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty; // 12 × 18 inch

    [Column(TypeName = "decimal(10,2)")]
    public decimal Width { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Height { get; set; }

    [MaxLength(20)]
    public string Unit { get; set; } = "inch";

    [MaxLength(100)]
    public string Category { get; set; } = "Standard Photo";

    public int ActiveOrdersCount { get; set; } = 0;

    [MaxLength(50)]
    public string Status { get; set; } = "Active"; // Active, Inactive

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
