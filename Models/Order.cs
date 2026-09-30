using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RaigonArts.Api.Models;

public class Order
{
    [Key]
    [MaxLength(50)]
    public string Id { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string CustomerId { get; set; } = string.Empty;

    [ForeignKey(nameof(CustomerId))]
    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AdvancePaid { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAmount { get; set; }

    [MaxLength(50)]
    public string PaymentStatus { get; set; } = "Unpaid"; // Paid, Partial, Unpaid

    [MaxLength(50)]
    public string OrderStatus { get; set; } = "In Progress"; // In Progress, Pending, Completed, Cancelled

    [MaxLength(50)]
    public string ConfigMode { get; set; } = "same"; // same, custom

    [Column(TypeName = "text")]
    public string? CommonSpecsJson { get; set; }

    [MaxLength(500)]
    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderPhoto> Photos { get; set; } = new List<OrderPhoto>();
}
