using OrderBE.DTOs;
using OrderBE.Repository;

namespace OrderBE.Service
{
    /// <summary>
    /// ProductService（商品サービス）
    /// 商品・カテゴリ取得のビジネスロジック層
    /// Repository層とController層の仲介、ビジネスルール実装を担当
    ///
    /// TODO: 次のタスクで実装される（現在はTDD方式のスタブ）
    /// </summary>
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

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
        /// TODO: 次のタスクで実装される
        /// </summary>
        /// <param name="categoryId">カテゴリID（nullの場合は全商品を取得）</param>
        /// <returns>商品一覧とカテゴリ一覧を含むレスポンスDTO</returns>
        public Task<ProductsWithCategoriesResponse> GetProductsWithCategoriesAsync(int? categoryId)
        {
            throw new NotImplementedException("ProductService.GetProductsWithCategoriesAsync is not implemented yet. This is TDD Red state.");
        }
    }
}
