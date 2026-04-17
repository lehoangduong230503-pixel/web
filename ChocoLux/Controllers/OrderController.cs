using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ChocoLux.Controllers
{
    [Authorize(Roles = "CUSTOMER")]   // FIX: chỉ CUSTOMER mới vào được
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;
        private const string CartKey = "Cart";

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetCartCount()
        {
            var json = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return 0;
            var cart = JsonSerializer.Deserialize<List<CartItem>>(json);
            return cart?.Sum(i => i.Quantity) ?? 0;
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var data = await _orderService.GetCheckoutDataAsync(userId);

            ViewBag.CartCount = GetCartCount();
            ViewBag.User = data.user;
            ViewBag.Addresses = data.user?.Addresses.OrderByDescending(a => a.IsDefault).ToList() ?? new List<Address>();
            ViewBag.ShippingZones = data.shippingZones;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder(
            string recipientName, string recipientPhone,
            string shippingAddress, int shippingZoneId, string paymentMethod)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var cartItems = GetSessionCartItems();
            var result = await _orderService.PlaceOrderAsync(userId, recipientName, recipientPhone, shippingAddress, shippingZoneId, paymentMethod, cartItems);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            HttpContext.Session.Remove(CartKey);
            return Json(new { success = true, orderId = result.OrderId });
        }

        [HttpPost]
        public async Task<IActionResult> CreateVnpayPayment(
            string recipientName, string recipientPhone, string shippingAddress, int shippingZoneId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var cartItems = GetSessionCartItems();
            var returnUrl = $"{Request.Scheme}://{Request.Host}/Order/VnpayReturn";
            var ipAddress = GetClientIpAddress();

            var result = await _orderService.CreateVnpayPaymentAsync(userId, recipientName, recipientPhone, shippingAddress, shippingZoneId, cartItems, returnUrl, ipAddress);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true, paymentUrl = result.PaymentUrl });
        }

        [AllowAnonymous]
        public async Task<IActionResult> VnpayReturn()
        {
            var vnpParams = Request.Query
                .Where(q => q.Key.StartsWith("vnp_"))
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            var secureHash = vnpParams.GetValueOrDefault("vnp_SecureHash", "");
            vnpParams.Remove("vnp_SecureHash");
            vnpParams.Remove("vnp_SecureHashType");

            bool isVi = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName == "vi";

            var result = await _orderService.HandleVnpayReturnAsync(vnpParams, secureHash, isVi);
            if (result.ShouldClearCart)
                HttpContext.Session.Remove(CartKey);

            if (result.IsSuccess)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction("MyOrders");
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> VnpayIpn()
        {
            var vnpParams = Request.Query
                .Where(q => q.Key.StartsWith("vnp_"))
                .ToDictionary(q => q.Key, q => q.Value.ToString());

            var secureHash = vnpParams.GetValueOrDefault("vnp_SecureHash", string.Empty);
            vnpParams.Remove("vnp_SecureHash");
            vnpParams.Remove("vnp_SecureHashType");

            var result = await _orderService.HandleVnpayIpnAsync(vnpParams, secureHash);
            return Json(new { result.RspCode, result.Message });
        }

        public async Task<IActionResult> MyOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var orders = await _orderService.GetMyOrdersAsync(userId);

            ViewBag.CartCount = GetCartCount();
            return View(orders);
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _orderService.CancelOrderAsync(userId, orderId);
            if (!result.Success)
                return Json(new { success = false, message = result.Message });

            return Json(new { success = true });
        }

        public async Task<IActionResult> OrderDetail(int id)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var order = await _orderService.GetOrderDetailAsync(userId, id);

            if (order == null) return NotFound();
            ViewBag.CartCount = GetCartCount();
            return View(order);
        }

        private string GetClientIpAddress()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (string.IsNullOrWhiteSpace(ip) || ip == "::1") return "127.0.0.1";
            if (ip.Contains(':')) return "127.0.0.1";
            return ip;
        }

        private List<CartItem> GetSessionCartItems()
        {
            var json = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return new List<CartItem>();
            return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
        }
    }
}