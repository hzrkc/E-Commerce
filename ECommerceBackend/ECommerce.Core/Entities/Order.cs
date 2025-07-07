using System.ComponentModel.DataAnnotations;
using ECommerce.Core.Enums;

namespace ECommerce.Core.Entities;

public class Order : BaseEntity
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal TotalAmount { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public string? ProcessingNotes { get; set; }
}