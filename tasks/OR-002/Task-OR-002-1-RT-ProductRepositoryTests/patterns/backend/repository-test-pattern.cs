using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using {ProjectName}.Data;
using {ProjectName}.Models;
using {ProjectName}.Repository;
using {ProjectName}.Exceptions;
using Xunit;

namespace {ProjectName}.Tests.Unit.{DomainName}
{
    /// <summary>
    /// {EntityName}Repository（{EntityDescription}リポジトリ）の統合テストクラス
    /// TDD方式で作成されたテストケース
    /// InMemoryDatabaseを使用してRepository層のテストを実施
    /// </summary>
    public class {EntityName}RepositoryTests : IDisposable
    {
        private readonly {DbContextName} _context;
        private readonly {EntityName}Repository _repository;
        private readonly ILogger<{EntityName}Repository> _logger;

        public {EntityName}RepositoryTests()
        {
            // InMemoryDatabaseのセットアップ
            var options = new DbContextOptionsBuilder<{DbContextName}>()
                .UseInMemoryDatabase(databaseName: "{EntityName}RepositoryTestDb")
                .Options;

            _context = new {DbContextName}(options);
            _logger = new LoggerFactory().CreateLogger<{EntityName}Repository>();
            _repository = new {EntityName}Repository(_context, _logger);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        /// <summary>
        /// テスト: 有効な{EntityName}の挿入が成功し、IDが自動採番されることを検証
        /// Given: 有効な{EntityName}エンティティ
        /// When: InsertAsync を実行
        /// Then: {EntityName}が挿入され、IDが自動採番される
        /// </summary>
        [Fact]
        public async Task InsertAsync_Valid{EntityName}_Returns{EntityName}WithId()
        {
            // Arrange
            var entity = new {EntityName}
            {
                // Add required properties
                // {PropertyName} = "{PropertyValue}"
            };

            // Act
            var result = await _repository.InsertAsync(entity);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.{EntityName}Id > 0);
            // Add additional assertions for properties
        }

        /// <summary>
        /// テスト: 既存{EntityName}の更新が成功することを検証
        /// Given: データベースに既存の{EntityName}が存在
        /// When: UpdateAsync を実行
        /// Then: {EntityName}が更新される
        /// </summary>
        [Fact]
        public async Task UpdateAsync_Existing{EntityName}_Returns Updated{EntityName}()
        {
            // Arrange
            var entity = new {EntityName}
            {
                // Add required properties
            };
            var inserted = await _repository.InsertAsync(entity);

            // Update properties
            // inserted.{PropertyName} = "{NewValue}";

            // Act
            var result = await _repository.UpdateAsync(inserted);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(inserted.{EntityName}Id, result.{EntityName}Id);
            // Assert.Equal("{NewValue}", result.{PropertyName});
        }

        /// <summary>
        /// テスト: 存在しない{EntityName}の更新時にEntityNotFoundExceptionがスローされることを検証
        /// Given: データベースに存在しない{EntityName}ID
        /// When: UpdateAsync を実行
        /// Then: EntityNotFoundExceptionがスローされる
        /// </summary>
        [Fact]
        public async Task UpdateAsync_NonExisting{EntityName}_ThrowsEntityNotFoundException()
        {
            // Arrange
            var entity = new {EntityName}
            {
                {EntityName}Id = 99999, // Non-existing ID
                // Add required properties
            };

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _repository.UpdateAsync(entity));
        }

        /// <summary>
        /// テスト: IDで{EntityName}を取得できることを検証
        /// Given: データベースに{EntityName}が存在
        /// When: GetByIdAsync を実行
        /// Then: {EntityName}が取得される
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_Existing{EntityName}_Returns{EntityName}()
        {
            // Arrange
            var entity = new {EntityName}
            {
                // Add required properties
            };
            var inserted = await _repository.InsertAsync(entity);

            // Act
            var result = await _repository.GetByIdAsync(inserted.{EntityName}Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(inserted.{EntityName}Id, result.{EntityName}Id);
        }

        /// <summary>
        /// テスト: 存在しないIDで取得時にEntityNotFoundExceptionがスローされることを検証
        /// Given: データベースに存在しない{EntityName}ID
        /// When: GetByIdAsync を実行
        /// Then: EntityNotFoundExceptionがスローされる
        /// </summary>
        [Fact]
        public async Task GetByIdAsync_NonExisting{EntityName}_ThrowsEntityNotFoundException()
        {
            // Arrange
            int nonExistingId = 99999;

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _repository.GetByIdAsync(nonExistingId));
        }

        /// <summary>
        /// テスト: 全{EntityName}を取得できることを検証
        /// Given: データベースに複数の{EntityName}が存在
        /// When: GetAllAsync を実行
        /// Then: 全{EntityName}が取得される
        /// </summary>
        [Fact]
        public async Task GetAllAsync_Multiple{EntityPluralName}_ReturnsAll{EntityPluralName}()
        {
            // Arrange
            var entity1 = new {EntityName} { /* properties */ };
            var entity2 = new {EntityName} { /* properties */ };
            await _repository.InsertAsync(entity1);
            await _repository.InsertAsync(entity2);

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Count() >= 2);
        }

        /// <summary>
        /// テスト: {EntityName}が存在しない場合にEntityNotFoundExceptionがスローされることを検証
        /// Given: データベースに{EntityName}が存在しない
        /// When: GetAllAsync を実行
        /// Then: EntityNotFoundExceptionがスローされる
        /// </summary>
        [Fact]
        public async Task GetAllAsync_No{EntityPluralName}_ThrowsEntityNotFoundException()
        {
            // Arrange
            // データベースは空の状態

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _repository.GetAllAsync());
        }

        /// <summary>
        /// テスト: データ制約違反時にInvalidOperationExceptionがスローされることを検証
        /// Given: データベース制約に違反する{EntityName}
        /// When: InsertAsync を実行
        /// Then: InvalidOperationExceptionがスローされる
        /// </summary>
        [Fact]
        public async Task InsertAsync_ConstraintViolation_ThrowsInvalidOperationException()
        {
            // Arrange
            var entity1 = new {EntityName}
            {
                // Add properties that will cause constraint violation
                // Example: UniqueConstraintProperty = "Duplicate"
            };
            await _repository.InsertAsync(entity1);

            var entity2 = new {EntityName}
            {
                // Same value for unique constraint property
                // UniqueConstraintProperty = "Duplicate"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.InsertAsync(entity2));
        }

        // Add additional test cases for:
        // - リレーションシップのテスト（Include検証）
        // - ステータス検証（該当する場合）
        // - nullable項目検証
        // - decimal型精度検証
    }
}
