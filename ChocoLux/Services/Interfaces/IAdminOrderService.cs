using ChocoLux.Models;
using X.PagedList;

namespace ChocoLux.Services.Interfaces
{
    public interface IAdminOrderService
    {
        IPagedList<Order> GetOrders(string? status, int page, int pageSize);
        Task<bool> UpdateStatusAsync(int id, string status);
        Task<Order?> GetOrderDetailAsync(int id);
    }
}
