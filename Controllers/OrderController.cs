using Microsoft.AspNetCore.Mvc;
using OrderBE.Models;
using OrderBE.Service;
using OrderBE.DTOs;
using OrderBE.Exceptions;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using System.Net.Sockets;

namespace OrderBE.Controllers
{
    /// <summary>
    /// Order管理APIのControllerクラス
    /// 注文作成、アイテム追加、注文確定、決済処理のエンドポイントを提供
    /// </summary>
    [ApiController]
    [Route("api/v1/orders")]
    public class OrderController : ControllerBase
    {
        private readonly OrderService _service;

        /// <summary>
        /// OrderControllerのコンストラクタ
        /// </summary>
        /// <param name="service">Orderサービス（依存性注入）</param>
        public OrderController(OrderService service)
        {
            _service = service;
        }

        /// <summary>
        /// 新規注文を作成する（POST /api/v1/orders）
        /// </summary>
        /// <param name="request">作成する注文リクエスト</param>
        /// <returns>作成された注文情報（HTTP 201 Created）</returns>
        /// <response code="201">注文作成成功（Created）</response>
        /// <response code="400">バリデーションエラー（Bad Request）</response>
        /// <response code="500">データベースエラー（Internal Server Error）</response>
        /// <response code="503">ネットワーク・タイムアウトエラー（Service Unavailable）</response>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var order = new Order
                {
                    MemberCardNo = request.MemberCardNo,
                    Items = new List<OrderItem>()
                };

                var created = await _service.CreateOrderAsync(order);

                var response = new OrderResponse
                {
                    OrderId = created.OrderId,
                    Status = created.Status,
                    Total = created.Total,
                    Items = created.Items.Select(item => new OrderItemResponse
                    {
                        OrderItemId = item.OrderItemId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductPrice = item.ProductPrice,
                        ProductDiscountPercent = item.ProductDiscountPercent ?? 0,
                        Quantity = item.Quantity
                    }).ToList()
                };

                return StatusCode(StatusCodes.Status201Created, response);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (SocketException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Network connection unavailable", detail = ex.Message });
            }
            catch (TimeoutException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Request timeout", detail = ex.Message });
            }
            catch (InvalidOperationException ex) when (ContainsTimeoutOrSocketException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database service unavailable", detail = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.InnerException is DbUpdateException)
            {
                return BadRequest(new { message = "Invalid data provided", detail = ex.Message });
            }
            catch (NpgsqlException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Database service unavailable", detail = ex.Message });
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
        /// 注文にアイテムを追加する（POST /api/v1/orders/{id}/items）
        /// </summary>
        /// <param name="id">注文ID</param>
        /// <param name="request">アイテム追加リクエスト</param>
        /// <returns>更新された注文情報（HTTP 200 OK）</returns>
        /// <response code="200">アイテム追加成功（OK）</response>
        /// <response code="400">バリデーションエラー（Bad Request）</response>
        /// <response code="404">注文が見つからない（Not Found）</response>
        /// <response code="500">データベースエラー（Internal Server Error）</response>
        /// <response code="503">ネットワーク・タイムアウトエラー（Service Unavailable）</response>
        [HttpPost("{id}/items")]
        public async Task<IActionResult> AddOrderItem(int id, [FromBody] AddOrderItemRequest request)
        {
            try
            {
                var updated = await _service.AddOrderItemAsync(id, request.ProductId, request.Quantity);

                var response = new OrderResponse
                {
                    OrderId = updated.OrderId,
                    Status = updated.Status,
                    Total = updated.Total,
                    Items = updated.Items.Select(item => new OrderItemResponse
                    {
                        OrderItemId = item.OrderItemId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        ProductPrice = item.ProductPrice,
                        ProductDiscountPercent = item.ProductDiscountPercent ?? 0,
                        Quantity = item.Quantity
                    }).ToList()
                };

                return Ok(response);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex) when (ex.InnerException is DbUpdateException)
            {
                return BadRequest(new { message = "Invalid data provided", detail = ex.Message });
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
        /// 注文を確定する（PUT /api/v1/orders/{id}/confirm）
        /// </summary>
        /// <param name="id">注文ID</param>
        /// <returns>確定された注文情報（HTTP 200 OK）</returns>
        /// <response code="200">注文確定成功（OK）</response>
        /// <response code="400">ビジネスロジックエラー（Bad Request）</response>
        /// <response code="404">注文が見つからない（Not Found）</response>
        /// <response code="500">データベースエラー（Internal Server Error）</response>
        /// <response code="503">ネットワーク・タイムアウトエラー（Service Unavailable）</response>
        [HttpPut("{id}/confirm")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            try
            {
                var updated = await _service.ConfirmOrderAsync(id);

                var response = new
                {
                    orderId = updated.OrderId,
                    status = updated.Status,
                    total = updated.Total,
                    confirmed = updated.Confirmed,
                    confirmedAt = updated.ConfirmedAt
                };

                return Ok(response);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "OrderConfirmationFailed", message = "注文確定に失敗しました", details = ex.Message });
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
        /// 決済処理を実行する（PUT /api/v1/orders/{id}/pay）
        /// </summary>
        /// <param name="id">注文ID</param>
        /// <param name="request">決済処理リクエスト</param>
        /// <returns>決済完了した注文情報（HTTP 200 OK）</returns>
        /// <response code="200">決済完了成功（OK）</response>
        /// <response code="400">ビジネスロジックエラー（Bad Request）</response>
        /// <response code="404">注文が見つからない（Not Found）</response>
        /// <response code="500">ポイント不足などのエラー（Internal Server Error）</response>
        /// <response code="503">ネットワーク・タイムアウトエラー（Service Unavailable）</response>
        [HttpPut("{id}/pay")]
        public async Task<IActionResult> PayOrder(int id, [FromBody] PayOrderRequest request)
        {
            try
            {
                var updated = await _service.PayOrderAsync(id, request.PaymentMethod, request.MemberCardNo, request.PointTransactionId);

                var response = new
                {
                    orderId = updated.OrderId,
                    status = updated.Status,
                    total = updated.Total,
                    paymentMethod = updated.PaymentMethod,
                    pointsUsed = updated.PointsUsed,
                    memberNewBalance = updated.MemberNewBalance,
                    paidAt = updated.PaidAt,
                    paid = updated.Paid
                };

                return Ok(response);
            }
            catch (EntityNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = "PaymentProcessingFailed", message = "決済処理に失敗しました", details = ex.Message });
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
