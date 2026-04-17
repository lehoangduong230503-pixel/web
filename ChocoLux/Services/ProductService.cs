using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using ChocoLux.Services.Interfaces;
using X.PagedList;
using X.PagedList.Extensions;

namespace ChocoLux.Services
{
    public class ProductService : IProductService
    {
        private readonly ICatalogRepository _catalogRepository;

        public ProductService(ICatalogRepository catalogRepository)
        {
            _catalogRepository = catalogRepository;
        }

        public async Task<(IPagedList<Product> products, List<Category> categories, List<Origin> origins)> GetPagedProductsAsync(
            string? search, int? categoryId, int? originId, string? sort, int page, int pageSize)
        {
            var query = _catalogRepository.GetActiveProductsQuery();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (originId.HasValue)
                query = query.Where(p => p.OriginId == originId.Value);

            query = sort switch
            {
                "asc" => query.OrderBy(p => p.Price),
                "desc" => query.OrderByDescending(p => p.Price),
                _ => query.OrderByDescending(p => p.SoldCount)
            };

            var categories = await _catalogRepository.GetCategoriesAsync();
            var origins = await _catalogRepository.GetOriginsAsync();
            var pagedProducts = query.ToPagedList(page, pageSize);

            return (pagedProducts, categories, origins);
        }

        public Task<Product?> GetProductDetailAsync(int id)
        {
            return _catalogRepository.GetProductWithDetailsAsync(id);
        }
    }
}
