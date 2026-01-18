namespace OrderBE.DTOs
{
    /// <summary>
    /// 商品一覧・カテゴリ一覧レスポンスDTO
    /// GET /api/v1/products エンドポイントのレスポンス形式
    /// </summary>
    public class ProductsWithCategoriesResponse
    {
        /// <summary>
        /// 商品一覧
        /// </summary>
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();

        /// <summary>
        /// カテゴリ一覧
        /// </summary>
        public List<CategoryDto> Categories { get; set; } = new List<CategoryDto>();
    }

    /// <summary>
    /// 商品DTO
    /// 商品情報とキャンペーン割引後価格を含む
    /// </summary>
    public class ProductDto
    {
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// 商品名
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// 価格（税込）
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// キャンペーン対象フラグ
        /// </summary>
        public bool IsCampaign { get; set; }

        /// <summary>
        /// キャンペーン割引率（%）
        /// </summary>
        public int CampaignDiscountPercent { get; set; }

        /// <summary>
        /// 割引後価格
        /// 計算式: price × (100 - campaign_discount_percent) / 100
        /// キャンペーン対象外の場合はpriceと同じ値
        /// </summary>
        public int DiscountedPrice { get; set; }

        /// <summary>
        /// カテゴリID
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// 有効フラグ
        /// </summary>
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// カテゴリDTO
    /// カテゴリマスタ情報
    /// </summary>
    public class CategoryDto
    {
        /// <summary>
        /// カテゴリID
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// カテゴリ名
        /// </summary>
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 表示順序
        /// </summary>
        public int DisplayOrder { get; set; }
    }
}
