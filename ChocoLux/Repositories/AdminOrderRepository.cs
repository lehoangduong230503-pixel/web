using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChocoLux.Repositories
{
    public class AdminOrderRepository : IAdminOrderRepository
    {
        private readonly ChocoLuxContext _db;

        public AdminOrderRepository(ChocoLuxContext db)
        {
            _db = db;
        }

        public IQueryable<Order> GetOrdersQuery()
        {
            return _db.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .AsQueryable();
        }

        public Task<Order?> FindOrderByIdAsync(int id)
        {
            return _db.Orders.FindAsync(id).AsTask();
        }

        public Task<Order?> GetOrderDetailAsync(int id)
        {
            return _db.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }
    }
}
