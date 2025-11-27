using Candidate_BE.Models;
using Candidate_BE.DTOs;
using Candidate_BE.Repository;
using Candidate_BE.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace Candidate_BE.Service
{
    public class CandidateService
    {
        private readonly CandidateRepository _repository;

        public CandidateService(CandidateRepository repository)
        {
            _repository = repository;
        }

        public async Task<CandidateDto> AddCandidateAsync(Candidate entity)
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

        public async Task<CandidateDto> UpdateCandidateAsync(Candidate entity)
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

        public async Task<CandidateDto?> GetCandidateByIdAsync(int id)
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
                throw new Exception($"Service error during candidate retrieval: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<CandidateDto>> GetAllCandidatesAsync()
        {
            try
            {
                var entities = await _repository.GetAllCandidatesAsync();
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
                throw new Exception($"Service error during candidates retrieval: {ex.Message}", ex);
            }
        }

        private CandidateDto MapEntityToDto(Candidate entity)
        {
            return new CandidateDto
            {
                Id = entity.Id,
                Type = entity.Type,
                YearsExperience = entity.YearsExperience,
                Coding = entity.Coding,
                DetailedDesign = entity.DetailedDesign,
                Instructor = entity.Instructor,
                IntegrationTest = entity.IntegrationTest,
                Leader = entity.Leader,
                Maintenance = entity.Maintenance,
                Operation = entity.Operation,
                OverallDesign = entity.OverallDesign,
                ProjectManager = entity.ProjectManager,
                ScrumMaster = entity.ScrumMaster,
                Specification = entity.Specification,
                UnitTest = entity.UnitTest,
                UiuxReal = entity.UiuxReal,
                RequirementsReal = entity.RequirementsReal,
                IsApproved = entity.IsApproved,
                JobTitle = entity.JobTitle,
                CloudServices = entity.Clouds?.Select(c => c.Name).ToList(),
                Databases = entity.Databases?.Select(d => d.Name).ToList(),
                BackendFrameworks = entity.FrameworksBackend?.Select(f => f.Name).ToList(),
                FrontendFrameworks = entity.FrameworksFrontend?.Select(f => f.Name).ToList(),
                OperatingSystems = entity.OS?.Select(o => o.Name).ToList(),
                ProgrammingLanguages = entity.ProgrammingLanguages?.Select(p => p.Name).ToList()
            };
        }
    }
}
