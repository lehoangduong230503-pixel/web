using ChocoLux.Models;

namespace ChocoLux.Repositories.Interfaces
{
    public interface IAdminProductRepository
    {
        IQueryable<Product> GetActiveProductsQuery();
        Task<Product?> FindProductByIdAsync(int id);
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Origin>> GetOriginsAsync();
        void AddProduct(Product product);
        Task SaveChangesAsync();
    }
}
