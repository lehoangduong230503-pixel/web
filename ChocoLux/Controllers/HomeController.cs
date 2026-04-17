// ChocoLux/Controllers/HomeController.cs
using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace ChocoLux.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private const string CartKey = "Cart";

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        public async Task<IActionResult> Index()
        {
            var featuredProducts = await _homeService.GetFeaturedProductsAsync(8);

            ViewBag.FeaturedProducts = featuredProducts;

            // FIX: set CartCount để navbar hiển thị đúng ngay từ server-side
            var json = HttpContext.Session.GetString(CartKey);
            ViewBag.CartCount = string.IsNullOrEmpty(json) ? 0
                : JsonSerializer.Deserialize<List<CartItem>>(json)?.Sum(i => i.Quantity) ?? 0;

            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult SetLanguage(string culture, string returnUrl = "/")
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true
                }
            );
            return LocalRedirect(returnUrl);
        }
    }
}