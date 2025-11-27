using OrderBE.Models;
using OrderBE.Repository;
using OrderBE.Exceptions;
using Npgsql;
using System.Net.Sockets;

namespace OrderBE.Service
{
    /// <summary>
    /// OrderService（注文サービス）クラス
    /// ビジネスロジック層として、注文の作成・取得・更新・削除、合計金額計算を提供
    /// IOrderRepositoryインターフェースに依存し、依存性注入とテスト容易性を実現
    /// </summary>
    public class OrderService
    {
        private readonly IOrderRepository _repository;

        public OrderService(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            try
            {
                order.Status = "IN_ORDER";
                order.Total = await CalculateTotalAsync(order.Items);

                var createdOrder = await _repository.CreateOrderAsync(order);
                return createdOrder;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }

        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            try
            {
                var order = await _repository.GetOrderByIdAsync(orderId);
                return order;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }

        public async Task<List<Order>> GetOrdersByStatusAsync(string status)
        {
            try
            {
                var orders = await _repository.GetOrdersByStatusAsync(status);
                return orders;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }

        public async Task<Order> UpdateOrderStatusAsync(int orderId, string newStatus)
        {
            try
            {
                var order = await _repository.GetOrderByIdAsync(orderId);
                order.Status = newStatus;

                var updatedOrder = await _repository.UpdateOrderAsync(order);
                return updatedOrder;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }

        public async Task<decimal> CalculateTotalAsync(List<OrderItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            decimal total = 0;

            foreach (var item in items)
            {
                var discountPercent = item.ProductDiscountPercent ?? 0;
                var itemTotal = item.Quantity * item.ProductPrice * (1 - discountPercent / 100);
                total += itemTotal;
            }

            return await Task.FromResult(Math.Round(total, 2));
        }

        public async Task<Order> AddOrderItemAsync(int orderId, int productId, int quantity)
        {
            try
            {
                var order = await _repository.GetOrderByIdAsync(orderId);

                // 外部Product Serviceから商品情報を取得（簡易実装として固定値を使用）
                var productName = $"Product {productId}";
                var productPrice = 300.00m;
                var productDiscountPercent = 0m;

                var newItem = new OrderItem
                {
                    OrderId = orderId,
                    ProductId = productId,
                    ProductName = productName,
                    ProductPrice = productPrice,
                    ProductDiscountPercent = productDiscountPercent,
                    Quantity = quantity
                };

                order.Items.Add(newItem);
                order.Total = await CalculateTotalAsync(order.Items);

                var updatedOrder = await _repository.UpdateOrderAsync(order);
                return updatedOrder;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }

        public async Task<Order> ConfirmOrderAsync(int orderId)
        {
            try
            {
                var order = await _repository.GetOrderByIdAsync(orderId);

                if (order.Status == "CONFIRMED" || order.Status == "PAID")
                {
                    throw new InvalidOperationException("Order is already confirmed or paid");
                }

                order.Status = "CONFIRMED";
                order.Confirmed = true;
                order.ConfirmedAt = DateTime.UtcNow;

                var updatedOrder = await _repository.UpdateOrderAsync(order);
                return updatedOrder;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }

        public async Task<Order> PayOrderAsync(int orderId, string paymentMethod, string? memberCardNo, string? pointTransactionId)
        {
            try
            {
                var order = await _repository.GetOrderByIdAsync(orderId);

                if (order.Status == "PAID")
                {
                    throw new InvalidOperationException("Order is already paid");
                }

                if (order.Status != "CONFIRMED")
                {
                    throw new InvalidOperationException("Order must be confirmed before payment");
                }

                // 外部Member Serviceからポイント減算を実行（簡易実装として固定値を使用）
                var pointsUsed = order.Total;
                var memberNewBalance = 250m; // 仮の新残高

                order.Status = "PAID";
                order.PaymentMethod = paymentMethod;
                order.PointsUsed = pointsUsed;
                order.MemberNewBalance = memberNewBalance;
                order.PaidAt = DateTime.UtcNow;
                order.Paid = true;
                order.MemberCardNo = memberCardNo;

                var updatedOrder = await _repository.UpdateOrderAsync(order);
                return updatedOrder;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (NpgsqlException)
            {
                throw;
            }
            catch (SocketException)
            {
                throw;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }
    }
}
