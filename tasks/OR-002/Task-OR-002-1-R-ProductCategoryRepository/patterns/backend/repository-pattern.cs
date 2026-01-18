using {ProjectName}.Models;
using {ProjectName}.Data;
using {ProjectName}.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace {ProjectName}.Repository
{
    /// <summary>
    /// {EntityName}Repository
    /// {EntityName}エンティティのデータアクセス層
    /// データベースとの直接的なやり取りを担当
    /// </summary>
    public class {EntityName}Repository
    {
        private readonly {DbContextName} _context;
        private readonly ILogger<{EntityName}Repository> _logger;

        public {EntityName}Repository({DbContextName} context, ILogger<{EntityName}Repository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 新規{EntityName}を挿入
        /// </summary>
        /// <param name="entity">挿入する{EntityName}エンティティ</param>
        /// <returns>挿入された{EntityName}エンティティ</returns>
        public async Task<{EntityName}> InsertAsync({EntityName} entity)
        {
            _context.{EntityPluralName}.Add(entity);
            try
            {
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (DbUpdateException ex)
            {
                // EF CoreのDbUpdateExceptionをビジネスロジック層向けの例外に変換
                // データベース制約違反 → 無効操作として抽象化
                _logger.LogWarning(ex, "Insert failed due to update issue.");
                throw new InvalidOperationException("Insert failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                // PostgreSQL固有の接続エラーはそのまま上位層に伝播
                // Controller層でService Unavailable (503)として処理される
                _logger.LogError(ex, "PostgreSQL connection error during insert.");
                throw;
            }
            catch (SocketException ex)
            {
                // ネットワーク接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "Network connection error during insert.");
                throw;
            }
            catch (TimeoutException ex)
            {
                // タイムアウトはそのまま上位層に伝播
                _logger.LogError(ex, "Database connection timeout during insert.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                // EF Coreの一時的障害や接続関連例外はそのまま上位層に伝播
                _logger.LogError(ex, "Database connection issue during insert (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップして文脈情報を追加
                _logger.LogError(ex, "Unexpected error during insert.");
                throw new Exception($"Repository error during insert: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 既存{EntityName}を更新
        /// </summary>
        /// <param name="entity">更新する{EntityName}エンティティ</param>
        /// <returns>更新された{EntityName}エンティティ</returns>
        public async Task<{EntityName}> UpdateAsync({EntityName} entity)
        {
            {EntityName} existing;

            try
            {
                existing = await _context.{EntityPluralName}
                    // Add .Include() for related entities if needed
                    // .Include(e => e.RelatedEntity)
                    .FirstOrDefaultAsync(e => e.{EntityName}Id == entity.{EntityName}Id);
            }
            catch (NpgsqlException ex)
            {
                // PostgreSQL固有の接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "PostgreSQL connection error during {entity-name} lookup.");
                throw;
            }
            catch (SocketException ex)
            {
                // ネットワーク接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "Network connection error during {entity-name} lookup.");
                throw;
            }
            catch (TimeoutException ex)
            {
                // タイムアウトはそのまま上位層に伝播
                _logger.LogError(ex, "Database connection timeout during {entity-name} lookup.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                // EF Coreの一時的障害や接続関連例外はそのまま上位層に伝播
                _logger.LogError(ex, "Database connection issue during {entity-name} lookup (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップして文脈情報を追加
                _logger.LogError(ex, "Unexpected error during {entity-name} lookup.");
                throw new Exception($"Repository error during {entity-name} lookup: {ex.Message}", ex);
            }

            if (existing == null)
                throw new EntityNotFoundException($"{EntityName} with ID {entity.{EntityName}Id} not found.");

            _context.Entry(existing).CurrentValues.SetValues(entity);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // EF CoreのDbUpdateExceptionをビジネスロジック層向けの例外に変換
                _logger.LogWarning(ex, "Update failed due to database constraint.");
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
            return existing;
        }

        /// <summary>
        /// IDで{EntityName}を取得
        /// </summary>
        /// <param name="id">{EntityName}ID</param>
        /// <returns>{EntityName}エンティティ</returns>
        public async Task<{EntityName}?> GetByIdAsync(int id)
        {
            try
            {
                var entity = await _context.{EntityPluralName}
                    // Add .Include() for related entities if needed
                    // .Include(e => e.RelatedEntity)
                    .FirstOrDefaultAsync(e => e.{EntityName}Id == id);

                if (entity == null)
                    throw new EntityNotFoundException($"{EntityName} with ID {id} not found.");

                return entity;
            }
            catch (EntityNotFoundException)
            {
                // エンティティ未発見例外はそのまま再スロー
                throw;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during {entity-name} retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during {entity-name} retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during {entity-name} retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during {entity-name} retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {entity-name} retrieval.");
                throw new Exception($"Repository error during {entity-name} retrieval: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 全{EntityName}を取得
        /// </summary>
        /// <returns>{EntityName}エンティティのリスト</returns>
        public async Task<IEnumerable<{EntityName}>> GetAllAsync()
        {
            try
            {
                var entities = await _context.{EntityPluralName}
                    // Add .Include() for related entities if needed
                    // .Include(e => e.RelatedEntity)
                    .ToListAsync();

                if (!entities.Any())
                    throw new EntityNotFoundException("No {entity-plural-name} found in the database.");

                return entities;
            }
            catch (EntityNotFoundException)
            {
                // エンティティ未発見例外はそのまま再スロー
                throw;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during {entity-plural-name} retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during {entity-plural-name} retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during {entity-plural-name} retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during {entity-plural-name} retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during {entity-plural-name} retrieval.");
                throw new Exception($"Repository error during {entity-plural-name} retrieval: {ex.Message}", ex);
            }
        }
    }
}
