using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderBE.Data;
using OrderBE.Models;
using OrderBE.Repository;
using Xunit;

namespace OrderBE.Tests.Unit.Product
{
    /// <summary>
    /// ProductRepository（商品リポジトリ）の統合テストクラス
    /// TDD方式で作成されたテストケース
    /// InMemoryDatabaseを使用してRepository層のテストを実施
    /// </summary>
    public class ProductRepositoryTests
    {
        /// <summary>
        /// テスト用のInMemoryDatabaseコンテキストを作成
        /// 各テストで独立したデータベースを使用するため、Guid.NewGuid()で一意な名前を生成
        /// </summary>
        private ProductDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ProductDbContext(options);
        }

        /// <summary>
        /// テスト用カテゴリデータを作成
        /// </summary>
        /// <param name="context">DbContext</param>
        /// <returns>作成したカテゴリのリスト</returns>
        private async Task<List<Category>> CreateTestCategoriesAsync(ProductDbContext context)
        {
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryName = "ドリンク",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    CategoryName = "フード",
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    CategoryName = "デザート",
                    DisplayOrder = 3,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
            return categories;
        }

        #region GetAllProductsAsync Tests

        /// <summary>
        /// テスト: 複数の商品が存在する場合、全商品が正しく取得されることを検証
        /// Given: データベースに3件の商品が存在
        /// When: GetAllProductsAsyncを実行
        /// Then: 3件の商品が全て取得される
        /// </summary>
        [Fact]
        public async Task GetAllProductsAsync_MultipleProducts_ReturnsAllProducts()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);

            var products = new List<Models.Product>
            {
                new Models.Product
                {
                    ProductName = "カフェラテ",
                    RecipeId = 1,
                    CategoryId = categories[0].CategoryId,
                    Price = 450,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductName = "クロワッサン",
                    RecipeId = 2,
                    CategoryId = categories[1].CategoryId,
                    Price = 300,
                    IsCampaign = true,
                    CampaignDiscountPercent = 10,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductName = "チーズケーキ",
                    RecipeId = 3,
                    CategoryId = categories[2].CategoryId,
                    Price = 500,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count());
            Assert.Contains(result, p => p.ProductName == "カフェラテ");
            Assert.Contains(result, p => p.ProductName == "クロワッサン");
            Assert.Contains(result, p => p.ProductName == "チーズケーキ");
        }

        /// <summary>
        /// テスト: 商品が存在しない場合、空のリストが返されることを検証
        /// Given: データベースに商品が0件
        /// When: GetAllProductsAsyncを実行
        /// Then: 空のリストが返される
        /// </summary>
        [Fact]
        public async Task GetAllProductsAsync_NoProducts_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            // Act
            var result = await repository.GetAllProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// テスト: 商品取得時にカテゴリ情報が含まれることを検証
        /// Given: データベースに商品とカテゴリが存在
        /// When: GetAllProductsAsyncを実行
        /// Then: 商品にカテゴリ情報（Category）がIncludeされている
        /// </summary>
        [Fact]
        public async Task GetAllProductsAsync_WithCategory_IncludesCategoryInfo()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);

            var product = new Models.Product
            {
                ProductName = "カフェラテ",
                RecipeId = 1,
                CategoryId = categories[0].CategoryId,
                Price = 450,
                IsCampaign = false,
                CampaignDiscountPercent = 0,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllProductsAsync();

            // Assert
            Assert.NotNull(result);
            var retrievedProduct = result.First();
            Assert.NotNull(retrievedProduct.Category);
            Assert.Equal("ドリンク", retrievedProduct.Category.CategoryName);
        }

        #endregion

        #region GetProductsByCategoryIdAsync Tests

        /// <summary>
        /// テスト: カテゴリIDを指定して商品を取得し、該当カテゴリの商品のみ返されることを検証
        /// Given: 異なるカテゴリに属する複数の商品が存在
        /// When: GetProductsByCategoryIdAsync(categoryId: ドリンクカテゴリID)を実行
        /// Then: ドリンクカテゴリの商品のみ取得される
        /// </summary>
        [Fact]
        public async Task GetProductsByCategoryIdAsync_ExistingCategory_ReturnsFilteredProducts()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);
            var drinkCategoryId = categories[0].CategoryId;
            var foodCategoryId = categories[1].CategoryId;

            var products = new List<Models.Product>
            {
                new Models.Product
                {
                    ProductName = "カフェラテ",
                    RecipeId = 1,
                    CategoryId = drinkCategoryId,
                    Price = 450,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductName = "アメリカーノ",
                    RecipeId = 2,
                    CategoryId = drinkCategoryId,
                    Price = 400,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductName = "クロワッサン",
                    RecipeId = 3,
                    CategoryId = foodCategoryId,
                    Price = 300,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetProductsByCategoryIdAsync(drinkCategoryId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, p => Assert.Equal(drinkCategoryId, p.CategoryId));
            Assert.Contains(result, p => p.ProductName == "カフェラテ");
            Assert.Contains(result, p => p.ProductName == "アメリカーノ");
            Assert.DoesNotContain(result, p => p.ProductName == "クロワッサン");
        }

        /// <summary>
        /// テスト: 存在しないカテゴリIDを指定した場合、空のリストが返されることを検証
        /// Given: データベースに商品が存在
        /// When: GetProductsByCategoryIdAsync(categoryId: 99999)を実行
        /// Then: 空のリストが返される
        /// </summary>
        [Fact]
        public async Task GetProductsByCategoryIdAsync_NonExistingCategory_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);

            var product = new Models.Product
            {
                ProductName = "カフェラテ",
                RecipeId = 1,
                CategoryId = categories[0].CategoryId,
                Price = 450,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetProductsByCategoryIdAsync(99999);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// テスト: カテゴリ別商品取得時にカテゴリ情報が含まれることを検証
        /// Given: データベースに商品とカテゴリが存在
        /// When: GetProductsByCategoryIdAsyncを実行
        /// Then: 商品にカテゴリ情報がIncludeされている
        /// </summary>
        [Fact]
        public async Task GetProductsByCategoryIdAsync_WithCategory_IncludesCategoryInfo()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);
            var drinkCategoryId = categories[0].CategoryId;

            var product = new Models.Product
            {
                ProductName = "カフェラテ",
                RecipeId = 1,
                CategoryId = drinkCategoryId,
                Price = 450,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetProductsByCategoryIdAsync(drinkCategoryId);

            // Assert
            Assert.NotNull(result);
            var retrievedProduct = result.First();
            Assert.NotNull(retrievedProduct.Category);
            Assert.Equal("ドリンク", retrievedProduct.Category.CategoryName);
        }

        #endregion

        #region GetActiveProductsAsync Tests

        /// <summary>
        /// テスト: アクティブな商品とインアクティブな商品が混在する場合、アクティブな商品のみ取得されることを検証
        /// Given: IsActive=true 2件、IsActive=false 1件の商品が存在
        /// When: GetActiveProductsAsyncを実行
        /// Then: IsActive=trueの商品のみ2件取得される
        /// </summary>
        [Fact]
        public async Task GetActiveProductsAsync_MixedProducts_ReturnsOnlyActiveProducts()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);

            var products = new List<Models.Product>
            {
                new Models.Product
                {
                    ProductName = "カフェラテ",
                    RecipeId = 1,
                    CategoryId = categories[0].CategoryId,
                    Price = 450,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductName = "クロワッサン",
                    RecipeId = 2,
                    CategoryId = categories[1].CategoryId,
                    Price = 300,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductName = "販売終了商品",
                    RecipeId = 3,
                    CategoryId = categories[2].CategoryId,
                    Price = 500,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetActiveProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, p => Assert.True(p.IsActive));
            Assert.Contains(result, p => p.ProductName == "カフェラテ");
            Assert.Contains(result, p => p.ProductName == "クロワッサン");
            Assert.DoesNotContain(result, p => p.ProductName == "販売終了商品");
        }

        /// <summary>
        /// テスト: アクティブな商品が存在しない場合、空のリストが返されることを検証
        /// Given: データベースにIsActive=falseの商品のみ存在
        /// When: GetActiveProductsAsyncを実行
        /// Then: 空のリストが返される
        /// </summary>
        [Fact]
        public async Task GetActiveProductsAsync_NoActiveProducts_ReturnsEmptyList()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);

            var inactiveProduct = new Models.Product
            {
                ProductName = "販売終了商品",
                RecipeId = 1,
                CategoryId = categories[0].CategoryId,
                Price = 500,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Products.Add(inactiveProduct);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetActiveProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        /// <summary>
        /// テスト: アクティブ商品取得時にカテゴリ情報が含まれることを検証
        /// Given: データベースにアクティブな商品とカテゴリが存在
        /// When: GetActiveProductsAsyncを実行
        /// Then: 商品にカテゴリ情報がIncludeされている
        /// </summary>
        [Fact]
        public async Task GetActiveProductsAsync_WithCategory_IncludesCategoryInfo()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);

            var product = new Models.Product
            {
                ProductName = "カフェラテ",
                RecipeId = 1,
                CategoryId = categories[0].CategoryId,
                Price = 450,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetActiveProductsAsync();

            // Assert
            Assert.NotNull(result);
            var retrievedProduct = result.First();
            Assert.NotNull(retrievedProduct.Category);
            Assert.Equal("ドリンク", retrievedProduct.Category.CategoryName);
        }

        #endregion

        #region Campaign Product Tests

        /// <summary>
        /// テスト: キャンペーン商品が正しく取得できることを検証
        /// Given: キャンペーン設定あり/なしの商品が混在
        /// When: GetAllProductsAsyncを実行
        /// Then: キャンペーン情報（IsCampaign、CampaignDiscountPercent）が正しく取得される
        /// </summary>
        [Fact]
        public async Task GetAllProductsAsync_CampaignProducts_ReturnsCampaignInfo()
        {
            // Arrange
            using var context = CreateInMemoryContext();
            var logger = new Mock<ILogger<ProductRepository>>();
            var repository = new ProductRepository(context, logger.Object);

            var categories = await CreateTestCategoriesAsync(context);

            var products = new List<Models.Product>
            {
                new Models.Product
                {
                    ProductName = "通常商品",
                    RecipeId = 1,
                    CategoryId = categories[0].CategoryId,
                    Price = 450,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductName = "キャンペーン商品",
                    RecipeId = 2,
                    CategoryId = categories[1].CategoryId,
                    Price = 300,
                    IsCampaign = true,
                    CampaignDiscountPercent = 20,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Products.AddRange(products);
            await context.SaveChangesAsync();

            // Act
            var result = await repository.GetAllProductsAsync();

            // Assert
            Assert.NotNull(result);

            var normalProduct = result.First(p => p.ProductName == "通常商品");
            Assert.False(normalProduct.IsCampaign);
            Assert.Equal(0, normalProduct.CampaignDiscountPercent);

            var campaignProduct = result.First(p => p.ProductName == "キャンペーン商品");
            Assert.True(campaignProduct.IsCampaign);
            Assert.Equal(20, campaignProduct.CampaignDiscountPercent);
        }

        #endregion
    }
}
