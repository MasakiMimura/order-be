using OrderBE.DTOs;

namespace OrderBE.Service
{
    /// <summary>
    /// IProductService（商品サービスインターフェース）
    /// 商品・カテゴリ取得のビジネスロジック層の契約を定義
    /// Controller層の単体テストでMock化を可能にする
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// 商品一覧とカテゴリ一覧を取得
        /// キャンペーン商品の割引後価格を計算して返却
        /// </summary>
        /// <param name="categoryId">カテゴリID（nullの場合は全商品を取得）</param>
        /// <returns>商品一覧とカテゴリ一覧を含むレスポンスDTO</returns>
        Task<ProductsWithCategoriesResponse> GetProductsWithCategoriesAsync(int? categoryId);
    }
}
