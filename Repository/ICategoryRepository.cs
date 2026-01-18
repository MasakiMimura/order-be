using OrderBE.Models;

namespace OrderBE.Repository
{
    /// <summary>
    /// ICategoryRepository（カテゴリリポジトリインターフェース）
    /// カテゴリデータアクセス層のRead操作の契約を定義
    /// Service層の単体テストでMock化を可能にする
    /// </summary>
    public interface ICategoryRepository
    {
        /// <summary>
        /// 全カテゴリを取得
        /// 表示順序（display_order）でソートして返却
        /// </summary>
        /// <returns>カテゴリのリスト（表示順序でソート済み）</returns>
        Task<IEnumerable<Category>> GetAllCategoriesAsync();
    }
}
