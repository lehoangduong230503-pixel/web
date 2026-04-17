using ChocoLux.Models;
using X.PagedList;

namespace ChocoLux.Services.Interfaces
{
    public interface IAdminProductService
    {
        Task<IPagedList<Product>> GetPagedProductsAsync(string? search, string? sort, int page, int pageSize);
        Task<Product?> GetProductByIdAsync(int id);
        Task<(List<Category> categories, List<Origin> origins)> GetDropdownDataAsync();
        Task<AdminProductSaveResult> SaveAsync(Product model, IFormFile? imageFile, string webRootPath);
        Task<bool> SoftDeleteAsync(int id);
    }

    public class AdminProductSaveResult
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
    }
}
