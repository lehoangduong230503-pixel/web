using ChocoLux.Models;
using ChocoLux.Repositories.Interfaces;
using ChocoLux.Services.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace ChocoLux.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IConfiguration _config;

        public OrderService(IOrderRepository orderRepository, IConfiguration config)
        {
            _orderRepository = orderRepository;
            _config = config;
        }

        public async Task<(User? user, List<ShippingZone> shippingZones)> GetCheckoutDataAsync(int userId)
        {
            var user = await _orderRepository.GetUserWithAddressesAsync(userId);
            var zones = await _orderRepository.GetShippingZonesAsync();
            return (user, zones);
        }

        public async Task<OrderActionResult> PlaceOrderAsync(int userId, string recipientName, string recipientPhone, string shippingAddress, int shippingZoneId, string paymentMethod, List<CartItem> cartItems)
        {
            if (!cartItems.Any())
                return new OrderActionResult { Success = false, Message = "Giỏ hàng trống!" };

            var shippingZone = await _orderRepository.GetShippingZoneByIdAsync(shippingZoneId);
            if (shippingZone == null)
                return new OrderActionResult { Success = false, Message = "Khu vực giao hàng không hợp lệ." };

            var subtotal = cartItems.Sum(i => i.Price * i.Quantity);
            var vatRate = 8m;
            var vatAmount = subtotal * vatRate / 100;
            var shippingFee = shippingZone.Fee;
            var total = subtotal + vatAmount + shippingFee;
            var orderCode = "CL" + DateTime.Now.ToString("yyMMddHHmmss");

            var order = new Order
            {
                OrderCode = orderCode,
                UserId = userId,
                RecipientName = recipientName,
                RecipientPhone = recipientPhone,
                ShippingAddressSnapshot = shippingAddress,
                Subtotal = subtotal,
                VatRate = vatRate,
                VatAmount = vatAmount,
                ShippingFee = shippingFee,
                TotalAmount = total,
                OrderStatus = "Pending",
                CreatedAt = DateTime.Now
            };

            _orderRepository.AddOrder(order);
            await _orderRepository.SaveChangesAsync();

            _orderRepository.AddOrderItems(cartItems.Select(item => new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PriceAtPurchase = item.Price
            }));

            var stockCheck = await DeductStockAsync(cartItems);
            if (!stockCheck.success)
                return new OrderActionResult { Success = false, Message = stockCheck.message };

            _orderRepository.AddPayment(new Payment
            {
                OrderId = order.Id,
                PaymentMethod = paymentMethod,
                PaymentStatus = "Pending"
            });

            await _orderRepository.SaveChangesAsync();

            return new OrderActionResult { Success = true, OrderId = order.Id };
        }

        public async Task<OrderActionResult> CreateVnpayPaymentAsync(int userId, string recipientName, string recipientPhone, string shippingAddress, int shippingZoneId, List<CartItem> cartItems, string returnUrl, string ipAddress)
        {
            if (!cartItems.Any())
                return new OrderActionResult { Success = false, Message = "Giỏ hàng trống!" };

            var shippingZone = await _orderRepository.GetShippingZoneByIdAsync(shippingZoneId);
            if (shippingZone == null)
                return new OrderActionResult { Success = false, Message = "Khu vực giao hàng không hợp lệ." };

            var subtotal = cartItems.Sum(i => i.Price * i.Quantity);
            var vatRate = 8m;
            var vatAmount = subtotal * vatRate / 100;
            var shippingFee = shippingZone.Fee;
            var total = subtotal + vatAmount + shippingFee;
            var orderCode = "CL" + DateTime.Now.ToString("yyMMddHHmmss");

            var order = new Order
            {
                OrderCode = orderCode,
                UserId = userId,
                RecipientName = recipientName,
                RecipientPhone = recipientPhone,
                ShippingAddressSnapshot = shippingAddress,
                Subtotal = subtotal,
                VatRate = vatRate,
                VatAmount = vatAmount,
                ShippingFee = shippingFee,
                TotalAmount = total,
                OrderStatus = "PendingPayment",
                CreatedAt = DateTime.Now
            };

            _orderRepository.AddOrder(order);
            await _orderRepository.SaveChangesAsync();

            _orderRepository.AddOrderItems(cartItems.Select(item => new OrderItem
            {
                OrderId = order.Id,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                PriceAtPurchase = item.Price
            }));

            _orderRepository.AddPayment(new Payment
            {
                OrderId = order.Id,
                PaymentMethod = "VNPay",
                PaymentStatus = "Pending"
            });

            await _orderRepository.SaveChangesAsync();

            return new OrderActionResult
            {
                Success = true,
                PaymentUrl = BuildVnpayUrl(orderCode, total, returnUrl, ipAddress)
            };
        }

        public async Task<VnpayHandleResult> HandleVnpayReturnAsync(Dictionary<string, string> vnpParams, string secureHash, bool isVi)
        {
            var isValid = VerifyVnpaySignature(vnpParams, secureHash);
            var responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode", string.Empty);
            var transactionStatus = vnpParams.GetValueOrDefault("vnp_TransactionStatus", string.Empty);
            var txnRef = vnpParams.GetValueOrDefault("vnp_TxnRef", string.Empty);
            var amountRaw = vnpParams.GetValueOrDefault("vnp_Amount", "0");

            var order = await _orderRepository.GetOrderByCodeWithPaymentAndItemsAsync(txnRef);
            if (order != null && long.TryParse(amountRaw, out var vnpAmount))
            {
                var expectedAmount = (long)(order.TotalAmount * 100);
                if (vnpAmount != expectedAmount)
                {
                    return new VnpayHandleResult
                    {
                        IsSuccess = false,
                        Message = isVi ? "Sai số tiền giao dịch từ VNPay!" : "Invalid VNPay transaction amount!",
                        ShouldClearCart = false
                    };
                }
            }

            if (order != null && isValid && responseCode == "00" && transactionStatus == "00")
            {
                var shouldDeductStock = order.Payment?.PaymentStatus != "Paid";
                if (shouldDeductStock)
                {
                    var cartItems = order.OrderItems
                        .Select(i => new CartItem { ProductId = i.ProductId, Quantity = i.Quantity })
                        .ToList();

                    var stockCheck = await DeductStockAsync(cartItems);
                    if (!stockCheck.success)
                    {
                        order.OrderStatus = "PaymentFailed";
                        if (order.Payment != null) order.Payment.PaymentStatus = "Failed";
                        await _orderRepository.SaveChangesAsync();
                        return new VnpayHandleResult
                        {
                            IsSuccess = false,
                            Message = stockCheck.message,
                            ShouldClearCart = false
                        };
                    }
                }

                order.OrderStatus = "Pending";
                if (order.Payment != null)
                {
                    order.Payment.PaymentStatus = "Paid";
                    order.Payment.PaidAt = DateTime.Now;
                }
                await _orderRepository.SaveChangesAsync();

                return new VnpayHandleResult
                {
                    IsSuccess = true,
                    Message = isVi ? "Thanh toán VNPay thành công!" : "VNPay payment successful!",
                    ShouldClearCart = true
                };
            }

            if (order != null)
            {
                order.OrderStatus = "PaymentFailed";
                if (order.Payment != null) order.Payment.PaymentStatus = "Failed";
                await _orderRepository.SaveChangesAsync();
            }

            return new VnpayHandleResult
            {
                IsSuccess = false,
                Message = isVi ? "Thanh toán VNPay thất bại!" : "VNPay payment failed!",
                ShouldClearCart = false
            };
        }

        public async Task<VnpayIpnResult> HandleVnpayIpnAsync(Dictionary<string, string> vnpParams, string secureHash)
        {
            if (!VerifyVnpaySignature(vnpParams, secureHash))
            {
                return new VnpayIpnResult { RspCode = "97", Message = "Invalid signature" };
            }

            var txnRef = vnpParams.GetValueOrDefault("vnp_TxnRef", string.Empty);
            var responseCode = vnpParams.GetValueOrDefault("vnp_ResponseCode", string.Empty);
            var transactionStatus = vnpParams.GetValueOrDefault("vnp_TransactionStatus", string.Empty);
            var amountRaw = vnpParams.GetValueOrDefault("vnp_Amount", "0");

            var order = await _orderRepository.GetOrderByCodeWithPaymentAndItemsAsync(txnRef);
            if (order == null)
            {
                return new VnpayIpnResult { RspCode = "01", Message = "Order not found" };
            }

            if (!long.TryParse(amountRaw, out var vnpAmount))
            {
                return new VnpayIpnResult { RspCode = "04", Message = "invalid amount" };
            }

            var expectedAmount = (long)(order.TotalAmount * 100);
            if (vnpAmount != expectedAmount)
            {
                return new VnpayIpnResult { RspCode = "04", Message = "invalid amount" };
            }

            if (order.Payment?.PaymentStatus == "Paid")
            {
                return new VnpayIpnResult { RspCode = "02", Message = "Order already confirmed" };
            }

            if (responseCode == "00" && transactionStatus == "00")
            {
                var cartItems = order.OrderItems
                    .Select(i => new CartItem { ProductId = i.ProductId, Quantity = i.Quantity })
                    .ToList();

                var stockCheck = await DeductStockAsync(cartItems);
                if (!stockCheck.success)
                {
                    order.OrderStatus = "PaymentFailed";
                    if (order.Payment != null) order.Payment.PaymentStatus = "Failed";
                    await _orderRepository.SaveChangesAsync();
                    return new VnpayIpnResult { RspCode = "99", Message = "Unknow error" };
                }

                order.OrderStatus = "Pending";
                if (order.Payment != null)
                {
                    order.Payment.PaymentStatus = "Paid";
                    order.Payment.PaidAt = DateTime.Now;
                }
            }
            else
            {
                order.OrderStatus = "PaymentFailed";
                if (order.Payment != null) order.Payment.PaymentStatus = "Failed";
            }

            await _orderRepository.SaveChangesAsync();
            return new VnpayIpnResult { RspCode = "00", Message = "Confirm Success" };
        }

        public Task<List<Order>> GetMyOrdersAsync(int userId)
        {
            return _orderRepository.GetOrdersByUserIdAsync(userId);
        }

        public async Task<OrderActionResult> CancelOrderAsync(int userId, int orderId)
        {
            var order = await _orderRepository.GetOrderByUserAsync(orderId, userId);
            if (order == null)
                return new OrderActionResult { Success = false, Message = "Không tìm thấy đơn hàng!" };

            if (order.OrderStatus != "Pending")
                return new OrderActionResult { Success = false, Message = "Chỉ có thể hủy đơn hàng ở trạng thái chưa xử lý!" };

            order.OrderStatus = "Cancelled";
            await _orderRepository.SaveChangesAsync();
            return new OrderActionResult { Success = true };
        }

        public Task<Order?> GetOrderDetailAsync(int userId, int orderId)
        {
            return _orderRepository.GetOrderDetailByUserAsync(orderId, userId);
        }

        private string BuildVnpayUrl(string orderCode, decimal amount, string returnUrl, string ipAddress)
        {
            var vnpHashSecret = _config["VNPay:HashSecret"] ?? "";
            var vnpTmnCode = _config["VNPay:TmnCode"] ?? "";
            var vnpUrl = _config["VNPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var configuredReturnUrl = _config["VNPay:ReturnUrl"];
            var finalReturnUrl = string.IsNullOrWhiteSpace(configuredReturnUrl) ? returnUrl : configuredReturnUrl;

            var now = DateTime.Now;

            var vnpParams = new SortedDictionary<string, string>
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = vnpTmnCode,
                ["vnp_Amount"] = ((long)(amount * 100)).ToString(),
                ["vnp_CurrCode"] = "VND",
                ["vnp_TxnRef"] = orderCode,
                ["vnp_OrderInfo"] = $"Thanh toan don hang {orderCode}",
                ["vnp_OrderType"] = "other",
                ["vnp_Locale"] = "vn",
                ["vnp_ReturnUrl"] = finalReturnUrl,
                ["vnp_IpAddr"] = ipAddress,
                ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss"),
                ["vnp_ExpireDate"] = now.AddMinutes(15).ToString("yyyyMMddHHmmss")
            };

            var signData = BuildVnpData(vnpParams);
            var secureHash = HmacSha512(vnpHashSecret, signData);
            return $"{vnpUrl}?{signData}&vnp_SecureHash={secureHash}";
        }

        private bool VerifyVnpaySignature(Dictionary<string, string> vnpParams, string secureHash)
        {
            var vnpHashSecret = _config["VNPay:HashSecret"] ?? "";

            var sorted = new SortedDictionary<string, string>(vnpParams);

            sorted.Remove("vnp_SecureHash");
            sorted.Remove("vnp_SecureHashType");

            var signData = BuildVnpData(sorted);

            var checkHash = HmacSha512(vnpHashSecret, signData);

            return checkHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildVnpData(SortedDictionary<string, string> data)
        {
            return string.Join("&", data
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{VnpayEncode(kv.Key)}={VnpayEncode(kv.Value)}"));
        }

        private static string VnpayEncode(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return Uri.EscapeDataString(value).Replace("%20", "+");
        }

        private static string HmacSha512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }

        private async Task<(bool success, string message)> DeductStockAsync(List<CartItem> cartItems)
        {
            var productIds = cartItems.Select(i => i.ProductId).Distinct().ToList();
            var products = await _orderRepository.GetProductsByIdsAsync(productIds);

            foreach (var item in cartItems)
            {
                if (!products.TryGetValue(item.ProductId, out var product))
                    return (false, "Sản phẩm không tồn tại.");

                var remaining = product.TotalStock - product.SoldCount;
                if (item.Quantity > remaining)
                    return (false, $"Sản phẩm '{product.Name}' không đủ tồn kho.");
            }

            foreach (var item in cartItems)
            {
                products[item.ProductId].SoldCount += item.Quantity;
            }

            return (true, string.Empty);
        }
    }
}