using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChocoLux.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ChocoLuxContext _db;

        public OrderRepository(ChocoLuxContext db)
        {
            _db = db;
        }

        public Task<User?> GetUserWithAddressesAsync(int userId)
        {
            return _db.Users
                .Include(u => u.Addresses)
                    .ThenInclude(a => a.City)
                .FirstOrDefaultAsync(u => u.Id == userId);
        }

        public Task<List<ShippingZone>> GetShippingZonesAsync()
        {
            return _db.ShippingZones.OrderBy(s => s.CityName).ToListAsync();
        }

        public Task<ShippingZone?> GetShippingZoneByIdAsync(int shippingZoneId)
        {
            return _db.ShippingZones.FirstOrDefaultAsync(s => s.Id == shippingZoneId);
        }

        public void AddOrder(Order order)
        {
            _db.Orders.Add(order);
        }

        public void AddOrderItems(IEnumerable<OrderItem> items)
        {
            _db.OrderItems.AddRange(items);
        }

        public void AddPayment(Payment payment)
        {
            _db.Payments.Add(payment);
        }

        public Task SaveChangesAsync()
        {
            return _db.SaveChangesAsync();
        }

        public Task<Order?> GetOrderByCodeWithPaymentAndItemsAsync(string orderCode)
        {
            return _db.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderCode == orderCode);
        }

        public Task<List<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return _db.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .ToListAsync();
        }

        public Task<Order?> GetOrderByUserAsync(int orderId, int userId)
        {
            return _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        public Task<Order?> GetOrderDetailByUserAsync(int orderId, int userId)
        {
            return _db.Orders
                .Include(o => o.Payment)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        public Task<Dictionary<int, Product>> GetProductsByIdsAsync(List<int> productIds)
        {
            return _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);
        }
    }
}
