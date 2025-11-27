using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using {ProjectName}.Data;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace {ProjectName}.Tests.Unit.{DomainName}
{
    /// <summary>
    /// {EntityName}Controller（{EntityDescription}）の統合テストクラス
    /// TDD方式で作成されたテストケース
    /// WebApplicationFactoryを使用してASP.NET Core統合テストを実施
    /// HTTPステータスコード検証とエラーレスポンス形式の統一検証
    /// </summary>
    public class {EntityName}ControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        public {EntityName}ControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// テスト: 有効なリクエストで{EntityName}作成が成功し、201 Createdが返されることを検証
        /// Given: 有効な{EntityName}作成リクエスト
        /// When: POST /api/v1/{route_prefix} を実行
        /// Then: 201 Created が返される
        /// </summary>
        [Fact]
        public async Task Create{EntityName}_ValidRequest_Returns201Created()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new { /* Add request properties */ };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/{route_prefix}", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        /// <summary>
        /// テスト: レスポンスボディに{EntityName}IDとステータスが含まれることを検証
        /// Given: 有効なリクエスト
        /// When: POST /api/v1/{route_prefix} を実行
        /// Then: レスポンスボディに {entity_id_property}, {status_property} が含まれる
        /// </summary>
        [Fact]
        public async Task Create{EntityName}_ValidRequest_ReturnsEntityWithId()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new { /* Add request properties */ };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/{route_prefix}", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            var entity = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // Assert
            Assert.True(entity.TryGetProperty("{entity_id_property}", out var entityId));
            Assert.True(entityId.GetInt32() > 0);
            // Add additional property assertions
        }

        /// <summary>
        /// テスト: 無効なデータで400 Bad Requestが返されることを検証
        /// Given: 不正なJSON形式のリクエスト
        /// When: POST /api/v1/{route_prefix} を実行
        /// Then: 400 Bad Request が返される
        /// </summary>
        [Fact]
        public async Task Create{EntityName}_InvalidData_Returns400BadRequest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = new StringContent(
                "{ invalid json }",
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/{route_prefix}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("message", responseBody);
            Assert.Contains("detail", responseBody);
        }

        /// <summary>
        /// テスト: エラーレスポンス形式が統一されていることを検証
        /// Given: 不正なJSON形式のリクエスト
        /// When: POST /api/v1/{route_prefix} を実行
        /// Then: エラーレスポンスに message と detail フィールドが含まれる
        /// </summary>
        [Fact]
        public async Task Create{EntityName}_InvalidData_ReturnsStandardErrorFormat()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = new StringContent(
                "{ invalid json }",
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/{route_prefix}", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Contains("message", responseBody);
            Assert.Contains("detail", responseBody);
        }

        /// <summary>
        /// テスト: データベース接続障害時に503 Service Unavailableが返されることを検証
        /// Given: データベース接続が失敗する環境
        /// When: POST /api/v1/{route_prefix} を実行
        /// Then: 503 Service Unavailable が返される
        /// </summary>
        [Fact]
        public async Task Create{EntityName}_DatabaseConnectionFailure_Returns503ServiceUnavailable()
        {
            // Arrange
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseContentRoot(Directory.GetCurrentDirectory());

                    builder.ConfigureServices(services =>
                    {
                        var descriptor = services.SingleOrDefault(
                            d => d.ServiceType == typeof(DbContextOptions<{DbContextName}>));

                        if (descriptor != null)
                        {
                            services.Remove(descriptor);
                        }

                        services.AddDbContext<{DbContextName}>(options =>
                        {
                            options.UseNpgsql("Host=invalid-host;Database=test;Username=test;Password=test;Timeout=1");
                        });
                    });
                });

            var client = factory.CreateClient();
            var request = new { /* Add request properties */ };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await client.PostAsync("/api/v1/{route_prefix}", content);

            // Assert
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        /// <summary>
        /// テスト: GET /api/v1/{route_prefix}/{id} で{EntityName}取得が成功し、200 OKが返されることを検証
        /// Given: データベースに{EntityName}が存在する
        /// When: GET /api/v1/{route_prefix}/{id} を実行
        /// Then: 200 OK が返され、{EntityName}データが取得できる
        /// </summary>
        [Fact]
        public async Task Get{EntityName}ById_Existing{EntityName}_Returns200OK()
        {
            // Arrange
            var client = _factory.CreateClient();

            var createRequest = new { /* Add request properties */ };
            var createContent = new StringContent(
                JsonSerializer.Serialize(createRequest),
                Encoding.UTF8,
                "application/json");

            var createResponse = await client.PostAsync("/api/v1/{route_prefix}", createContent);
            var createResponseBody = await createResponse.Content.ReadAsStringAsync();
            var createdEntity = JsonSerializer.Deserialize<JsonElement>(createResponseBody);
            var entityId = createdEntity.GetProperty("{entity_id_property}").GetInt32();

            // Act
            var response = await client.GetAsync($"/api/v1/{route_prefix}/{entityId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
