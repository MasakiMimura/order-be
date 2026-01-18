using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderBE.Data;
using OrderBE.Models;
using System.Net;
using System.Text.Json;
using Xunit;

namespace OrderBE.Tests.Unit.Product
{
    /// <summary>
    /// ProductController（商品コントローラー）の統合テストクラス
    /// TDD方式で作成されたテストケース（5テスト）
    /// WebApplicationFactoryを使用してASP.NET Core統合テストを実施
    /// HTTPステータスコード検証とレスポンス形式の検証
    /// </summary>
    public class ProductControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;

        /// <summary>
        /// コンストラクタ
        /// CustomWebApplicationFactoryをDIで受け取り、テスト用クライアントを生成
        /// </summary>
        /// <param name="factory">カスタムWebApplicationFactory</param>
        public ProductControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// テスト: 有効なリクエストで商品一覧取得が成功し、200 OKが返されることを検証
        /// Given: 有効なリクエスト
        /// When: GET /api/v1/products を実行
        /// Then: 200 OK が返される
        /// </summary>
        [Fact]
        public async Task GetProducts_ValidRequest_Returns200OK()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/products");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        /// <summary>
        /// テスト: レスポンスボディにproductsとcategoriesフィールドが含まれることを検証
        /// Given: 有効なリクエスト
        /// When: GET /api/v1/products を実行
        /// Then: レスポンスボディに products, categories フィールドが含まれる
        /// </summary>
        [Fact]
        public async Task GetProducts_ValidRequest_ReturnsProductsAndCategories()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/products");
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(jsonResponse.TryGetProperty("products", out var productsElement),
                "Response should contain 'products' field");
            Assert.True(jsonResponse.TryGetProperty("categories", out var categoriesElement),
                "Response should contain 'categories' field");
            Assert.Equal(JsonValueKind.Array, productsElement.ValueKind);
            Assert.Equal(JsonValueKind.Array, categoriesElement.ValueKind);
        }

        /// <summary>
        /// テスト: categoryIdパラメータ指定で該当カテゴリの商品のみ返却されることを検証
        /// Given: categoryId=1 のクエリパラメータとカテゴリ1の商品がデータベースに存在
        /// When: GET /api/v1/products?categoryId=1 を実行
        /// Then: 200 OK が返され、categoryId=1 の商品のみ返却される
        /// </summary>
        [Fact]
        public async Task GetProducts_WithCategoryId_ReturnsFilteredProducts()
        {
            // Arrange
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    var projectDir = GetProjectPath();
                    builder.UseContentRoot(projectDir);

                    builder.ConfigureServices(services =>
                    {
                        // ProductDbContextに関連するすべての登録を削除
                        var descriptorsToRemove = services
                            .Where(d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>) ||
                                        d.ServiceType == typeof(ProductDbContext))
                            .ToList();

                        foreach (var descriptor in descriptorsToRemove)
                        {
                            services.Remove(descriptor);
                        }

                        // InMemoryデータベースを使用するDbContextOptionsを直接登録
                        var options = new DbContextOptionsBuilder<ProductDbContext>()
                            .UseInMemoryDatabase($"ProductTestDb_FilterTest_{Guid.NewGuid()}")
                            .Options;

                        services.AddSingleton<DbContextOptions<ProductDbContext>>(options);
                        services.AddScoped<ProductDbContext>();

                        // テストデータをシード
                        var sp = services.BuildServiceProvider();
                        using var scope = sp.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

                        // カテゴリを追加
                        var category1 = new Category { CategoryId = 1, CategoryName = "ドリンク", DisplayOrder = 1 };
                        var category2 = new Category { CategoryId = 2, CategoryName = "フード", DisplayOrder = 2 };
                        db.Categories.AddRange(category1, category2);

                        // 商品を追加
                        db.Products.AddRange(
                            new OrderBE.Models.Product
                            {
                                ProductId = 1,
                                ProductName = "エスプレッソ",
                                Price = 300,
                                CategoryId = 1,
                                RecipeId = 1,
                                IsActive = true,
                                IsCampaign = false,
                                CampaignDiscountPercent = 0
                            },
                            new OrderBE.Models.Product
                            {
                                ProductId = 2,
                                ProductName = "クロワッサン",
                                Price = 250,
                                CategoryId = 2,
                                RecipeId = 2,
                                IsActive = true,
                                IsCampaign = false,
                                CampaignDiscountPercent = 0
                            }
                        );
                        db.SaveChanges();
                    });
                });

            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/products?categoryId=1");
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(jsonResponse.TryGetProperty("products", out var productsElement));

            // すべての商品がcategoryId=1であることを検証
            foreach (var product in productsElement.EnumerateArray())
            {
                Assert.True(product.TryGetProperty("categoryId", out var categoryIdElement));
                Assert.Equal(1, categoryIdElement.GetInt32());
            }
        }

        /// <summary>
        /// テスト: 存在しないcategoryIdで空のproducts配列が返されることを検証
        /// Given: 存在しないcategoryId=999
        /// When: GET /api/v1/products?categoryId=999 を実行
        /// Then: 200 OK が返され、空の products 配列が返却される
        /// </summary>
        [Fact]
        public async Task GetProducts_NonExistentCategoryId_ReturnsEmptyProducts()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/products?categoryId=999");
            var responseBody = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(jsonResponse.TryGetProperty("products", out var productsElement));
            Assert.Equal(0, productsElement.GetArrayLength());
        }

        /// <summary>
        /// テスト: データベース接続障害時に503 Service Unavailableが返されることを検証
        /// Given: データベース接続が失敗する環境（無効なホスト名）
        /// When: GET /api/v1/products を実行
        /// Then: 503 Service Unavailable が返される
        /// </summary>
        [Fact]
        public async Task GetProducts_DatabaseConnectionFailure_Returns503ServiceUnavailable()
        {
            // Arrange
            var factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    var projectDir = GetProjectPath();
                    builder.UseContentRoot(projectDir);

                    builder.ConfigureServices(services =>
                    {
                        // ProductDbContextに関連するすべての登録を削除
                        var descriptorsToRemove = services
                            .Where(d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>) ||
                                        d.ServiceType == typeof(ProductDbContext))
                            .ToList();

                        foreach (var descriptor in descriptorsToRemove)
                        {
                            services.Remove(descriptor);
                        }

                        // 無効なホストのPostgreSQLを使用（接続タイムアウト=1秒）
                        services.AddDbContext<ProductDbContext>(options =>
                        {
                            options.UseNpgsql("Host=invalid-host-that-does-not-exist;Database=test;Username=test;Password=test;Timeout=1");
                        });
                    });
                });

            var client = factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/v1/products");

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
