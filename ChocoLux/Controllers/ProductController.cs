using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChocoLux.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService) => _productService = productService;

        // ── DANH SÁCH + LỌC + PHÂN TRANG ───────────────────────
        public async Task<IActionResult> Index(
            string? search, int? categoryId, int? originId,
            string? sort, int page = 1)
        {
            var result = await _productService.GetPagedProductsAsync(search, categoryId, originId, sort, page, 12);

            // ViewBag cho filter giữ trạng thái
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.OriginId = originId;
            ViewBag.Sort = sort;

            // Truyền danh sách cho dropdown
            ViewBag.Categories = result.categories;
            ViewBag.Origins = result.origins;

            return View(result.products);
        }

        // ── CHI TIẾT SẢN PHẨM ──────────────────────────────────
        public async Task<IActionResult> Detail(int id)
        {
            var product = await _productService.GetProductDetailAsync(id);

            if (product == null) return NotFound();
            return View(product);
        }
    }
}