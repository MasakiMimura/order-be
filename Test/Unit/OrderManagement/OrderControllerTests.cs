using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderBE.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OrderBE.Tests.Unit.OrderManagement
{
    /// <summary>
    /// カスタムWebApplicationFactory
    /// ContentRootを明示的に設定してWebApplicationFactoryの自動検出問題を回避
    /// InMemoryデータベースを使用してテストを高速化
    /// </summary>
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var projectDir = GetProjectPath();
            builder.UseContentRoot(projectDir);

            builder.ConfigureServices(services =>
            {
                // OrderDbContextに関連するすべての登録を削除
                var descriptorsToRemove = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>) ||
                                d.ServiceType == typeof(OrderDbContext))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
                {
                    services.Remove(descriptor);
                }

                // InMemoryデータベースを使用するDbContextOptionsを直接登録
                var options = new DbContextOptionsBuilder<OrderDbContext>()
                    .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
                    .Options;

                services.AddSingleton<DbContextOptions<OrderDbContext>>(options);
                services.AddScoped<OrderDbContext>();
            });
        }

        /// <summary>
        /// プロジェクトルートディレクトリを取得
        /// OrderBE.csproj ファイルを基準に検索
        /// </summary>
        private static string GetProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "OrderBE.csproj")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new InvalidOperationException($"Could not find project root from {currentDirectory}");
            }

            return directory.FullName;
        }
    }

    /// <summary>
    /// OrderController（注文管理）の統合テストクラス
    /// TDD方式で作成されたテストケース
    /// WebApplicationFactoryを使用してASP.NET Core統合テストを実施
    /// HTTPステータスコード検証とエラーレスポンス形式の統一検証
    /// </summary>
    public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OrderControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        /// <summary>
        /// テスト: 有効なリクエストで注文作成が成功し、201 Createdが返されることを検証
        /// Given: 有効な注文作成リクエスト（ゲスト注文）
        /// When: POST /api/v1/orders を実行
        /// Then: 201 Created が返される
        /// </summary>
        [Fact]
        public async Task CreateOrder_ValidRequest_Returns201Created()
        {
            // Arrange
            var request = new { memberCardNo = (string?)null };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/orders", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        /// <summary>
        /// テスト: レスポンスボディに注文ID、ステータス、合計金額、アイテムが含まれることを検証
        /// Given: 有効な会員注文リクエスト
        /// When: POST /api/v1/orders を実行
        /// Then: レスポンスボディに orderId (> 0), status ("IN_ORDER"), total (0), items (空配列) が含まれる
        /// </summary>
        [Fact]
        public async Task CreateOrder_WithMemberCardNo_ReturnsOrderWithRequiredFields()
        {
            // Arrange
            var request = new { memberCardNo = "ABC123DEF456" };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/orders", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            var order = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            Assert.True(order.TryGetProperty("orderId", out var orderId));
            Assert.True(orderId.GetInt32() > 0);

            Assert.True(order.TryGetProperty("status", out var status));
            Assert.Equal("IN_ORDER", status.GetString());

            Assert.True(order.TryGetProperty("total", out var total));
            Assert.Equal(0, total.GetDecimal());

            Assert.True(order.TryGetProperty("items", out var items));
            Assert.Equal(JsonValueKind.Array, items.ValueKind);
            Assert.Equal(0, items.GetArrayLength());
        }

        /// <summary>
        /// テスト: 無効なJSON形式で400 Bad Requestが返されることを検証
        /// Given: 不正なJSON形式のリクエスト
        /// When: POST /api/v1/orders を実行
        /// Then: 400 Bad Request が返され、エラーレスポンスに message と detail フィールドが含まれる
        /// </summary>
        [Fact]
        public async Task CreateOrder_InvalidJson_Returns400BadRequest()
        {
            // Arrange
            var content = new StringContent(
                "{ invalid json }",
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/orders", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("message", responseBody);
            Assert.Contains("detail", responseBody);
        }

        /// <summary>
        /// テスト: データベース接続障害時に503 Service Unavailableが返されることを検証
        /// Given: データベース接続が失敗する環境
        /// When: POST /api/v1/orders を実行
        /// Then: 503 Service Unavailable が返される
        /// </summary>
        [Fact]
        public async Task CreateOrder_DatabaseConnectionFailure_Returns503ServiceUnavailable()
        {
            // Arrange
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    var projectDir = GetProjectPath();
                    builder.UseContentRoot(projectDir);

                    builder.ConfigureServices(services =>
                    {
                        // 既存のDbContext関連サービスを削除
                        var descriptorsToRemove = services
                            .Where(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>) ||
                                        d.ServiceType == typeof(OrderDbContext))
                            .ToList();

                        foreach (var descriptor in descriptorsToRemove)
                        {
                            services.Remove(descriptor);
                        }

                        // 不正な接続文字列でPostgreSQLを設定（接続失敗を引き起こす）
                        services.AddDbContext<OrderDbContext>(options =>
                        {
                            options.UseNpgsql("Host=invalid-host;Database=test;Username=test;Password=test;Timeout=1");
                        });
                    });
                });

            var client = factory.CreateClient();
            var request = new { memberCardNo = (string?)null };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/orders", content);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        /// <summary>
        /// プロジェクトルートディレクトリを取得
        /// OrderBE.csproj ファイルを基準に検索
        /// </summary>
        private static string GetProjectPath()
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var directory = new DirectoryInfo(currentDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "OrderBE.csproj")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                throw new InvalidOperationException($"Could not find project root from {currentDirectory}");
            }

            return directory.FullName;
        }
    }
}
