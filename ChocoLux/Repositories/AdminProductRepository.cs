using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChocoLux.Repositories
{
    public class AdminProductRepository : IAdminProductRepository
    {
        private readonly ChocoLuxContext _db;

        public AdminProductRepository(ChocoLuxContext db)
        {
            _db = db;
        }

        public IQueryable<Product> GetActiveProductsQuery()
        {
            return _db.Products
                .Include(p => p.Category)
                .Include(p => p.Origin)
                .Where(p => p.IsActive)
                .AsQueryable();
        }

        public Task<Product?> FindProductByIdAsync(int id)
        {
            return _db.Products.FindAsync(id).AsTask();
        }

        public Task<List<Category>> GetCategoriesAsync()
        {
            return _db.Categories.ToListAsync();
        }

        public Task<List<Origin>> GetOriginsAsync()
        {
            return _db.Origins.ToListAsync();
        }

        public void AddProduct(Product product)
        {
            _db.Products.Add(product);
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }
    }
}
