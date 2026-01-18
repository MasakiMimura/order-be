using Microsoft.AspNetCore.Mvc;
using {ProjectName}.Service;
using {ProjectName}.Models;
using {ProjectName}.DTOs;
using {ProjectName}.Exceptions;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace {ProjectName}.Controllers
{
    /// <summary>
    /// {EntityName}管理APIのControllerクラス
    /// {EndpointDescription}
    /// </summary>
    [ApiController]
    [Route("api/v1/{route_prefix}")]
    public class {EntityName}Controller : ControllerBase
    {
        private readonly {EntityName}Service _service;

        /// <summary>
        /// {EntityName}Controllerのコンストラクタ
        /// </summary>
        /// <param name="service">{EntityName}サービス（依存性注入）</param>
        public {EntityName}Controller({EntityName}Service service)
        {
            _service = service;
        }

        /// <summary>
        /// 新規{EntityName}を作成する（POST /api/v1/{route_prefix}）
        /// </summary>
        /// <param name="request">作成する{EntityName}リクエスト</param>
        /// <returns>作成された{EntityName}情報（HTTP 201 Created）</returns>
        /// <response code="201">{EntityName}作成成功（Created）</response>
        /// <response code="400">バリデーションエラー（Bad Request）</response>
        /// <response code="404">エンティティが見つからない（Not Found）</response>
        /// <response code="500">データベースエラー（Internal Server Error）</response>
        /// <response code="503">ネットワーク・タイムアウトエラー（Service Unavailable）</response>
        [HttpPost]
        public async Task<IActionResult> Create{EntityName}([FromBody] Create{EntityName}Request request)
        {
            try
            {
                // Create{EntityName}Requestから{EntityName}エンティティを作成
                var entity = new {EntityName}
                {
                    // Map request properties to entity
                    // Example: PropertyName = request.PropertyName
                };

                var created = await _service.Create{EntityName}Async(entity);
                return CreatedAtAction(nameof(Get{EntityName}ById), new { id = created.{EntityName}Id }, created);
            }
            catch (InvalidOperationException ex) when (ex.InnerException is DbUpdateException)
            {
                return BadRequest(new { message = "Invalid data provided", detail = ex.Message });
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (NpgsqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database service unavailable", detail = ex.Message });
            }
            catch (SocketException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Network connection unavailable", detail = ex.Message });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Request timeout", detail = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database update failed", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error", detail = ex.Message });
            }
        }

        /// <summary>
        /// 指定されたIDの{EntityName}を取得する（GET /api/v1/{route_prefix}/{id}）
        /// </summary>
        /// <param name="id">{EntityName}ID</param>
        /// <returns>{EntityName}情報（HTTP 200 OK）</returns>
        /// <response code="200">{EntityName}取得成功（OK）</response>
        /// <response code="404">{EntityName}が見つからない（Not Found）</response>
        /// <response code="500">データベースエラー（Internal Server Error）</response>
        /// <response code="503">ネットワーク・タイムアウトエラー（Service Unavailable）</response>
        [HttpGet("{id}")]
        public async Task<IActionResult> Get{EntityName}ById(int id)
        {
            try
            {
                var entity = await _service.Get{EntityName}ByIdAsync(id);
                return Ok(entity);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (NpgsqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database service unavailable", detail = ex.Message });
            }
            catch (SocketException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Network connection unavailable", detail = ex.Message });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Request timeout", detail = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database service unavailable", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error", detail = ex.Message });
            }
        }
    }
}
