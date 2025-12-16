using System.Linq.Expressions;
using blazor_wasm_app.Models;

namespace blazor_wasm_app.Services;

public interface IRepository<TEntity, TKey>
    where TEntity : BaseEntity<TKey>
    where TKey : struct
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<IEnumerable<TEntity>> GetAllAsync();
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
    Task<TEntity> AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TKey id);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);
    Task<bool> ExistsAsync(TKey id);
}

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<IEnumerable<Product>> GetByCategoryAsync(ProductCategory category);
    Task<IEnumerable<Product>> SearchAsync(string searchTerm, decimal? minPrice = null, decimal? maxPrice = null);
    Task<Dictionary<ProductCategory, int>> GetCategoryCountsAsync();
}

public interface IOrderRepository : IRepository<Order, int>
{
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);
    Task<IEnumerable<Order>> GetByCustomerEmailAsync(string email);
    Task<decimal> GetTotalRevenueAsync(DateTime? from = null, DateTime? to = null);
}
