using OrderBE.DTOs;
using OrderBE.Models;
using OrderBE.Repository;

namespace OrderBE.Service
{
    /// <summary>
    /// ProductService（商品サービス）
    /// 商品・カテゴリ取得のビジネスロジック層
    /// Repository層とController層の仲介、ビジネスルール実装を担当
    ///
    /// 処理内容:
    /// - IProductRepository、ICategoryRepositoryをコンストラクタインジェクションで注入
    /// - 商品一覧とカテゴリ一覧の取得、フィルタリング処理
    /// - キャンペーン商品の割引後価格計算
    /// - レスポンスDTO形式への変換
    /// - 例外の透過的な伝播（Repository層からの例外をそのまま再スロー）
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        /// <summary>
        /// ProductServiceのコンストラクタ
        /// 依存性注入パターン（コンストラクタインジェクション）を使用
        /// </summary>
        /// <param name="productRepository">商品リポジトリ</param>
        /// <param name="categoryRepository">カテゴリリポジトリ</param>
        public ProductService(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        /// <summary>
        /// 商品一覧とカテゴリ一覧を取得
        /// キャンペーン商品の割引後価格を計算して返却
        ///
        /// 処理内容:
        /// 1. categoryIdがnullの場合はGetActiveProductsAsyncで全アクティブ商品を取得
        /// 2. categoryIdが指定されている場合はGetProductsByCategoryIdAsyncでフィルタリング
        /// 3. GetAllCategoriesAsyncで全カテゴリを取得（display_order順）
        /// 4. 商品をProductDtoに変換（割引後価格を計算）
        /// 5. カテゴリをCategoryDtoに変換
        /// 6. ProductsWithCategoriesResponseを生成して返却
        ///
        /// 例外処理:
        /// - Repository層からの例外（NpgsqlException, TimeoutException, SocketException等）は
        ///   透過的に伝播（catch-throw）し、Controller層で適切なHTTPステータスコードに変換される
        /// </summary>
        /// <param name="categoryId">カテゴリID（nullの場合は全商品を取得）</param>
        /// <returns>商品一覧とカテゴリ一覧を含むレスポンスDTO</returns>
        /// <exception cref="NpgsqlException">PostgreSQL接続エラーの場合</exception>
        /// <exception cref="SocketException">ネットワーク接続エラーの場合</exception>
        /// <exception cref="TimeoutException">データベース接続タイムアウトの場合</exception>
        public async Task<ProductsWithCategoriesResponse> GetProductsWithCategoriesAsync(int? categoryId)
        {
            // Repository層から商品一覧を取得
            // categoryIdが指定されている場合はカテゴリでフィルタリング、nullの場合は全アクティブ商品
            IEnumerable<Product> products;
            if (categoryId.HasValue)
            {
                products = await _productRepository.GetProductsByCategoryIdAsync(categoryId.Value);
            }
            else
            {
                products = await _productRepository.GetActiveProductsAsync();
            }

            // Repository層から全カテゴリを取得（display_order順）
            var categories = await _categoryRepository.GetAllCategoriesAsync();

            // 商品をProductDtoに変換（割引後価格を計算）
            var productDtos = products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Price = p.Price,
                IsCampaign = p.IsCampaign,
                CampaignDiscountPercent = p.CampaignDiscountPercent,
                DiscountedPrice = CalculateDiscountedPrice(p.Price, p.CampaignDiscountPercent),
                CategoryId = p.CategoryId,
                IsActive = p.IsActive
            }).ToList();

            // カテゴリをCategoryDtoに変換
            var categoryDtos = categories.Select(c => new CategoryDto
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                DisplayOrder = c.DisplayOrder
            }).ToList();

            // レスポンスDTOを生成して返却
            return new ProductsWithCategoriesResponse
            {
                Products = productDtos,
                Categories = categoryDtos
            };
        }

        /// <summary>
        /// 割引後価格を計算
        /// 計算式: price × (100 - campaign_discount_percent) / 100
        ///
        /// 処理内容:
        /// - キャンペーン割引率が0の場合は元の価格をそのまま返却
        /// - 割引率が設定されている場合は割引後価格を計算
        /// </summary>
        /// <param name="price">元の価格</param>
        /// <param name="discountPercent">割引率（0-100）</param>
        /// <returns>割引後価格</returns>
        private static int CalculateDiscountedPrice(int price, int discountPercent)
        {
            if (discountPercent == 0)
            {
                return price;
            }

            return price * (100 - discountPercent) / 100;
        }
    }
}
