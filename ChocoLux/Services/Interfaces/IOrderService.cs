using ChocoLux.Models;

namespace ChocoLux.Services.Interfaces
{
    public interface IOrderService
    {
        Task<(User? user, List<ShippingZone> shippingZones)> GetCheckoutDataAsync(int userId);
        Task<OrderActionResult> PlaceOrderAsync(int userId, string recipientName, string recipientPhone, string shippingAddress, int shippingZoneId, string paymentMethod, List<CartItem> cartItems);
        Task<OrderActionResult> CreateVnpayPaymentAsync(int userId, string recipientName, string recipientPhone, string shippingAddress, int shippingZoneId, List<CartItem> cartItems, string returnUrl, string ipAddress);
        Task<VnpayHandleResult> HandleVnpayReturnAsync(Dictionary<string, string> vnpParams, string secureHash, bool isVi);
        Task<VnpayIpnResult> HandleVnpayIpnAsync(Dictionary<string, string> vnpParams, string secureHash);
        Task<List<Order>> GetMyOrdersAsync(int userId);
        Task<OrderActionResult> CancelOrderAsync(int userId, int orderId);
        Task<Order?> GetOrderDetailAsync(int userId, int orderId);
    }

    public class OrderActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? OrderId { get; set; }
        public string? PaymentUrl { get; set; }
    }

    public class VnpayHandleResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool ShouldClearCart { get; set; }
    }

    public class VnpayIpnResult
    {
        public string RspCode { get; set; } = "99";
        public string Message { get; set; } = "Unknow error";
    }
}
