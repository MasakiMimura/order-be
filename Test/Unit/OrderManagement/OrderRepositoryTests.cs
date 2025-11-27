using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderBE.Data;
using OrderBE.Exceptions;
using OrderBE.Models;
using OrderBE.Repository;
using Xunit;

namespace OrderBE.Tests.Unit.OrderManagement
{
    /// <summary>
    /// OrderRepository（注文リポジトリ）の単体テストクラス
    /// TDD方式で作成されたテストケース（7テスト）
    /// InMemoryDatabaseを使用してデータベース操作をテスト
    /// </summary>
    public class OrderRepositoryTests
    {
        /// <summary>
        /// テスト用のInMemoryDatabaseコンテキストを作成
        /// 各テストで独立したデータベースを使用するため、Guid.NewGuid()で一意な名前を生成
        /// </summary>
        private OrderDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new OrderDbContext(options);
        }

        /// <summary>
        /// テスト: 有効な注文データで注文作成が成功することを検証
        /// Given: 会員カード番号付き、2つの注文明細を持つ有効な注文データ
        /// When: CreateOrderAsyncを実行
        /// Then: 注文が作成され、OrderIdが自動採番され、IN_ORDER状態、注文明細が関連付けられる
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_ValidOrder_ReturnsCreatedOrder()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<OrderRepository>>();
            var repository = new OrderRepository(context, logger.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = "MEMBER00001",
                Total = 1170.00m,
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

            // Act
            var result = await repository.CreateOrderAsync(order);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OrderId > 0);
            Assert.Equal("IN_ORDER", result.Status);
            Assert.Equal(2, result.Items.Count);
            Assert.Equal("MEMBER00001", result.MemberCardNo);
            Assert.Equal(1170.00m, result.Total);
        }

        /// <summary>
        /// テスト: ゲスト注文（会員カード番号なし）の作成が成功することを検証
        /// Given: MemberCardNoがnullの注文データ（ゲスト注文）
        /// When: CreateOrderAsyncを実行
        /// Then: 注文が作成され、MemberCardNoがnullのまま、IN_ORDER状態で保存される
        /// </summary>
        [Fact]
        public async Task CreateOrderAsync_GuestOrder_ReturnsCreatedOrderWithNullMemberCardNo()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<OrderRepository>>();
            var repository = new OrderRepository(context, logger.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = null,
                Total = 900.00m,
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
                    }
                }
            };

            // Act
            var result = await repository.CreateOrderAsync(order);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.OrderId > 0);
            Assert.Null(result.MemberCardNo);
            Assert.Equal("IN_ORDER", result.Status);
        }

        /// <summary>
        /// テスト: 既存の注文をIDで取得し、注文明細が含まれることを検証
        /// Given: OrderId=1の注文が存在し、1つの注文明細を持つ
        /// When: GetOrderByIdAsyncを実行
        /// Then: 注文データが取得され、OrderItemがIncludeされて取得される
        /// </summary>
        [Fact]
        public async Task GetOrderByIdAsync_ExistingOrder_ReturnsOrderWithItems()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<OrderRepository>>();
            var repository = new OrderRepository(context, logger.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = "MEMBER00001",
                Total = 1170.00m,
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
                    }
                }
            };

            var createdOrder = await repository.CreateOrderAsync(order);

            // Act
            var result = await repository.GetOrderByIdAsync(createdOrder.OrderId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createdOrder.OrderId, result.OrderId);
            Assert.NotNull(result.Items);
            Assert.Single(result.Items);
            Assert.Equal("カフェラテ", result.Items[0].ProductName);
        }

        /// <summary>
        /// テスト: 存在しない注文IDで取得を試みた場合、EntityNotFoundExceptionがスローされることを検証
        /// Given: OrderId=999の注文が存在しない
        /// When: GetOrderByIdAsync(999)を実行
        /// Then: EntityNotFoundExceptionがスローされる
        /// </summary>
        [Fact]
        public async Task GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<OrderRepository>>();
            var repository = new OrderRepository(context, logger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(
                async () => await repository.GetOrderByIdAsync(999)
            );
        }

        /// <summary>
        /// テスト: ステータス別に注文を取得し、指定したステータスの注文のみが取得されることを検証
        /// Given: IN_ORDER、CONFIRMED、PAID状態の注文が混在（計4件）
        /// When: GetOrdersByStatusAsync("IN_ORDER")を実行
        /// Then: IN_ORDER状態の注文のみ2件取得され、OrderItemがIncludeされている
        /// </summary>
        [Fact]
        public async Task GetOrdersByStatusAsync_InOrderStatus_ReturnsInOrderOrdersOnly()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<OrderRepository>>();
            var repository = new OrderRepository(context, logger.Object);

            var inOrderOrder1 = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Total = 1000.00m,
                Status = "IN_ORDER",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 1,
                        ProductName = "商品1",
                        ProductPrice = 1000.00m,
                        Quantity = 1
                    }
                }
            };

            var inOrderOrder2 = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Total = 2000.00m,
                Status = "IN_ORDER",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 2,
                        ProductName = "商品2",
                        ProductPrice = 2000.00m,
                        Quantity = 1
                    }
                }
            };

            var confirmedOrder = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Total = 3000.00m,
                Status = "CONFIRMED",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 3,
                        ProductName = "商品3",
                        ProductPrice = 3000.00m,
                        Quantity = 1
                    }
                }
            };

            var paidOrder = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Total = 4000.00m,
                Status = "PAID",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 4,
                        ProductName = "商品4",
                        ProductPrice = 4000.00m,
                        Quantity = 1
                    }
                }
            };

            await repository.CreateOrderAsync(inOrderOrder1);
            await repository.CreateOrderAsync(inOrderOrder2);
            await repository.CreateOrderAsync(confirmedOrder);
            await repository.CreateOrderAsync(paidOrder);

            // Act
            var result = await repository.GetOrdersByStatusAsync("IN_ORDER");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.All(result, o => Assert.Equal("IN_ORDER", o.Status));
            Assert.All(result, o => Assert.NotEmpty(o.Items));
        }

        /// <summary>
        /// テスト: 既存の注文を更新し、変更が正しく保存されることを検証
        /// Given: 既存のIN_ORDER注文（Total=1000.00）
        /// When: TotalとStatusを変更してUpdateOrderAsyncを実行
        /// Then: 注文が更新され、Total=2000.00、Status=CONFIRMEDとなる
        /// </summary>
        [Fact]
        public async Task UpdateOrderAsync_ValidUpdate_ReturnsUpdatedOrder()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<OrderRepository>>();
            var repository = new OrderRepository(context, logger.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                MemberCardNo = "MEMBER00001",
                Total = 1000.00m,
                Status = "IN_ORDER",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 1,
                        ProductName = "商品1",
                        ProductPrice = 1000.00m,
                        Quantity = 1
                    }
                }
            };

            var createdOrder = await repository.CreateOrderAsync(order);

            // Act
            createdOrder.Total = 2000.00m;
            createdOrder.Status = "CONFIRMED";
            var result = await repository.UpdateOrderAsync(createdOrder);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2000.00m, result.Total);
            Assert.Equal("CONFIRMED", result.Status);
        }

        /// <summary>
        /// テスト: 注文を削除し、注文明細も一緒に削除されることを検証
        /// Given: 既存の注文が存在し、1つの注文明細を持つ
        /// When: DeleteOrderAsyncを実行
        /// Then: 注文とOrderItemが削除され、GetOrderByIdAsyncでEntityNotFoundExceptionがスローされる
        /// </summary>
        [Fact]
        public async Task DeleteOrderAsync_ExistingOrder_DeletesOrderAndItems()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<OrderRepository>>();
            var repository = new OrderRepository(context, logger.Object);

            var order = new Order
            {
                CreatedAt = DateTime.UtcNow,
                Total = 1000.00m,
                Status = "IN_ORDER",
                Items = new List<OrderItem>
                {
                    new OrderItem
                    {
                        ProductId = 1,
                        ProductName = "商品1",
                        ProductPrice = 1000.00m,
                        Quantity = 1
                    }
                }
            };

            var createdOrder = await repository.CreateOrderAsync(order);

            // Act
            await repository.DeleteOrderAsync(createdOrder.OrderId);

            // Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(
                async () => await repository.GetOrderByIdAsync(createdOrder.OrderId)
            );
        }
    }
}
