using OrderBE.Models;
using OrderBE.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace OrderBE.Repository
{
    /// <summary>
    /// 商品（Product）エンティティのデータアクセスを担当するリポジトリクラス
    /// Read操作（取得）を提供し、包括的な例外処理とロギングを実装
    /// IProductRepositoryインターフェースを実装し、Service層からのMock化を可能にする
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly ProductDbContext _context;
        private readonly ILogger<ProductRepository> _logger;

        /// <summary>
        /// ProductRepositoryのコンストラクタ
        /// </summary>
        /// <param name="context">Entity Framework CoreのDbContext</param>
        /// <param name="logger">ログ出力用のILogger</param>
        public ProductRepository(ProductDbContext context, ILogger<ProductRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 全商品を取得
        /// Include()でカテゴリ情報（Category）も一緒に取得される
        ///
        /// 処理内容:
        /// 1. _context.Products.Include(p => p.Category)でCategoryも同時取得
        /// 2. ToListAsync()でリスト化（空リストの場合も正常終了）
        /// </summary>
        /// <returns>全商品のリスト（カテゴリ情報を含む）</returns>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .ToListAsync();

                return products;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during products retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during products retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during products retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during products retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during products retrieval.");
                throw new Exception($"Repository error during products retrieval: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// カテゴリIDで商品を取得
        /// Include()でカテゴリ情報（Category）も一緒に取得される
        ///
        /// 処理内容:
        /// 1. _context.Products.Include(p => p.Category)でCategoryも同時取得
        /// 2. Where(p => p.CategoryId == categoryId)でカテゴリIDフィルタリング
        /// 3. ToListAsync()でリスト化（空リストの場合も正常終了）
        /// </summary>
        /// <param name="categoryId">取得する商品のカテゴリID</param>
        /// <returns>指定カテゴリの商品リスト（カテゴリ情報を含む）</returns>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.CategoryId == categoryId)
                    .ToListAsync();

                return products;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during products retrieval by category.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during products retrieval by category.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during products retrieval by category.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during products retrieval by category (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during products retrieval by category.");
                throw new Exception($"Repository error during products retrieval by category: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// アクティブな商品のみを取得
        /// Include()でカテゴリ情報（Category）も一緒に取得される
        ///
        /// 処理内容:
        /// 1. _context.Products.Include(p => p.Category)でCategoryも同時取得
        /// 2. Where(p => p.IsActive)でis_active=trueフィルタリング
        /// 3. ToListAsync()でリスト化（空リストの場合も正常終了）
        /// </summary>
        /// <returns>アクティブな商品のリスト（カテゴリ情報を含む）</returns>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<IEnumerable<Product>> GetActiveProductsAsync()
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive)
                    .ToListAsync();

                return products;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during active products retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during active products retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during active products retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during active products retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during active products retrieval.");
                throw new Exception($"Repository error during active products retrieval: {ex.Message}", ex);
            }
        }
    }
}
