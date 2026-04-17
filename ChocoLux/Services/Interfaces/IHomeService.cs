using ChocoLux.Models;

namespace ChocoLux.Services.Interfaces
{
    public interface IHomeService
    {
        Task<List<Product>> GetFeaturedProductsAsync(int count);
    }
}
