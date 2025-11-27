namespace OrderBE.DTOs
{
    /// <summary>
    /// 決済処理リクエストDTO
    /// PUT /api/v1/orders/{id}/pay のリクエストボディ
    /// </summary>
    public class PayOrderRequest
    {
        /// <summary>
        /// 支払い方法（"POINT"等）
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// 会員カード番号（オプション）
        /// </summary>
        public string? MemberCardNo { get; set; }

        /// <summary>
        /// ポイント取引ID（オプション）
        /// </summary>
        public string? PointTransactionId { get; set; }
    }
}
