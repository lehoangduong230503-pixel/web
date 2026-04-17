using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChocoLux.Repositories
{
    public class CatalogRepository : ICatalogRepository
    {
        private readonly ChocoLuxContext _db;

        public CatalogRepository(ChocoLuxContext db)
        {
            _db = db;
        }

        public Task<List<Product>> GetFeaturedProductsAsync(int count)
        {
            return _db.Products
                .Include(p => p.Category)
                .Include(p => p.Origin)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.SoldCount)
                .Take(count)
                .ToListAsync();
        }

        public IQueryable<Product> GetActiveProductsQuery()
        {
            return _db.Products
                .Include(p => p.Category)
                .Include(p => p.Origin)
                .Where(p => p.IsActive)
                .AsQueryable();
        }

        public Task<List<Category>> GetCategoriesAsync()
        {
            return _db.Categories.ToListAsync();
        }

        public Task<List<Origin>> GetOriginsAsync()
        {
            return _db.Origins.ToListAsync();
        }

        public Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return _db.Products
                .Include(p => p.Category)
                .Include(p => p.Origin)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
        }

        public Task<Product?> FindProductByIdAsync(int id)
        {
            return _db.Products.FindAsync(id).AsTask();
        }
    }
}
