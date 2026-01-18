using Moq;
using Npgsql;
using OrderBE.DTOs;
using OrderBE.Models;
using OrderBE.Repository;
using OrderBE.Service;
using Xunit;

namespace OrderBE.Tests.Unit.Product
{
    /// <summary>
    /// ProductService（商品サービス）の単体テストクラス
    /// TDD方式で作成されたテストケース（6テスト）
    /// Moqを使用してIProductRepositoryとICategoryRepositoryをMock化し、Service層のビジネスロジックをテスト
    /// </summary>
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepository;
        private readonly Mock<ICategoryRepository> _mockCategoryRepository;
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _mockProductRepository = new Mock<IProductRepository>();
            _mockCategoryRepository = new Mock<ICategoryRepository>();
            _service = new ProductService(
                _mockProductRepository.Object,
                _mockCategoryRepository.Object
            );
        }

        #region GetProductsWithCategoriesAsync Tests

        /// <summary>
        /// テスト: カテゴリIDを指定しない場合、全商品と全カテゴリが返されることを検証
        /// Given: Repositoryに商品とカテゴリが存在する
        /// When: GetProductsWithCategoriesAsyncをcategoryId=nullで実行
        /// Then: 全商品と全カテゴリが返される
        /// </summary>
        [Fact]
        public async Task GetProductsWithCategoriesAsync_NoFilter_ReturnsAllProductsAndCategories()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryId = 1,
                    CategoryName = "ドリンク",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    CategoryId = 2,
                    CategoryName = "フード",
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var products = new List<Models.Product>
            {
                new Models.Product
                {
                    ProductId = 1,
                    ProductName = "カフェラテ",
                    RecipeId = 1,
                    CategoryId = 1,
                    Price = 450,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductId = 2,
                    ProductName = "クロワッサン",
                    RecipeId = 2,
                    CategoryId = 2,
                    Price = 300,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockProductRepository
                .Setup(r => r.GetActiveProductsAsync())
                .ReturnsAsync(products);

            _mockCategoryRepository
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetProductsWithCategoriesAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Products.Count);
            Assert.Equal(2, result.Categories.Count);
            Assert.Contains(result.Products, p => p.ProductName == "カフェラテ");
            Assert.Contains(result.Products, p => p.ProductName == "クロワッサン");
            Assert.Contains(result.Categories, c => c.CategoryName == "ドリンク");
            Assert.Contains(result.Categories, c => c.CategoryName == "フード");
            _mockProductRepository.Verify(r => r.GetActiveProductsAsync(), Times.Once);
            _mockCategoryRepository.Verify(r => r.GetAllCategoriesAsync(), Times.Once);
        }

        /// <summary>
        /// テスト: カテゴリIDを指定した場合、該当カテゴリの商品のみフィルタリングされることを検証
        /// Given: Repositoryに複数カテゴリの商品が存在する
        /// When: GetProductsWithCategoriesAsyncをcategoryId=1で実行
        /// Then: カテゴリID=1の商品のみ返される
        /// </summary>
        [Fact]
        public async Task GetProductsWithCategoriesAsync_WithCategoryId_ReturnsFilteredProducts()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryId = 1,
                    CategoryName = "ドリンク",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    CategoryId = 2,
                    CategoryName = "フード",
                    DisplayOrder = 2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var drinkProducts = new List<Models.Product>
            {
                new Models.Product
                {
                    ProductId = 1,
                    ProductName = "カフェラテ",
                    RecipeId = 1,
                    CategoryId = 1,
                    Price = 450,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductId = 3,
                    ProductName = "アメリカーノ",
                    RecipeId = 3,
                    CategoryId = 1,
                    Price = 400,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockProductRepository
                .Setup(r => r.GetProductsByCategoryIdAsync(1))
                .ReturnsAsync(drinkProducts);

            _mockCategoryRepository
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetProductsWithCategoriesAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Products.Count);
            Assert.All(result.Products, p => Assert.Equal(1, p.CategoryId));
            Assert.Contains(result.Products, p => p.ProductName == "カフェラテ");
            Assert.Contains(result.Products, p => p.ProductName == "アメリカーノ");
            Assert.DoesNotContain(result.Products, p => p.ProductName == "クロワッサン");
            Assert.Equal(2, result.Categories.Count);
            _mockProductRepository.Verify(r => r.GetProductsByCategoryIdAsync(1), Times.Once);
            _mockProductRepository.Verify(r => r.GetActiveProductsAsync(), Times.Never);
            _mockCategoryRepository.Verify(r => r.GetAllCategoriesAsync(), Times.Once);
        }

        /// <summary>
        /// テスト: キャンペーン商品の割引後価格が正しく計算されることを検証
        /// Given: キャンペーン商品（is_campaign=true, campaign_discount_percent=10, price=300）が存在する
        /// When: GetProductsWithCategoriesAsyncを実行
        /// Then: 割引後価格（discountedPrice=270）が正しく計算される
        /// </summary>
        [Fact]
        public async Task GetProductsWithCategoriesAsync_CampaignProduct_CalculatesDiscountedPrice()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryId = 1,
                    CategoryName = "ドリンク",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var products = new List<Models.Product>
            {
                new Models.Product
                {
                    ProductId = 1,
                    ProductName = "キャンペーン商品",
                    RecipeId = 1,
                    CategoryId = 1,
                    Price = 300,
                    IsCampaign = true,
                    CampaignDiscountPercent = 10,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Models.Product
                {
                    ProductId = 2,
                    ProductName = "通常商品",
                    RecipeId = 2,
                    CategoryId = 1,
                    Price = 500,
                    IsCampaign = false,
                    CampaignDiscountPercent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            _mockProductRepository
                .Setup(r => r.GetActiveProductsAsync())
                .ReturnsAsync(products);

            _mockCategoryRepository
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetProductsWithCategoriesAsync(null);

            // Assert
            Assert.NotNull(result);

            // キャンペーン商品: 300 × (100 - 10) / 100 = 270
            var campaignProduct = result.Products.First(p => p.ProductName == "キャンペーン商品");
            Assert.True(campaignProduct.IsCampaign);
            Assert.Equal(10, campaignProduct.CampaignDiscountPercent);
            Assert.Equal(300, campaignProduct.Price);
            Assert.Equal(270, campaignProduct.DiscountedPrice);

            // 通常商品: 割引なしなのでPriceと同じ
            var normalProduct = result.Products.First(p => p.ProductName == "通常商品");
            Assert.False(normalProduct.IsCampaign);
            Assert.Equal(0, normalProduct.CampaignDiscountPercent);
            Assert.Equal(500, normalProduct.Price);
            Assert.Equal(500, normalProduct.DiscountedPrice);
        }

        /// <summary>
        /// テスト: 商品が0件の場合、空のproducts配列が返されることを検証
        /// Given: Repositoryに商品が0件
        /// When: GetProductsWithCategoriesAsyncを実行
        /// Then: 空のproducts配列が返される
        /// </summary>
        [Fact]
        public async Task GetProductsWithCategoriesAsync_EmptyProducts_ReturnsEmptyProductsArray()
        {
            // Arrange
            var categories = new List<Category>
            {
                new Category
                {
                    CategoryId = 1,
                    CategoryName = "ドリンク",
                    DisplayOrder = 1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            var emptyProducts = new List<Models.Product>();

            _mockProductRepository
                .Setup(r => r.GetActiveProductsAsync())
                .ReturnsAsync(emptyProducts);

            _mockCategoryRepository
                .Setup(r => r.GetAllCategoriesAsync())
                .ReturnsAsync(categories);

            // Act
            var result = await _service.GetProductsWithCategoriesAsync(null);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Products);
            Assert.Empty(result.Products);
            Assert.NotNull(result.Categories);
            Assert.Single(result.Categories);
            _mockProductRepository.Verify(r => r.GetActiveProductsAsync(), Times.Once);
            _mockCategoryRepository.Verify(r => r.GetAllCategoriesAsync(), Times.Once);
        }

        #endregion

        #region Exception Propagation Tests

        /// <summary>
        /// テスト: RepositoryからのNpgsqlExceptionがServiceを透過して伝播することを検証
        /// Given: RepositoryがNpgsqlExceptionをスロー
        /// When: GetProductsWithCategoriesAsyncを実行
        /// Then: NpgsqlExceptionがServiceを透過して伝播する
        /// </summary>
        [Fact]
        public async Task GetProductsWithCategoriesAsync_RepositoryThrowsNpgsqlException_PropagatesException()
        {
            // Arrange
            _mockProductRepository
                .Setup(r => r.GetActiveProductsAsync())
                .ThrowsAsync(new NpgsqlException("PostgreSQL connection error"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<NpgsqlException>(
                async () => await _service.GetProductsWithCategoriesAsync(null)
            );

            Assert.Equal("PostgreSQL connection error", exception.Message);
            _mockProductRepository.Verify(r => r.GetActiveProductsAsync(), Times.Once);
        }

        /// <summary>
        /// テスト: RepositoryからのTimeoutExceptionがServiceを透過して伝播することを検証
        /// Given: RepositoryがTimeoutExceptionをスロー
        /// When: GetProductsWithCategoriesAsyncを実行
        /// Then: TimeoutExceptionがServiceを透過して伝播する
        /// </summary>
        [Fact]
        public async Task GetProductsWithCategoriesAsync_RepositoryThrowsTimeoutException_PropagatesException()
        {
            // Arrange
            _mockProductRepository
                .Setup(r => r.GetActiveProductsAsync())
                .ThrowsAsync(new TimeoutException("Database connection timeout"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                async () => await _service.GetProductsWithCategoriesAsync(null)
            );

            Assert.Equal("Database connection timeout", exception.Message);
            _mockProductRepository.Verify(r => r.GetActiveProductsAsync(), Times.Once);
        }

        #endregion
    }
}
