using ChocoLux.Models;

namespace ChocoLux.Repositories.Interfaces
{
    public interface IAdminOrderRepository
    {
        IQueryable<Order> GetOrdersQuery();
        Task<Order?> FindOrderByIdAsync(int id);
        Task<Order?> GetOrderDetailAsync(int id);
        Task SaveChangesAsync();
    }
}
