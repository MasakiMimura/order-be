using Candidate_BE.Models;
using Candidate_BE.Data;
using Candidate_BE.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace Candidate_BE.Repository
{
    public class CandidateRepository
    {
        private readonly SkillDbContext _context;
        private readonly ILogger<CandidateRepository> _logger;

        public CandidateRepository(SkillDbContext context, ILogger<CandidateRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Candidate> InsertAsync(Candidate candidate)
        {
            _context.Candidates.Add(candidate);
            try
            {
                await _context.SaveChangesAsync();
                return candidate;
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

        public async Task<Candidate> UpdateAsync(Candidate candidate)
        {
            Candidate existing;
            
            try
            {
                existing = await _context.Candidates
                    .Include(c => c.Clouds)
                    .Include(c => c.Databases)
                    .Include(c => c.FrameworksBackend)
                    .Include(c => c.FrameworksFrontend)
                    .Include(c => c.OS)
                    .Include(c => c.ProgrammingLanguages)
                    .FirstOrDefaultAsync(c => c.Id == candidate.Id);
            }
            catch (NpgsqlException ex)
            {
                // PostgreSQL固有の接続エラーはそのまま上位層に伝播
                // Controller層でService Unavailable (503)として処理される
                _logger.LogError(ex, "PostgreSQL connection error during candidate lookup.");
                throw;
            }
            catch (SocketException ex)
            {
                // ネットワーク接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "Network connection error during candidate lookup.");
                throw;
            }
            catch (TimeoutException ex)
            {
                // タイムアウトはそのまま上位層に伝播
                _logger.LogError(ex, "Database connection timeout during candidate lookup.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                // EF Coreの一時的障害や接続関連例外はそのまま上位層に伝播
                _logger.LogError(ex, "Database connection issue during candidate lookup (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップして文脈情報を追加
                _logger.LogError(ex, "Unexpected error during candidate lookup.");
                throw new Exception($"Repository error during candidate lookup: {ex.Message}", ex);
            }

            if (existing == null)
                throw new EntityNotFoundException($"Candidate with ID {candidate.Id} not found.");

            _context.Entry(existing).CurrentValues.SetValues(candidate);
            UpdateChildCollection(existing.Clouds, candidate.Clouds, c => c.Name);
            UpdateChildCollection(existing.Databases, candidate.Databases, d => d.Name);
            UpdateChildCollection(existing.FrameworksBackend, candidate.FrameworksBackend, f => f.Name);
            UpdateChildCollection(existing.FrameworksFrontend, candidate.FrameworksFrontend, f => f.Name);
            UpdateChildCollection(existing.OS, candidate.OS, o => o.Name);
            UpdateChildCollection(existing.ProgrammingLanguages, candidate.ProgrammingLanguages, p => p.Name);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // EF CoreのDbUpdateExceptionをビジネスロジック層向けの例外に変換
                // データベース制約違反 → 無効操作として抽象化
                _logger.LogWarning(ex, "Update failed due to database constraint.");
                throw new InvalidOperationException("Update failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                // PostgreSQL固有の接続エラーはそのまま上位層に伝播
                // Controller層でService Unavailable (503)として処理される
                _logger.LogError(ex, "PostgreSQL connection error during update.");
                throw;
            }
            catch (SocketException ex)
            {
                // ネットワーク接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "Network connection error during update.");
                throw;
            }
            catch (TimeoutException ex)
            {
                // タイムアウトはそのまま上位層に伝播
                _logger.LogError(ex, "Database connection timeout during update.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                // EF Coreの一時的障害や接続関連例外はそのまま上位層に伝播
                _logger.LogError(ex, "Database connection issue during update (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップして文脈情報を追加
                _logger.LogError(ex, "Unexpected error during update.");
                throw new Exception($"Repository error during update: {ex.Message}", ex);
            }
            return existing;
        }

        private void UpdateChildCollection<T>(List<T> existing, List<T> updated, Func<T, object> keySelector) where T : class
        {
            var toRemove = existing.Where(e => !updated.Any(u => keySelector(u).Equals(keySelector(e)))).ToList();
            var toAdd = updated.Where(u => !existing.Any(e => keySelector(e).Equals(keySelector(u)))).ToList();

            foreach (var item in toRemove) _context.Remove(item);
            foreach (var item in toAdd) existing.Add(item);
        }

        public async Task<Candidate?> GetByIdAsync(int id)
        {
            try
            {
                var candidate = await _context.Candidates
                    .Include(c => c.Clouds)
                    .Include(c => c.Databases)
                    .Include(c => c.FrameworksBackend)
                    .Include(c => c.FrameworksFrontend)
                    .Include(c => c.OS)
                    .Include(c => c.ProgrammingLanguages)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (candidate == null)
                    throw new EntityNotFoundException($"Candidate with ID {id} not found.");

                return candidate;
            }
            catch (EntityNotFoundException)
            {
                // エンティティ未発見例外はそのまま再スロー
                throw;
            }
            catch (NpgsqlException ex)
            {
                // PostgreSQL固有の接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "PostgreSQL connection error during candidate retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                // ネットワーク接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "Network connection error during candidate retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                // タイムアウトはそのまま上位層に伝播
                _logger.LogError(ex, "Database connection timeout during candidate retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                // EF Coreの一時的障害や接続関連例外はそのまま上位層に伝播
                _logger.LogError(ex, "Database connection issue during candidate retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップして文脈情報を追加
                _logger.LogError(ex, "Unexpected error during candidate retrieval.");
                throw new Exception($"Repository error during candidate retrieval: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Candidate>> GetAllCandidatesAsync()
        {
            try
            {
                var candidates = await _context.Candidates
                    .Include(c => c.Clouds)
                    .Include(c => c.Databases)
                    .Include(c => c.FrameworksBackend)
                    .Include(c => c.FrameworksFrontend)
                    .Include(c => c.OS)
                    .Include(c => c.ProgrammingLanguages)
                    .ToListAsync();

                if (!candidates.Any())
                    throw new EntityNotFoundException("No candidates found in the database.");

                return candidates;
            }
            catch (EntityNotFoundException)
            {
                // エンティティ未発見例外はそのまま再スロー
                throw;
            }
            catch (NpgsqlException ex)
            {
                // PostgreSQL固有の接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "PostgreSQL connection error during candidates retrieval.");
                throw;
            }
            catch (SocketException ex)
            {
                // ネットワーク接続エラーはそのまま上位層に伝播
                _logger.LogError(ex, "Network connection error during candidates retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                // タイムアウトはそのまま上位層に伝播
                _logger.LogError(ex, "Database connection timeout during candidates retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                // EF Coreの一時的障害や接続関連例外はそのまま上位層に伝播
                _logger.LogError(ex, "Database connection issue during candidates retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップして文脈情報を追加
                _logger.LogError(ex, "Unexpected error during candidates retrieval.");
                throw new Exception($"Repository error during candidates retrieval: {ex.Message}", ex);
            }
        }
    }
}
