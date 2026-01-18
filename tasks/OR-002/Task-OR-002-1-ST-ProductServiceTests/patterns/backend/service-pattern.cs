using {ProjectName}.Models;
using {ProjectName}.DTOs;
using {ProjectName}.Repository;
using {ProjectName}.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace {ProjectName}.Service
{
    /// <summary>
    /// {EntityName}Service
    /// {EntityName}エンティティのビジネスロジック層
    /// Repository層とController層の仲介、ビジネスルール実装を担当
    /// </summary>
    public class {EntityName}Service
    {
        private readonly {EntityName}Repository _repository;

        public {EntityName}Service({EntityName}Repository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 新規{EntityName}を追加
        /// </summary>
        /// <param name="entity">{EntityName}エンティティ</param>
        /// <returns>{EntityName}DTO</returns>
        public async Task<{EntityName}Dto> Add{EntityName}Async({EntityName} entity)
        {
            try
            {
                var inserted = await _repository.InsertAsync(entity);
                return MapEntityToDto(inserted);
            }
            catch (InvalidOperationException)
            {
                // Repository層で変換されたビジネスロジック例外をそのまま再スロー
                // Controller層でBad Request (400)として処理される
                throw;
            }
            catch (NpgsqlException)
            {
                // データベース接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (SocketException)
            {
                // ネットワーク接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (TimeoutException)
            {
                // タイムアウトエラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (DbUpdateException)
            {
                // EF Coreのデータベース更新例外をそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップしてService層の文脈情報を追加
                throw new Exception($"Service error during insert: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// {EntityName}を更新
        /// </summary>
        /// <param name="entity">{EntityName}エンティティ</param>
        /// <returns>{EntityName}DTO</returns>
        public async Task<{EntityName}Dto> Update{EntityName}Async({EntityName} entity)
        {
            try
            {
                var updated = await _repository.UpdateAsync(entity);
                return MapEntityToDto(updated);
            }
            catch (EntityNotFoundException)
            {
                // Repository層でスローされたエンティティ未発見例外をそのまま再スロー
                // Controller層でNot Found (404)として処理される
                throw;
            }
            catch (InvalidOperationException)
            {
                // Repository層で変換されたビジネスロジック例外をそのまま再スロー
                // Controller層でBad Request (400)として処理される
                throw;
            }
            catch (NpgsqlException)
            {
                // データベース接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (SocketException)
            {
                // ネットワーク接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (TimeoutException)
            {
                // タイムアウトエラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (DbUpdateException)
            {
                // EF Coreのデータベース更新例外をそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップしてService層の文脈情報を追加
                throw new Exception($"Service error during update: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// IDで{EntityName}を取得
        /// </summary>
        /// <param name="id">{EntityName}ID</param>
        /// <returns>{EntityName}DTO</returns>
        public async Task<{EntityName}Dto?> Get{EntityName}ByIdAsync(int id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                return MapEntityToDto(entity);
            }
            catch (EntityNotFoundException)
            {
                // Repository層でスローされたエンティティ未発見例外をそのまま再スロー
                // Controller層でNot Found (404)として処理される
                throw;
            }
            catch (NpgsqlException)
            {
                // データベース接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (SocketException)
            {
                // ネットワーク接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (TimeoutException)
            {
                // タイムアウトエラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (DbUpdateException)
            {
                // EF Coreのデータベース更新例外をそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップしてService層の文脈情報を追加
                throw new Exception($"Service error during {entity-name} retrieval: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 全{EntityName}を取得
        /// </summary>
        /// <returns>{EntityName}DTOのリスト</returns>
        public async Task<IEnumerable<{EntityName}Dto>> GetAll{EntityPluralName}Async()
        {
            try
            {
                var entities = await _repository.GetAllAsync();
                return entities.Select(MapEntityToDto);
            }
            catch (EntityNotFoundException)
            {
                // Repository層でスローされたエンティティ未発見例外をそのまま再スロー
                // Controller層でNot Found (404)として処理される
                throw;
            }
            catch (NpgsqlException)
            {
                // データベース接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (SocketException)
            {
                // ネットワーク接続エラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (TimeoutException)
            {
                // タイムアウトエラーをそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (DbUpdateException)
            {
                // EF Coreのデータベース更新例外をそのまま再スロー
                // Controller層でService Unavailable (503)として処理される
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップしてService層の文脈情報を追加
                throw new Exception($"Service error during {entity-plural-name} retrieval: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Entity を DTO にマッピング
        /// </summary>
        /// <param name="entity">{EntityName}エンティティ</param>
        /// <returns>{EntityName}DTO</returns>
        private {EntityName}Dto MapEntityToDto({EntityName} entity)
        {
            return new {EntityName}Dto
            {
                {EntityName}Id = entity.{EntityName}Id,
                // Map additional properties
                // {PropertyName} = entity.{PropertyName},

                // For related entities (if applicable):
                // RelatedItems = entity.RelatedEntities?.Select(r => r.Name).ToList()
            };
        }
    }
}
