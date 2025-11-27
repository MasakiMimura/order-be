namespace OrderBE.DTOs
{
    /// <summary>
    /// 注文アイテム追加リクエストDTO
    /// POST /api/v1/orders/{id}/items のリクエストボディ
    /// </summary>
    public class AddOrderItemRequest
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int Quantity { get; set; }
    }
}
