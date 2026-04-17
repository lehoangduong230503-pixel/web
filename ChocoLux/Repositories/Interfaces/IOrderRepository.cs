using ChocoLux.Models;

namespace ChocoLux.Repositories.Interfaces
{
    public interface IOrderRepository
    {
        Task<User?> GetUserWithAddressesAsync(int userId);
        Task<List<ShippingZone>> GetShippingZonesAsync();
        Task<ShippingZone?> GetShippingZoneByIdAsync(int shippingZoneId);
        void AddOrder(Order order);
        void AddOrderItems(IEnumerable<OrderItem> items);
        void AddPayment(Payment payment);
        Task SaveChangesAsync();
        Task<Order?> GetOrderByCodeWithPaymentAndItemsAsync(string orderCode);
        Task<List<Order>> GetOrdersByUserIdAsync(int userId);
        Task<Order?> GetOrderByUserAsync(int orderId, int userId);
        Task<Order?> GetOrderDetailByUserAsync(int orderId, int userId);
        Task<Dictionary<int, Product>> GetProductsByIdsAsync(List<int> productIds);
    }
}
