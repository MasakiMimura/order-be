using OrderBE.Models;
using OrderBE.Data;
using OrderBE.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace OrderBE.Repository
{
    /// <summary>
    /// 注文（Order）エンティティのデータアクセスを担当するリポジトリクラス
    /// CRUD操作（作成、取得、更新、削除）を提供し、包括的な例外処理とロギングを実装
    /// IOrderRepositoryインターフェースを実装し、Service層からのMock化を可能にする
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;
        private readonly ILogger<OrderRepository> _logger;

        /// <summary>
        /// OrderRepositoryのコンストラクタ
        /// </summary>
        /// <param name="context">Entity Framework CoreのDbContext</param>
        /// <param name="logger">ログ出力用のILogger</param>
        public OrderRepository(OrderDbContext context, ILogger<OrderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 新しい注文を作成
        /// 注文明細（OrderItem）も一緒にInsertされる（ナビゲーションプロパティ）
        ///
        /// 処理内容:
        /// 1. Orderエンティティを_context.Orders.Add()でトラッキング
        /// 2. SaveChangesAsync()でINSERT実行
        /// 3. OrderIdが自動採番され、OrderItemも同時にINSERTされる
        /// </summary>
        /// <param name="order">作成する注文オブジェクト（OrderIdは自動採番）</param>
        /// <returns>作成された注文（OrderIdが設定済み）</returns>
        /// <exception cref="InvalidOperationException">データベース制約違反の場合</exception>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            try
            {
                await _context.SaveChangesAsync();
                return order;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Insert failed due to update issue.");
                throw new InvalidOperationException("Insert failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during insert.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during insert.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during insert.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during insert (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during insert.");
                throw new Exception($"Repository error during insert: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 注文IDで注文を取得
        /// Include()で注文明細（OrderItem）も一緒に取得される
        ///
        /// 処理内容:
        /// 1. _context.Orders.Include(o => o.Items)でOrderItemも同時取得
        /// 2. FirstOrDefaultAsync()でorder_id検索
        /// 3. 見つからない場合、EntityNotFoundExceptionスロー
        /// </summary>
        /// <param name="orderId">取得する注文のID</param>
        /// <returns>取得した注文（注文明細を含む）</returns>
        /// <exception cref="EntityNotFoundException">指定したIDの注文が見つからない場合</exception>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<Order> GetOrderByIdAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    throw new EntityNotFoundException($"Order with ID {orderId} not found.");

                return order;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during order retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during order retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during order retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during order retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during order retrieval.");
                throw new Exception($"Repository error during order retrieval: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 指定したステータスの注文リストを取得
        /// Include()で注文明細（OrderItem）も一緒に取得される
        ///
        /// 処理内容:
        /// 1. _context.Orders.Include(o => o.Items)でOrderItemも同時取得
        /// 2. Where(o => o.Status == status)でステータスフィルタリング
        /// 3. ToListAsync()でリスト化（空リストの場合も正常終了）
        /// </summary>
        /// <param name="status">取得する注文のステータス（例: "IN_ORDER", "CONFIRMED", "PAID"）</param>
        /// <returns>指定したステータスの注文リスト（空のリストもあり得る）</returns>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<List<Order>> GetOrdersByStatusAsync(string status)
        {
            try
            {
                var orders = await _context.Orders
                    .Include(o => o.Items)
                    .Where(o => o.Status == status)
                    .ToListAsync();

                return orders;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during orders retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during orders retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during orders retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during orders retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during orders retrieval.");
                throw new Exception($"Repository error during orders retrieval: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 既存の注文を更新
        /// 注文ヘッダー情報（Total、Status、MemberCardNo）の更新と、
        /// 注文明細（OrderItem）の追加・削除・更新を同時に処理
        ///
        /// 処理内容:
        /// 1. 既存の注文をDBから取得（Include(o => o.Items)で明細も取得）
        /// 2. 注文ヘッダー情報を更新（Total、Status、MemberCardNoなど）
        /// 3. 注文明細を更新:
        ///    - 既存の明細で、更新後のリストにないものは削除
        ///    - 更新後のリストにあるものは、既存なら更新、新規なら追加
        /// 4. SaveChangesAsync()で一括保存
        /// </summary>
        /// <param name="order">更新する注文オブジェクト（OrderIdは既存のもの、Itemsコレクションも含む）</param>
        /// <returns>更新された注文</returns>
        /// <exception cref="EntityNotFoundException">指定したIDの注文が見つからない場合</exception>
        /// <exception cref="InvalidOperationException">データベース制約違反の場合</exception>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<Order> UpdateOrderAsync(Order order)
        {
            try
            {
                // 既存の注文を取得（OrderItemをInclude）
                var existingOrder = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                if (existingOrder == null)
                    throw new EntityNotFoundException($"Order with ID {order.OrderId} not found.");

                // 注文ヘッダー情報を更新
                existingOrder.Total = order.Total;
                existingOrder.Status = order.Status;
                existingOrder.MemberCardNo = order.MemberCardNo;
                existingOrder.CreatedAt = order.CreatedAt;
                existingOrder.Confirmed = order.Confirmed;
                existingOrder.ConfirmedAt = order.ConfirmedAt;
                existingOrder.PaymentMethod = order.PaymentMethod;
                existingOrder.PointsUsed = order.PointsUsed;
                existingOrder.MemberNewBalance = order.MemberNewBalance;
                existingOrder.PaidAt = order.PaidAt;
                existingOrder.Paid = order.Paid;

                // 注文明細を更新（UpdateChildCollectionパターン）
                UpdateOrderItems(existingOrder, order.Items);

                await _context.SaveChangesAsync();
                return existingOrder;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Update failed due to update issue.");
                throw new InvalidOperationException("Update failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during update.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during update.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during update.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during update (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during update.");
                throw new Exception($"Repository error during update: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 注文明細コレクションを更新（追加・削除・更新を処理）
        /// UpdateChildCollectionパターンを使用
        ///
        /// 処理ロジック:
        /// 1. 既存の明細と新しい明細のスナップショットを取得（コレクション変更時のエラー回避）
        /// 2. 既存の明細で、新しいリストにないものを削除
        /// 3. 新しいリストの各明細について:
        ///    - OrderItemId > 0なら既存明細として更新
        ///    - OrderItemId == 0なら新規明細として追加
        /// </summary>
        /// <param name="existingOrder">既存の注文（DBから取得済み）</param>
        /// <param name="newItems">更新後の注文明細リスト</param>
        private void UpdateOrderItems(Order existingOrder, List<OrderItem> newItems)
        {
            // 既存のアイテムと新しいアイテムのスナップショットを取得
            // （Entity Frameworkのトラッキングコレクションが同一参照の場合のエラーを回避）
            var existingItemsList = existingOrder.Items.ToList();
            var newItemsList = newItems.ToList();

            // 削除対象: 既存の明細で、新しいリストにないもの
            var itemsToRemove = existingItemsList
                .Where(existingItem => !newItemsList.Any(newItem => newItem.OrderItemId == existingItem.OrderItemId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                existingOrder.Items.Remove(item);
            }

            // 追加・更新
            foreach (var newItem in newItemsList)
            {
                var existingItem = existingItemsList
                    .FirstOrDefault(i => i.OrderItemId == newItem.OrderItemId && newItem.OrderItemId > 0);

                if (existingItem != null)
                {
                    // 既存明細を更新
                    existingItem.ProductId = newItem.ProductId;
                    existingItem.ProductName = newItem.ProductName;
                    existingItem.ProductPrice = newItem.ProductPrice;
                    existingItem.ProductDiscountPercent = newItem.ProductDiscountPercent;
                    existingItem.Quantity = newItem.Quantity;
                }
                else
                {
                    // 新規明細を追加
                    newItem.OrderId = existingOrder.OrderId;
                    existingOrder.Items.Add(newItem);
                }
            }
        }

        /// <summary>
        /// 注文を削除
        /// Include()で注文明細を取得し、CASCADE DELETEで注文明細も一緒に削除される
        ///
        /// 処理内容:
        /// 1. 既存の注文を取得（Include(o => o.Items)で明細も取得）
        /// 2. 見つからない場合、EntityNotFoundExceptionスロー
        /// 3. _context.Orders.Remove()で削除マーク
        /// 4. SaveChangesAsync()でDELETE実行（OrderItemも自動削除）
        /// </summary>
        /// <param name="orderId">削除する注文のID</param>
        /// <exception cref="EntityNotFoundException">指定したIDの注文が見つからない場合</exception>
        /// <exception cref="InvalidOperationException">データベース制約違反の場合</exception>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task DeleteOrderAsync(int orderId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.Items)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                    throw new EntityNotFoundException($"Order with ID {orderId} not found.");

                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Delete failed due to update issue.");
                throw new InvalidOperationException("Delete failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during delete.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during delete.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during delete.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during delete (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during delete.");
                throw new Exception($"Repository error during delete: {ex.Message}", ex);
            }
        }
    }
}
