using Microsoft.AspNetCore.Mvc;
using CoffeeShop.Models;
using CoffeeShop.Service;

namespace CoffeeShop.Controllers
{
    /// <summary>
    /// IT人材候補者コントローラー（CoffeeShop参考実装）
    /// このファイルはCoffeeShopプロジェクト固有の参考実装です。
    /// 新規プロジェクトでは、backend-controller/controller-pattern.cs の汎用パターンを使用してください。
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CandidateController : ControllerBase
    {
        private readonly ICandidateService _service;
        private readonly ILogger<CandidateController> _logger;

        public CandidateController(
            ICandidateService service,
            ILogger<CandidateController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// 候補者一覧取得
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllCandidates()
        {
            try
            {
                var candidates = await _service.GetAllCandidatesAsync();
                return Ok(new { candidates });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "候補者一覧取得エラー");
                return StatusCode(500, new
                {
                    message = "候補者一覧の取得に失敗しました",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// 候補者詳細取得
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCandidateById(int id)
        {
            try
            {
                var candidate = await _service.GetCandidateByIdAsync(id);
                return Ok(candidate);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new
                {
                    message = "候補者が見つかりません",
                    detail = ex.Message
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = "入力値が無効です",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "候補者取得エラー: ID={Id}", id);
                return StatusCode(500, new
                {
                    message = "候補者の取得に失敗しました",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// 候補者登録
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateCandidate([FromBody] CandidateCreateDto dto)
        {
            try
            {
                var candidate = await _service.CreateCandidateAsync(dto);
                return CreatedAtAction(
                    nameof(GetCandidateById),
                    new { id = candidate.CandidateId },
                    candidate
                );
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = "入力値が無効です",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "候補者登録エラー");
                return StatusCode(500, new
                {
                    message = "候補者の登録に失敗しました",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// 候補者更新
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCandidate(int id, [FromBody] CandidateUpdateDto dto)
        {
            try
            {
                var candidate = await _service.UpdateCandidateAsync(id, dto);
                return Ok(candidate);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new
                {
                    message = "候補者が見つかりません",
                    detail = ex.Message
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = "入力値が無効です",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "候補者更新エラー: ID={Id}", id);
                return StatusCode(500, new
                {
                    message = "候補者の更新に失敗しました",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// 候補者削除
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCandidate(int id)
        {
            try
            {
                var result = await _service.DeleteCandidateAsync(id);
                return NoContent();
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new
                {
                    message = "候補者が見つかりません",
                    detail = ex.Message
                });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = "入力値が無効です",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "候補者削除エラー: ID={Id}", id);
                return StatusCode(500, new
                {
                    message = "候補者の削除に失敗しました",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// スキル検索
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchCandidatesBySkill([FromQuery] string skill)
        {
            try
            {
                var candidates = await _service.SearchCandidatesBySkillAsync(skill);
                return Ok(new { candidates });
            }
            catch (ValidationException ex)
            {
                return BadRequest(new
                {
                    message = "入力値が無効です",
                    detail = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "スキル検索エラー: Skill={Skill}", skill);
                return StatusCode(500, new
                {
                    message = "候補者検索に失敗しました",
                    detail = ex.Message
                });
            }
        }
    }
}
