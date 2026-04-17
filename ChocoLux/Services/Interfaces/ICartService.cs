using ChocoLux.Models;

namespace ChocoLux.Services.Interfaces
{
    public interface ICartService
    {
        Task<CartAddResult> AddItemAsync(List<CartItem> cart, int productId, int quantity);
        Task<int> NormalizeQuantityAsync(int productId, int quantity);
    }

    public class CartAddResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int MaxStock { get; set; }
    }
}
