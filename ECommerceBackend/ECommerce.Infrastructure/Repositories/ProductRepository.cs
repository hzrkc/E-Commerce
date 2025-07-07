using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Entities;
using ECommerce.Core.Interfaces;
using ECommerce.Infrastructure.Data;

namespace ECommerce.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetActiveProductsAsync()
    {
        return await _dbSet
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<bool> IsProductAvailableAsync(string productId, int quantity)
    {
        if (!Guid.TryParse(productId, out Guid id))
            return false;

        var product = await _dbSet.FindAsync(id);
        return product != null && product.IsActive && product.StockQuantity >= quantity;
    }

    public async Task<bool> UpdateStockAsync(string productId, int quantity)
    {
        if (!Guid.TryParse(productId, out Guid id))
            return false;

        var product = await _dbSet.FindAsync(id);
        if (product == null || !product.IsActive || product.StockQuantity < quantity)
            return false;

        product.StockQuantity -= quantity;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
}