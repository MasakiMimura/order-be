using OrderBE.Models;

namespace OrderBE.Repository
{
    /// <summary>
    /// IOrderRepository（注文リポジトリインターフェース）
    /// データアクセス層のCRUD操作の契約を定義
    /// Service層の単体テストでMock化を可能にする
    /// </summary>
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetOrdersByStatusAsync(string status);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int orderId);
    }
}
