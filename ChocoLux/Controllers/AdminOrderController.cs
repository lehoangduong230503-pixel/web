using ChocoLux.Models;
using ChocoLux.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChocoLux.Controllers
{
    [Authorize(Roles = "Admin,ADMIN")]
    public class AdminOrderController : Controller
    {
        private readonly IAdminOrderService _adminOrderService;
        public AdminOrderController(IAdminOrderService adminOrderService) => _adminOrderService = adminOrderService;

        public IActionResult Index(string? status, int page = 1)
        {
            ViewBag.Status = status;
            return View(_adminOrderService.GetOrders(status, page, 15));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var success = await _adminOrderService.UpdateStatusAsync(id, status);
            return Json(new { success });
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await _adminOrderService.GetOrderDetailAsync(id);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}