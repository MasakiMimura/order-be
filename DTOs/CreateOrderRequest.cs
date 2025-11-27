namespace OrderBE.DTOs
{
    /// <summary>
    /// 注文作成リクエストDTO
    /// POST /api/v1/orders のリクエストボディ
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>
        /// 会員カード番号（ゲスト注文の場合はnull）
        /// </summary>
        public string? MemberCardNo { get; set; }
    }
}
