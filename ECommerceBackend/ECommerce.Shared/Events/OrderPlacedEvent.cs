namespace ECommerce.Shared.Events;

public class OrderPlacedEvent
{
    public Guid OrderId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}