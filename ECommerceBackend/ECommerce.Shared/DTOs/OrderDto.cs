using System.ComponentModel.DataAnnotations;
using ECommerce.Core.Enums;

namespace ECommerce.Shared.DTOs;

public class CreateOrderRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ProductId { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }
}

public class OrderResponse
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public OrderStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

public class OrderListResponse
{
    public IEnumerable<OrderResponse> Orders { get; set; } = new List<OrderResponse>();
    public int TotalCount { get; set; }
}