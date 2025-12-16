using blazor_wasm_app.Models;

namespace blazor_wasm_app.Services;

public class ProductRepository : InMemoryRepository<Product, Guid>, IProductRepository
{
    public ProductRepository()
    {
        SeedData();
    }

    private void SeedData()
    {
        var products = new[]
        {
            new Product { Id = Guid.NewGuid(), Name = "Laptop Pro 15", Description = "High-performance laptop for professionals", Price = 1299.99m, Category = ProductCategory.Electronics, StockQuantity = 50, Tags = new List<string> { "laptop", "professional", "high-end" } },
            new Product { Id = Guid.NewGuid(), Name = "Wireless Mouse", Description = "Ergonomic wireless mouse", Price = 49.99m, Category = ProductCategory.Electronics, StockQuantity = 200, Tags = new List<string> { "mouse", "wireless", "ergonomic" } },
            new Product { Id = Guid.NewGuid(), Name = "Programming T-Shirt", Description = "100% cotton developer t-shirt", Price = 24.99m, Category = ProductCategory.Clothing, StockQuantity = 100, Tags = new List<string> { "clothing", "developer", "cotton" } },
            new Product { Id = Guid.NewGuid(), Name = "Clean Code Book", Description = "A Handbook of Agile Software Craftsmanship", Price = 39.99m, Category = ProductCategory.Books, StockQuantity = 75, Tags = new List<string> { "book", "programming", "best-seller" } },
            new Product { Id = Guid.NewGuid(), Name = "Standing Desk", Description = "Electric height-adjustable desk", Price = 599.99m, Category = ProductCategory.HomeAndGarden, StockQuantity = 25, Tags = new List<string> { "desk", "ergonomic", "electric" } },
            new Product { Id = Guid.NewGuid(), Name = "Mechanical Keyboard", Description = "RGB mechanical gaming keyboard", Price = 149.99m, Category = ProductCategory.Electronics, StockQuantity = 80, Tags = new List<string> { "keyboard", "mechanical", "gaming" } },
            new Product { Id = Guid.NewGuid(), Name = "Coffee Beans Premium", Description = "Arabica coffee beans 1kg", Price = 19.99m, Category = ProductCategory.Food, StockQuantity = 150, Tags = new List<string> { "coffee", "arabica", "premium" } },
            new Product { Id = Guid.NewGuid(), Name = "Yoga Mat", Description = "Non-slip exercise yoga mat", Price = 29.99m, Category = ProductCategory.Sports, StockQuantity = 120, Tags = new List<string> { "yoga", "fitness", "exercise" } },
        };

        foreach (var product in products)
        {
            _store.TryAdd(product.Id, product);
        }
    }

    public Task<IEnumerable<Product>> GetByCategoryAsync(ProductCategory category)
    {
        var results = _store.Values.Where(p => p.Category == category).ToList();
        return Task.FromResult<IEnumerable<Product>>(results);
    }

    public Task<IEnumerable<Product>> SearchAsync(string searchTerm, decimal? minPrice = null, decimal? maxPrice = null)
    {
        var query = _store.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLowerInvariant().Contains(term) ||
                (p.Description?.ToLowerInvariant().Contains(term) ?? false) ||
                p.Tags.Any(t => t.ToLowerInvariant().Contains(term)));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        return Task.FromResult(query.ToList().AsEnumerable());
    }

    public Task<Dictionary<ProductCategory, int>> GetCategoryCountsAsync()
    {
        var counts = _store.Values
            .GroupBy(p => p.Category)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(counts);
    }
}
