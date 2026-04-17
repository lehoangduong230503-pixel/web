using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using ChocoLux.Repositories.Interfaces;

namespace ChocoLux.Services
{
    public class CartService : ICartService
    {
        private readonly ICatalogRepository _catalogRepository;

        public CartService(ICatalogRepository catalogRepository)
        {
            _catalogRepository = catalogRepository;
        }

        public async Task<CartAddResult> AddItemAsync(List<CartItem> cart, int productId, int quantity)
        {
            var product = await _catalogRepository.FindProductByIdAsync(productId);
            if (product == null)
            {
                return new CartAddResult
                {
                    Success = false,
                    Message = "Sản phẩm không tồn tại"
                };
            }

            var remaining = product.TotalStock - product.SoldCount;
            if (remaining <= 0)
            {
                return new CartAddResult
                {
                    Success = false,
                    Message = "Sản phẩm đã hết hàng"
                };
            }

            var existing = cart.FirstOrDefault(i => i.ProductId == productId);
            var currentQty = existing?.Quantity ?? 0;
            var newQty = currentQty + quantity;

            if (newQty > remaining)
            {
                var canAdd = remaining - currentQty;
                if (canAdd <= 0)
                {
                    return new CartAddResult
                    {
                        Success = false,
                        Message = $"Bạn đã thêm tối đa {remaining} sản phẩm này vào giỏ hàng (tồn kho: {remaining})."
                    };
                }

                return new CartAddResult
                {
                    Success = false,
                    Message = $"Chỉ có thể thêm {canAdd} sản phẩm nữa (tồn kho còn {remaining}, bạn đã có {currentQty} trong giỏ)."
                };
            }

            if (existing != null)
            {
                existing.Quantity = newQty;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    ImageUrl = product.MainImage ?? string.Empty,
                    Quantity = newQty
                });
            }

            return new CartAddResult
            {
                Success = true,
                MaxStock = remaining
            };
        }

        public async Task<int> NormalizeQuantityAsync(int productId, int quantity)
        {
            var product = await _catalogRepository.FindProductByIdAsync(productId);
            if (product == null)
            {
                return Math.Max(1, quantity);
            }

            var remaining = product.TotalStock - product.SoldCount;
            if (quantity > remaining) quantity = remaining;
            if (quantity < 1) quantity = 1;
            return quantity;
        }
    }
}
