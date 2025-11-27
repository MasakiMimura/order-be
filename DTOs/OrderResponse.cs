namespace OrderBE.DTOs
{
    /// <summary>
    /// 注文レスポンスDTO（基本情報）
    /// </summary>
    public class OrderResponse
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
        public bool? Confirmed { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public string? PaymentMethod { get; set; }
        public decimal? PointsUsed { get; set; }
        public decimal? MemberNewBalance { get; set; }
        public DateTime? PaidAt { get; set; }
        public bool? Paid { get; set; }
    }

    /// <summary>
    /// 注文アイテムレスポンスDTO
    /// </summary>
    public class OrderItemResponse
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public decimal ProductDiscountPercent { get; set; }
        public int Quantity { get; set; }
    }
}
