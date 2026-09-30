using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaigonArts.Api.Models;

public class WorkshopSetting
{
    [Key]
    public int Id { get; set; } = 1;

    [Required]
    [MaxLength(150)]
    public string WorkshopName { get; set; } = "Raigon Arts";

    [MaxLength(250)]
    public string Subtitle { get; set; } = "Custom Photo Framing & Studio Workshop";

    [MaxLength(20)]
    public string Phone { get; set; } = "+91 7012160065";

    [MaxLength(20)]
    public string WhatsappPhone { get; set; } = "+91 7012160065";

    [MaxLength(500)]
    public string Address { get; set; } = "Workshop St, Art District, Trivandrum, Kerala 695001";

    [MaxLength(10)]
    public string Currency { get; set; } = "₹";

    [MaxLength(100)]
    public string AdminUsername { get; set; } = "admin";

    [Column(TypeName = "decimal(5,2)")]
    public decimal TaxRate { get; set; } = 5;

    [MaxLength(20)]
    public string RegisteredPhone { get; set; } = "+91 7012160065";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
