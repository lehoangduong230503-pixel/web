using ChocoLux.Models;
using X.PagedList;

namespace ChocoLux.Services.Interfaces
{
    public interface IProductService
    {
        Task<(IPagedList<Product> products, List<Category> categories, List<Origin> origins)> GetPagedProductsAsync(
            string? search, int? categoryId, int? originId, string? sort, int page, int pageSize);
        Task<Product?> GetProductDetailAsync(int id);
    }
}
