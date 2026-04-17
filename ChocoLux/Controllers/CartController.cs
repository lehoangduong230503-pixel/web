using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ChocoLux.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private const string CartKey = "Cart";

        public CartController(ICartService cartService) => _cartService = cartService;

        private List<CartItem> GetCartItems()
        {
            var json = HttpContext.Session.GetString(CartKey);
            return string.IsNullOrEmpty(json)
                ? new List<CartItem>()
                : JsonSerializer.Deserialize<List<CartItem>>(json)!;
        }

        private void SaveCart(List<CartItem> cart) =>
            HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));

        public IActionResult Index() => View();

        [HttpGet]
        public IActionResult GetCart()
        {
            var cart = GetCartItems();
            var total = cart.Sum(i => i.Price * i.Quantity);
            return Json(new
            {
                items = cart,
                total = total,
                totalItems = cart.Sum(i => i.Quantity)
            });
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, int quantity = 1)
        {
            var cart = GetCartItems();
            var addResult = await _cartService.AddItemAsync(cart, productId, quantity);
            if (!addResult.Success)
                return Json(new { success = false, message = addResult.Message });

            SaveCart(cart);
            return Json(new
            {
                success = true,
                cartCount = cart.Sum(i => i.Quantity),
                maxStock = addResult.MaxStock
            });
        }

        [HttpPost]
        public async Task<IActionResult> Update(int productId, int quantity)
        {
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                item.Quantity = await _cartService.NormalizeQuantityAsync(productId, quantity);
            }
            SaveCart(cart);
            var newCart = GetCartItems();
            return Json(new
            {
                success = true,
                cartCount = newCart.Sum(i => i.Quantity),
                total = newCart.Sum(i => i.Price * i.Quantity)
            });
        }

        [HttpPost]
        public IActionResult Remove(int productId)
        {
            var cart = GetCartItems();
            cart.RemoveAll(i => i.ProductId == productId);
            SaveCart(cart);
            var newCount = cart.Sum(i => i.Quantity);
            return Json(new { success = true, cartCount = newCount });
        }
    }
}