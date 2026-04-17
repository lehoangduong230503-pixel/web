using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using ChocoLux.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace ChocoLux.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private const string CartKey = "Cart";

        public AccountController(IAccountService accountService) => _accountService = accountService;

        private int GetCartCount()
        {
            var json = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrEmpty(json)) return 0;
            var cart = JsonSerializer.Deserialize<List<CartItem>>(json);
            return cart?.Sum(i => i.Quantity) ?? 0;
        }

        // ── ĐĂNG NHẬP ──────────────────────────────────────────
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _accountService.AuthenticateAsync(model.Email, model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu không đúng");
                return View(model);
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim(ClaimTypes.Name, user.FullName ?? user.Username),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Role, user.Role.RoleName)
    };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync("Cookies", principal, authProps);

            if (user.Role.RoleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                user.Role.RoleName.Equals("ADMIN", StringComparison.OrdinalIgnoreCase))
                return RedirectToAction("Index", "AdminProduct");

            // Nếu có ReturnUrl hợp lệ (từ trang Checkout), redirect về đó
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return Redirect(model.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // ── ĐĂNG KÝ ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // FIX: truyền danh sách khu vực giao hàng cho trang đăng ký
            ViewBag.ShippingZones = await _accountService.GetShippingZonesAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, int? cityId, string? addressDetail)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ShippingZones = await _accountService.GetShippingZonesAsync();
                return View(model);
            }

            var result = await _accountService.RegisterAsync(model, cityId, addressDetail);
            if (!result.Success)
            {
                ModelState.AddModelError("Email", result.Error ?? "Đăng ký thất bại");
                ViewBag.ShippingZones = await _accountService.GetShippingZonesAsync();
                return View(model);
            }

            return RedirectToAction("Login");
        }

        // ── ĐĂNG XUẤT ──────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            // FIX: xóa giỏ hàng khỏi session khi đăng xuất
            HttpContext.Session.Remove(CartKey);
            return RedirectToAction("Index", "Home");
        }

        // ── THÔNG TIN CÁ NHÂN ──────────────────────────────────
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _accountService.GetProfileAsync(userId);

            ViewBag.CartCount = GetCartCount();
            ViewBag.ShippingZones = await _accountService.GetShippingZonesAsync();
            ViewBag.IsAdmin = User.IsInRole("ADMIN") || User.IsInRole("Admin");
            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User model)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _accountService.UpdateProfileAsync(userId, model);
            if (user == null) return NotFound();

            // FIX: cập nhật lại claim Name để navbar hiển thị tên mới
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Username),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync("Cookies", principal, new AuthenticationProperties { IsPersistent = true });

            TempData["Success"] = "Cập nhật thành công!";
            return RedirectToAction("Profile");
        }

        // ── QUẢN LÝ ĐỊA CHỈ ───────────────────────────────────
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddAddress(int cityId, string addressDetail)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _accountService.AddAddressAsync(userId, cityId, addressDetail);
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> DeleteAddress(int addressId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var deleted = await _accountService.DeleteAddressAsync(userId, addressId);
            if (!deleted) return NotFound();
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SetDefaultAddress(int addressId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await _accountService.SetDefaultAddressAsync(userId, addressId);
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetAddresses()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var result = await _accountService.GetAddressesAsync(userId);
            if (result == null) return NotFound();
            return Json(new { addresses = result.Addresses, fullName = result.FullName, phone = result.Phone });
        }
    }
}