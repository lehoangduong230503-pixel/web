using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using ChocoLux.Services.Interfaces;
using X.PagedList;
using X.PagedList.Extensions;

namespace ChocoLux.Services
{
    public class AdminOrderService : IAdminOrderService
    {
        private readonly IAdminOrderRepository _adminOrderRepository;

        public AdminOrderService(IAdminOrderRepository adminOrderRepository)
        {
            _adminOrderRepository = adminOrderRepository;
        }

        public IPagedList<Order> GetOrders(string? status, int page, int pageSize)
        {
            var query = _adminOrderRepository.GetOrdersQuery();

            if (!string.IsNullOrEmpty(status))
            {
                query = status == "Confirmed"
                    ? query.Where(o => o.OrderStatus == "Confirmed")
                    : query.Where(o => o.OrderStatus != "Confirmed");
            }

            return query
                .OrderByDescending(o => o.CreatedAt)
                .ToPagedList(page, pageSize);
        }

        public async Task<bool> UpdateStatusAsync(int id, string status)
        {
            var order = await _adminOrderRepository.FindOrderByIdAsync(id);
            if (order == null) return false;

            order.OrderStatus = status;
            switch (status)
            {
                case "Confirmed": order.ConfirmedAt = DateTime.Now; break;
                case "Shipped": order.ShippedAt = DateTime.Now; break;
                case "Completed": order.CompletedAt = DateTime.Now; break;
            }

            await _adminOrderRepository.SaveChangesAsync();
            return true;
        }

        public Task<Order?> GetOrderDetailAsync(int id)
        {
            return _adminOrderRepository.GetOrderDetailAsync(id);
        }
    }
}
