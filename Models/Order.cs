using System.ComponentModel.DataAnnotations;

namespace blazor_wasm_app.Models;

public class Order : AuditableEntity<int>
{
    [Required]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string CustomerEmail { get; set; } = string.Empty;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public List<OrderItem> Items { get; set; } = new();

    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);

    public DateTime? ShippedAt { get; set; }

    public Address? ShippingAddress { get; set; }
}

public class OrderItem
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

public class Address
{
    [Required]
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "Italy";
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
