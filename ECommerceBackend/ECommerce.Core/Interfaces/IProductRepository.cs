using ECommerce.Core.Entities;

namespace ECommerce.Core.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<Product>> GetActiveProductsAsync();
    Task<bool> IsProductAvailableAsync(string productId, int quantity);
    Task<bool> UpdateStockAsync(string productId, int quantity);
}