using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using ChocoLux.Services.Interfaces;
using X.PagedList;
using X.PagedList.Extensions;

namespace ChocoLux.Services
{
    public class AdminProductService : IAdminProductService
    {
        private readonly IAdminProductRepository _adminProductRepository;

        public AdminProductService(IAdminProductRepository adminProductRepository)
        {
            _adminProductRepository = adminProductRepository;
        }

        public Task<IPagedList<Product>> GetPagedProductsAsync(string? search, string? sort, int page, int pageSize)
        {
            var query = _adminProductRepository.GetActiveProductsQuery();
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.Contains(search));

            query = sort switch
            {
                "remain_asc" => query.OrderBy(p => p.TotalStock - p.SoldCount),
                "remain_desc" => query.OrderByDescending(p => p.TotalStock - p.SoldCount),
                "sold_asc" => query.OrderBy(p => p.SoldCount),
                "sold_desc" => query.OrderByDescending(p => p.SoldCount),
                _ => query.OrderByDescending(p => p.Id)
            };

            return Task.FromResult(query.ToPagedList(page, pageSize));
        }

        public Task<Product?> GetProductByIdAsync(int id)
        {
            return _adminProductRepository.FindProductByIdAsync(id);
        }

        public async Task<(List<Category> categories, List<Origin> origins)> GetDropdownDataAsync()
        {
            var categories = await _adminProductRepository.GetCategoriesAsync();
            var origins = await _adminProductRepository.GetOriginsAsync();
            return (categories, origins);
        }

        public async Task<AdminProductSaveResult> SaveAsync(Product model, IFormFile? imageFile, string webRootPath)
        {
            string? savedImagePath = null;
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsDir = Path.Combine(webRootPath, "images");
                if (!Directory.Exists(uploadsDir))
                    Directory.CreateDirectory(uploadsDir);

                var ext = Path.GetExtension(imageFile.FileName);
                var fileName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(uploadsDir, fileName);

                await using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                savedImagePath = "/images/" + fileName;
            }

            if (model.Id == 0)
            {
                model.Sku = "CL" + DateTime.Now.Ticks.ToString()[^8..];
                model.IsActive = true;
                model.UpdatedAt = DateTime.Now;
                if (savedImagePath != null)
                    model.MainImage = savedImagePath;

                _adminProductRepository.AddProduct(model);
                await _adminProductRepository.SaveChangesAsync();
                return new AdminProductSaveResult { Success = true };
            }

            var existing = await _adminProductRepository.FindProductByIdAsync(model.Id);
            if (existing == null)
                return new AdminProductSaveResult { Success = false, NotFound = true };

            existing.Name = model.Name;
            existing.Price = model.Price;
            existing.CategoryId = model.CategoryId;
            existing.OriginId = model.OriginId;
            existing.Description = model.Description;
            existing.DescriptionVi = model.DescriptionVi;
            existing.TotalStock = model.TotalStock;
            existing.UpdatedAt = DateTime.Now;

            if (savedImagePath != null)
                existing.MainImage = savedImagePath;

            await _adminProductRepository.SaveChangesAsync();
            return new AdminProductSaveResult { Success = true };
        }

        public async Task<bool> SoftDeleteAsync(int id)
        {
            var product = await _adminProductRepository.FindProductByIdAsync(id);
            if (product == null) return false;

            product.IsActive = false;
            await _adminProductRepository.SaveChangesAsync();
            return true;
        }
    }
}
