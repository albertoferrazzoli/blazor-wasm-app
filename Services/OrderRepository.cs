using blazor_wasm_app.Models;

namespace blazor_wasm_app.Services;

public class OrderRepository : InMemoryRepository<Order, int>, IOrderRepository
{
    private int _nextId = 1;

    public OrderRepository()
    {
        SeedData();
    }

    private void SeedData()
    {
        var orders = new[]
        {
            new Order
            {
                Id = _nextId++,
                CustomerName = "Mario Rossi",
                CustomerEmail = "mario.rossi@example.com",
                Status = OrderStatus.Delivered,
                ShippedAt = DateTime.UtcNow.AddDays(-5),
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Laptop Pro 15", Quantity = 1, UnitPrice = 1299.99m },
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Wireless Mouse", Quantity = 2, UnitPrice = 49.99m }
                },
                ShippingAddress = new Address { Street = "Via Roma 123", City = "Milano", PostalCode = "20100", Country = "Italy" }
            },
            new Order
            {
                Id = _nextId++,
                CustomerName = "Luigi Verdi",
                CustomerEmail = "luigi.verdi@example.com",
                Status = OrderStatus.Processing,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Clean Code Book", Quantity = 1, UnitPrice = 39.99m }
                },
                ShippingAddress = new Address { Street = "Corso Italia 45", City = "Roma", PostalCode = "00100", Country = "Italy" }
            },
            new Order
            {
                Id = _nextId++,
                CustomerName = "Anna Bianchi",
                CustomerEmail = "anna.bianchi@example.com",
                Status = OrderStatus.Pending,
                Items = new List<OrderItem>
                {
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Standing Desk", Quantity = 1, UnitPrice = 599.99m },
                    new OrderItem { ProductId = Guid.NewGuid(), ProductName = "Mechanical Keyboard", Quantity = 1, UnitPrice = 149.99m }
                },
                ShippingAddress = new Address { Street = "Via Napoli 78", City = "Firenze", PostalCode = "50100", Country = "Italy" }
            }
        };

        foreach (var order in orders)
        {
            _store.TryAdd(order.Id, order);
        }
    }

    public override Task<Order> AddAsync(Order entity)
    {
        entity.Id = _nextId++;
        return base.AddAsync(entity);
    }

    public Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status)
    {
        var results = _store.Values.Where(o => o.Status == status).ToList();
        return Task.FromResult<IEnumerable<Order>>(results);
    }

    public Task<IEnumerable<Order>> GetByCustomerEmailAsync(string email)
    {
        var results = _store.Values
            .Where(o => o.CustomerEmail.Equals(email, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<Order>>(results);
    }

    public Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _store.Values.AsEnumerable();

        if (from.HasValue)
            query = query.Where(o => o.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(o => o.CreatedAt <= to.Value);

        var total = query
            .Where(o => o.Status != OrderStatus.Cancelled)
            .Sum(o => o.TotalAmount);

        return Task.FromResult(total);
    }
}
