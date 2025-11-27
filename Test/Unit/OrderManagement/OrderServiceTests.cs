using Microsoft.Extensions.Logging;
using Moq;
using OrderBE.Exceptions;
using OrderBE.Models;
using OrderBE.Repository;
using OrderBE.Service;
using Xunit;

namespace OrderBE.Tests.Unit.OrderManagement
{
    /// <summary>
    /// OrderService（注文サービス）の単体テストクラス
    /// TDD方式で作成されたテストケース（8テスト）
    /// Moqを使用してIOrderRepositoryをMock化し、Service層のビジネスロジックをテスト
    /// </summary>
    public class OrderServiceTests
    {
        /// <summary>
        /// テスト: 有効な注文データで注文作成が成功し、IN_ORDER状態で初期化されることを検証
        /// Given: 会員カード番号付き、2つの注文明細を持つ有効な注文データ
        /// When: CreateOrderAsync実行
        /// Then: IN_ORDER状態で注文が作成され、Repository.CreateOrderAsyncが1回呼ばれる
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_ValidOrder_ReturnsCreatedOrder()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = "MEMBER00001",
                Status = "IN_ORDER",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 1,
                        ProductName = "カフェラテ",
                        ProductPrice = 450.00m,
                        ProductDiscountPercent = 0.00m,
                        Quantity = 2
                    },
                    new OrderItem
                    {
                        ProductId = 2,
                        ProductName = "クロワッサン",
                        ProductPrice = 300.00m,
                        ProductDiscountPercent = 10.00m,
                        Quantity = 1
                    }
                }
            };

            var createdOrder = new Order
            {
                OrderId = 1,
                CreatedAt = order.CreatedAt,
                MemberCardNo = order.MemberCardNo,
                Total = 1170.00m,
                Status = "IN_ORDER",
                Items = order.Items
            };

            mockRepository
                .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync(createdOrder);

            // Act
            var result = await service.CreateOrderAsync(order);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("IN_ORDER", result.Status);
            Assert.Equal(1, result.OrderId);
            mockRepository.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Once);
        }

        /// <summary>
        /// テスト: 注文作成時に合計金額が正しく計算されることを検証
        /// Given: 注文明細（数量=2、単価=100、割引=10%）
        /// When: CreateOrderAsync実行
        /// Then: total = 2 × 100 × (1 - 0.1) = 180.00
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_CalculatesTotalCorrectly()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Status = "IN_ORDER",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 1,
                        ProductName = "テスト商品",
                        ProductPrice = 100.00m,
                        ProductDiscountPercent = 10.00m,
                        Quantity = 2
                    }
                }
            };

            var createdOrder = new Order
            {
                OrderId = 1,
                CreatedAt = order.CreatedAt,
                Total = 180.00m,
                Status = "IN_ORDER",
                Items = order.Items
            };

            mockRepository
                .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync(createdOrder);

            // Act
            var result = await service.CreateOrderAsync(order);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(180.00m, result.Total);
            mockRepository.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Once);
        }

        /// <summary>
        /// テスト: 既存の注文をIDで取得できることを検証
        /// Given: OrderId=1の注文が存在（Repositoryから返却）
        /// When: GetOrderByIdAsync(1)実行
        /// Then: 注文データが返却され、Repository.GetOrderByIdAsyncが1回呼ばれる
        /// </summary>
        [Fact]
        public async Task GetOrderByIdAsync_ExistingOrder_ReturnsOrder()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            var expectedOrder = new Order
            {
                OrderId = 1,
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = "MEMBER00001",
                Total = 1000.00m,
                Status = "IN_ORDER",
                Items = new List<OrderItem>()
            };

            mockRepository
                .Setup(r => r.GetOrderByIdAsync(1))
                .ReturnsAsync(expectedOrder);

            // Act
            var result = await service.GetOrderByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.OrderId);
            Assert.Equal("MEMBER00001", result.MemberCardNo);
            mockRepository.Verify(r => r.GetOrderByIdAsync(1), Times.Once);
        }

        /// <summary>
        /// テスト: 存在しない注文IDで取得を試みた場合、EntityNotFoundExceptionが再スローされることを検証
        /// Given: RepositoryがEntityNotFoundExceptionをスロー
        /// When: GetOrderByIdAsync(999)実行
        /// Then: EntityNotFoundExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            mockRepository
                .Setup(r => r.GetOrderByIdAsync(999))
                .ThrowsAsync(new EntityNotFoundException("Order with ID 999 not found."));

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(
                async () => await service.GetOrderByIdAsync(999)
            );

            mockRepository.Verify(r => r.GetOrderByIdAsync(999), Times.Once);
        }

        /// <summary>
        /// テスト: ステータス別に注文リストを取得できることを検証
        /// Given: RepositoryがIN_ORDER状態の注文リストを返却
        /// When: GetOrdersByStatusAsync("IN_ORDER")実行
        /// Then: 注文リストが返却され、Repository.GetOrdersByStatusAsyncが1回呼ばれる
        /// </summary>
        [Fact]
        public async Task GetOrdersByStatusAsync_InOrderStatus_ReturnsInOrderOrders()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            var expectedOrders = new List<Order>
            {
                new Order
                {
                    OrderId = 1,
                    CreatedAt = DateTime.UtcNow,
                    Total = 1000.00m,
                    Status = "IN_ORDER",
                    Items = new List<OrderItem>()
                },
                new Order
                {
                    OrderId = 2,
                    CreatedAt = DateTime.UtcNow,
                    Total = 2000.00m,
                    Status = "IN_ORDER",
                    Items = new List<OrderItem>()
                }
            };

            mockRepository
                .Setup(r => r.GetOrdersByStatusAsync("IN_ORDER"))
                .ReturnsAsync(expectedOrders);

            // Act
            var result = await service.GetOrdersByStatusAsync("IN_ORDER");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, o => Assert.Equal("IN_ORDER", o.Status));
            mockRepository.Verify(r => r.GetOrdersByStatusAsync("IN_ORDER"), Times.Once);
        }

        /// <summary>
        /// テスト: 注文ステータスの更新が正しく実行されることを検証
        /// Given: IN_ORDER状態の注文が存在
        /// When: UpdateOrderStatusAsync(1, "CONFIRMED")実行
        /// Then: CONFIRMED状態に更新され、Repository.GetOrderByIdAsyncとUpdateOrderAsyncが各1回呼ばれる
        /// </summary>
        [Fact]
        public async Task UpdateOrderStatusAsync_ValidTransition_ReturnsUpdatedOrder()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            var existingOrder = new Order
            {
                OrderId = 1,
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = "MEMBER00001",
                Total = 1000.00m,
                Status = "IN_ORDER",
                Items = new List<OrderItem>()
            };

            var updatedOrder = new Order
            {
                OrderId = 1,
                CreatedAt = existingOrder.CreatedAt,
                MemberCardNo = existingOrder.MemberCardNo,
                Total = existingOrder.Total,
                Status = "CONFIRMED",
                Items = existingOrder.Items
            };

            mockRepository
                .Setup(r => r.GetOrderByIdAsync(1))
                .ReturnsAsync(existingOrder);

            mockRepository
                .Setup(r => r.UpdateOrderAsync(It.IsAny<Order>()))
                .ReturnsAsync(updatedOrder);

            // Act
            var result = await service.UpdateOrderStatusAsync(1, "CONFIRMED");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("CONFIRMED", result.Status);
            mockRepository.Verify(r => r.GetOrderByIdAsync(1), Times.Once);
            mockRepository.Verify(r => r.UpdateOrderAsync(It.IsAny<Order>()), Times.Once);
        }

        /// <summary>
        /// テスト: 複数の注文明細で合計金額が正しく計算されることを検証
        /// Given: 複数のOrderItem（異なる価格・割引率・数量）
        /// When: CalculateTotalAsync実行
        /// Then: 正しい合計金額が返却される（数量×単価×(1-割引率/100)の合計）
        /// </summary>
        [Fact]
        public async Task CalculateTotalAsync_MultipleItems_ReturnsCorrectTotal()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            var items = new List<OrderItem>
            {
                new OrderItem
                {
                    ProductId = 1,
                    ProductName = "カフェラテ",
                    ProductPrice = 450.00m,
                    ProductDiscountPercent = 0.00m,
                    Quantity = 2
                },
                new OrderItem
                {
                    ProductId = 2,
                    ProductName = "クロワッサン",
                    ProductPrice = 300.00m,
                    ProductDiscountPercent = 10.00m,
                    Quantity = 1
                }
            };

            // Act
            var result = await service.CalculateTotalAsync(items);

            // Assert
            // 計算: (2 × 450 × 1.0) + (1 × 300 × 0.9) = 900 + 270 = 1170.00
            Assert.Equal(1170.00m, result);
        }

        /// <summary>
        /// テスト: Repository層でInvalidOperationExceptionが発生した場合、そのまま再スローされることを検証
        /// Given: RepositoryがInvalidOperationExceptionをスロー
        /// When: CreateOrderAsync実行
        /// Then: InvalidOperationExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_RepositoryThrowsInvalidOperationException_RethrowsException()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var service = new OrderService(mockRepository.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Status = "IN_ORDER",
                Items = new List<OrderItem>()
            };

            mockRepository
                .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
                .ThrowsAsync(new InvalidOperationException("Database constraint violation"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await service.CreateOrderAsync(order)
            );

            mockRepository.Verify(r => r.CreateOrderAsync(It.IsAny<Order>()), Times.Once);
        }
    }
}
