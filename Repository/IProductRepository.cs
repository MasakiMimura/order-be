using OrderBE.Models;

namespace OrderBE.Repository
{
    /// <summary>
    /// IProductRepository（商品リポジトリインターフェース）
    /// 商品データアクセス層のRead操作の契約を定義
    /// Service層の単体テストでMock化を可能にする
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// 全商品を取得
        /// カテゴリ情報を含めて返却
        /// </summary>
        /// <returns>商品のリスト</returns>
        Task<IEnumerable<Product>> GetAllProductsAsync();

        /// <summary>
        /// カテゴリIDで商品を取得
        /// 指定されたカテゴリに属する商品のみを返却
        /// </summary>
        /// <param name="categoryId">カテゴリID</param>
        /// <returns>該当カテゴリの商品リスト</returns>
        Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId);

        /// <summary>
        /// アクティブな商品のみを取得
        /// is_active=trueの商品を返却
        /// </summary>
        /// <returns>アクティブな商品のリスト</returns>
        Task<IEnumerable<Product>> GetActiveProductsAsync();
    }
}
