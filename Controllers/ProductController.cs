using Microsoft.AspNetCore.Mvc;
using OrderBE.Service;
using OrderBE.DTOs;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace OrderBE.Controllers
{
    /// <summary>
    /// Product管理APIのControllerクラス
    /// 商品・カテゴリ一覧取得のエンドポイントを提供
    /// </summary>
    [ApiController]
    [Route("api/v1/products")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _service;

        /// <summary>
        /// ProductControllerのコンストラクタ
        /// </summary>
        /// <param name="service">Productサービス（依存性注入）</param>
        public ProductController(IProductService service)
        {
            _service = service;
        }

        /// <summary>
        /// 商品一覧とカテゴリ一覧を取得する（GET /api/v1/products）
        /// categoryIdを指定した場合は該当カテゴリの商品のみ取得
        /// </summary>
        /// <param name="categoryId">カテゴリID（オプション、nullの場合は全商品を取得）</param>
        /// <returns>商品一覧とカテゴリ一覧（HTTP 200 OK）</returns>
        /// <response code="200">商品・カテゴリ取得成功（OK）</response>
        /// <response code="500">データベースエラー（Internal Server Error）</response>
        /// <response code="503">ネットワーク・タイムアウトエラー（Service Unavailable）</response>
        [HttpGet]
        public async Task<IActionResult> GetProducts([FromQuery] int? categoryId)
        {
            try
            {
                var result = await _service.GetProductsWithCategoriesAsync(categoryId);
                return Ok(result);
            }
            catch (SocketException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "ServiceUnavailable", message = "ネットワーク接続に問題が発生しました", detail = ex.Message });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "ServiceUnavailable", message = "リクエストがタイムアウトしました", detail = ex.Message });
            }
            catch (InvalidOperationException ex) when (ContainsTimeoutOrSocketException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "ServiceUnavailable", message = "データベースサービスに接続できません", detail = ex.Message });
            }
            catch (NpgsqlException ex)
            {
                // DB接続障害（ホスト未検出、接続タイムアウト等）は503 Service Unavailable
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "ServiceUnavailable", message = "データベースサービスに接続できません", detail = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "InternalServerError", message = "商品データの取得に失敗しました", detail = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "InternalServerError", message = "商品データの取得に失敗しました", detail = ex.Message });
            }
        }

        /// <summary>
        /// 例外チェーン内にTimeoutExceptionまたはSocketExceptionが含まれているかチェック
        /// DB接続エラーを適切に503 Service Unavailableで返すために使用
        /// </summary>
        /// <param name="ex">チェックする例外</param>
        /// <returns>TimeoutExceptionまたはSocketExceptionが含まれている場合true</returns>
        private static bool ContainsTimeoutOrSocketException(Exception ex)
        {
            var currentException = ex;
            while (currentException != null)
            {
                if (currentException is TimeoutException || currentException is SocketException)
                {
                    return true;
                }
                currentException = currentException.InnerException;
            }
            return false;
        }
    }
}
