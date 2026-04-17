using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using ChocoLux.Services.Interfaces;

namespace ChocoLux.Services
{
    public class HomeService : IHomeService
    {
        private readonly ICatalogRepository _catalogRepository;

        public HomeService(ICatalogRepository catalogRepository)
        {
            _catalogRepository = catalogRepository;
        }

        public Task<List<Product>> GetFeaturedProductsAsync(int count)
        {
            return _catalogRepository.GetFeaturedProductsAsync(count);
        }
    }
}
