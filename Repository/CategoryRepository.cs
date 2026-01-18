using OrderBE.Models;
using OrderBE.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace OrderBE.Repository
{
    /// <summary>
    /// カテゴリ（Category）エンティティのデータアクセスを担当するリポジトリクラス
    /// Read操作（取得）を提供し、包括的な例外処理とロギングを実装
    /// ICategoryRepositoryインターフェースを実装し、Service層からのMock化を可能にする
    /// </summary>
    public class CategoryRepository : ICategoryRepository
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<CategoryRepository> _logger;

        /// <summary>
        /// CategoryRepositoryのコンストラクタ
        /// </summary>
        /// <param name="context">Entity Framework CoreのDbContext</param>
        /// <param name="logger">ログ出力用のILogger</param>
        public CategoryRepository(ProductDbContext context, ILogger<CategoryRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 全カテゴリを取得
        /// display_order順にソートして返却
        ///
        /// 処理内容:
        /// 1. _context.Categories.OrderBy(c => c.DisplayOrder)で表示順ソート
        /// 2. ToListAsync()でリスト化（空リストの場合も正常終了）
        /// </summary>
        /// <returns>全カテゴリのリスト（表示順でソート済み）</returns>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync();

                return categories;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during categories retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during categories retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during categories retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during categories retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during categories retrieval.");
                throw new Exception($"Repository error during categories retrieval: {ex.Message}", ex);
            }
        }
    }
}
