using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChocoLux.Controllers
{
    [Authorize(Roles = "Admin,ADMIN")]
    public class AdminProductController : Controller
    {
        private readonly IAdminProductService _adminProductService;
        private readonly IWebHostEnvironment _env;

        public AdminProductController(IAdminProductService adminProductService, IWebHostEnvironment env)
        {
            _adminProductService = adminProductService;
            _env = env;
        }

        public async Task<IActionResult> Index(string? search, string? sort, int page = 1)
        {
            ViewBag.Search = search;
            ViewBag.Sort = sort;
            var products = await _adminProductService.GetPagedProductsAsync(search, sort, page, 10);
            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View("CreateEdit", new Product());
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _adminProductService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            await LoadDropdowns();
            return View("CreateEdit", product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Product model, IFormFile? ImageFile)
        {
            ModelState.Remove("Sku");
            ModelState.Remove("Category");
            ModelState.Remove("Origin");
            ModelState.Remove("OrderItems");
            ModelState.Remove("MainImage");
            ModelState.Remove("Description");
            ModelState.Remove("DescriptionVi");

            if (!ModelState.IsValid)
            {
                await LoadDropdowns();
                return View("CreateEdit", model);
            }

            var result = await _adminProductService.SaveAsync(model, ImageFile, _env.WebRootPath);
            if (result.NotFound) return NotFound();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _adminProductService.SoftDeleteAsync(id);
            return Json(new { success });
        }

        private async Task LoadDropdowns()
        {
            var data = await _adminProductService.GetDropdownDataAsync();
            ViewBag.Categories = data.categories;
            ViewBag.Origins = data.origins;
        }
    }
}