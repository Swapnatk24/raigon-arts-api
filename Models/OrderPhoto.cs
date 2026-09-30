using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaigonArts.Api.Models;

public class OrderPhoto
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    [MaxLength(50)]
    public string? CustomerId { get; set; }

    [Column(TypeName = "text")]
    public string PhotoName { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "text")]
    public string PhotoUrl { get; set; } = string.Empty;

    [MaxLength(50)]
    public string FrameSize { get; set; } = "12 × 18 inch";

    [MaxLength(20)]
    public string Unit { get; set; } = "inch";

    [MaxLength(100)]
    public string FrameType { get; set; } = "Wooden Frame";

    [MaxLength(100)]
    public string FrameMaterial { get; set; } = "Teak Wood Moulding";

    [MaxLength(100)]
    public string FrameColor { get; set; } = "Walnut Brown";

    [MaxLength(50)]
    public string Orientation { get; set; } = "Landscape"; // Landscape, Portrait, Square

    public int Quantity { get; set; } = 1;

    public long FileSizeBytes { get; set; } = 0;
    public int DimensionsWidth { get; set; } = 0;
    public int DimensionsHeight { get; set; } = 0;

    [MaxLength(100)]
    public string MimeType { get; set; } = "image/jpeg";

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
