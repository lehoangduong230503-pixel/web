using ChocoLux.Models;

namespace ChocoLux.Repositories.Interfaces
{
    public interface ICatalogRepository
    {
        Task<List<Product>> GetFeaturedProductsAsync(int count);
        IQueryable<Product> GetActiveProductsQuery();
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Origin>> GetOriginsAsync();
        Task<Product?> GetProductWithDetailsAsync(int id);
        Task<Product?> FindProductByIdAsync(int id);
    }
}
