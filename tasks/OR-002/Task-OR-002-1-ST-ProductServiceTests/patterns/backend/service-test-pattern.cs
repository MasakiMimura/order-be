using Moq;
using {ProjectName}.Models;
using {ProjectName}.DTOs;
using {ProjectName}.Repository;
using {ProjectName}.Service;
using {ProjectName}.Exceptions;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace {ProjectName}.Tests.Unit.{DomainName}
{
    /// <summary>
    /// {EntityName}Service（{EntityDescription}サービス）の単体テストクラス
    /// TDD方式で作成されたテストケース
    /// Moqを使用してRepositoryをMock化してService層のテストを実施
    /// </summary>
    public class {EntityName}ServiceTests
    {
        private readonly Mock<{EntityName}Repository> _mockRepository;
        private readonly {EntityName}Service _service;

        public {EntityName}ServiceTests()
        {
            _mockRepository = new Mock<{EntityName}Repository>();
            _service = new {EntityName}Service(_mockRepository.Object);
        }

        /// <summary>
        /// テスト: 有効な{EntityName}の追加が成功し、DTOが返されることを検証
        /// Given: 有効な{EntityName}エンティティ
        /// When: Add{EntityName}Async を実行
        /// Then: Repository.InsertAsyncが呼ばれ、DTOが返される
        /// </summary>
        [Fact]
        public async Task Add{EntityName}Async_Valid{EntityName}_Returns{EntityName}Dto()
        {
            // Arrange
            var entity = new {EntityName}
            {
                {EntityName}Id = 0,
                // Add required properties
            };

            var insertedEntity = new {EntityName}
            {
                {EntityName}Id = 1, // Auto-generated ID
                // Same properties as entity
            };

            _mockRepository
                .Setup(r => r.InsertAsync(It.IsAny<{EntityName}>()))
                .ReturnsAsync(insertedEntity);

            // Act
            var result = await _service.Add{EntityName}Async(entity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.{EntityName}Id);
            _mockRepository.Verify(r => r.InsertAsync(It.IsAny<{EntityName}>()), Times.Once);
        }

        /// <summary>
        /// テスト: InvalidOperationException発生時に例外が再スローされることを検証
        /// Given: Repository.InsertAsyncがInvalidOperationExceptionをスロー
        /// When: Add{EntityName}Async を実行
        /// Then: InvalidOperationExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task Add{EntityName}Async_InvalidOperation_ThrowsInvalidOperationException()
        {
            // Arrange
            var entity = new {EntityName} { /* properties */ };

            _mockRepository
                .Setup(r => r.InsertAsync(It.IsAny<{EntityName}>()))
                .ThrowsAsync(new InvalidOperationException("Insert failed due to database constraint"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.Add{EntityName}Async(entity));
            _mockRepository.Verify(r => r.InsertAsync(It.IsAny<{EntityName}>()), Times.Once);
        }

        /// <summary>
        /// テスト: 既存{EntityName}の更新が成功し、DTOが返されることを検証
        /// Given: 既存の{EntityName}エンティティ
        /// When: Update{EntityName}Async を実行
        /// Then: Repository.UpdateAsyncが呼ばれ、DTOが返される
        /// </summary>
        [Fact]
        public async Task Update{EntityName}Async_Existing{EntityName}_Returns{EntityName}Dto()
        {
            // Arrange
            var entity = new {EntityName}
            {
                {EntityName}Id = 1,
                // Add properties
            };

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<{EntityName}>()))
                .ReturnsAsync(entity);

            // Act
            var result = await _service.Update{EntityName}Async(entity);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.{EntityName}Id);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<{EntityName}>()), Times.Once);
        }

        /// <summary>
        /// テスト: 存在しない{EntityName}の更新時にEntityNotFoundExceptionが再スローされることを検証
        /// Given: Repository.UpdateAsyncがEntityNotFoundExceptionをスロー
        /// When: Update{EntityName}Async を実行
        /// Then: EntityNotFoundExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task Update{EntityName}Async_NonExisting{EntityName}_ThrowsEntityNotFoundException()
        {
            // Arrange
            var entity = new {EntityName} { {EntityName}Id = 99999 };

            _mockRepository
                .Setup(r => r.UpdateAsync(It.IsAny<{EntityName}>()))
                .ThrowsAsync(new EntityNotFoundException($"{EntityName} with ID 99999 not found."));

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.Update{EntityName}Async(entity));
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<{EntityName}>()), Times.Once);
        }

        /// <summary>
        /// テスト: IDで{EntityName}を取得できることを検証
        /// Given: Repository.GetByIdAsyncが{EntityName}を返す
        /// When: Get{EntityName}ByIdAsync を実行
        /// Then: DTOが返される
        /// </summary>
        [Fact]
        public async Task Get{EntityName}ByIdAsync_Existing{EntityName}_Returns{EntityName}Dto()
        {
            // Arrange
            var entity = new {EntityName}
            {
                {EntityName}Id = 1,
                // Add properties
            };

            _mockRepository
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(entity);

            // Act
            var result = await _service.Get{EntityName}ByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.{EntityName}Id);
            _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        }

        /// <summary>
        /// テスト: 存在しないIDで取得時にEntityNotFoundExceptionが再スローされることを検証
        /// Given: Repository.GetByIdAsyncがEntityNotFoundExceptionをスロー
        /// When: Get{EntityName}ByIdAsync を実行
        /// Then: EntityNotFoundExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task Get{EntityName}ByIdAsync_NonExisting{EntityName}_ThrowsEntityNotFoundException()
        {
            // Arrange
            _mockRepository
                .Setup(r => r.GetByIdAsync(99999))
                .ThrowsAsync(new EntityNotFoundException($"{EntityName} with ID 99999 not found."));

            // Act & Assert
            await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.Get{EntityName}ByIdAsync(99999));
            _mockRepository.Verify(r => r.GetByIdAsync(99999), Times.Once);
        }

        /// <summary>
        /// テスト: 全{EntityName}を取得できることを検証
        /// Given: Repository.GetAllAsyncが{EntityName}リストを返す
        /// When: GetAll{EntityPluralName}Async を実行
        /// Then: DTOリストが返される
        /// </summary>
        [Fact]
        public async Task GetAll{EntityPluralName}Async_Multiple{EntityPluralName}_ReturnsAll{EntityName}Dtos()
        {
            // Arrange
            var entities = new List<{EntityName}>
            {
                new {EntityName} { {EntityName}Id = 1 },
                new {EntityName} { {EntityName}Id = 2 }
            };

            _mockRepository
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(entities);

            // Act
            var result = await _service.GetAll{EntityPluralName}Async();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
        }

        /// <summary>
        /// テスト: NpgsqlException発生時に例外が再スローされることを検証
        /// Given: Repository.InsertAsyncがNpgsqlExceptionをスロー
        /// When: Add{EntityName}Async を実行
        /// Then: NpgsqlExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task Add{EntityName}Async_NpgsqlException_ThrowsNpgsqlException()
        {
            // Arrange
            var entity = new {EntityName} { /* properties */ };

            _mockRepository
                .Setup(r => r.InsertAsync(It.IsAny<{EntityName}>()))
                .ThrowsAsync(new NpgsqlException("PostgreSQL connection error"));

            // Act & Assert
            await Assert.ThrowsAsync<NpgsqlException>(() => _service.Add{EntityName}Async(entity));
            _mockRepository.Verify(r => r.InsertAsync(It.IsAny<{EntityName}>()), Times.Once);
        }

        /// <summary>
        /// テスト: TimeoutException発生時に例外が再スローされることを検証
        /// Given: Repository.InsertAsyncがTimeoutExceptionをスロー
        /// When: Add{EntityName}Async を実行
        /// Then: TimeoutExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task Add{EntityName}Async_TimeoutException_ThrowsTimeoutException()
        {
            // Arrange
            var entity = new {EntityName} { /* properties */ };

            _mockRepository
                .Setup(r => r.InsertAsync(It.IsAny<{EntityName}>()))
                .ThrowsAsync(new TimeoutException("Database connection timeout"));

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() => _service.Add{EntityName}Async(entity));
            _mockRepository.Verify(r => r.InsertAsync(It.IsAny<{EntityName}>()), Times.Once);
        }

        /// <summary>
        /// テスト: DbUpdateException発生時に例外が再スローされることを検証
        /// Given: Repository.InsertAsyncがDbUpdateExceptionをスロー
        /// When: Add{EntityName}Async を実行
        /// Then: DbUpdateExceptionが再スローされる
        /// </summary>
        [Fact]
        public async Task Add{EntityName}Async_DbUpdateException_ThrowsDbUpdateException()
        {
            // Arrange
            var entity = new {EntityName} { /* properties */ };

            _mockRepository
                .Setup(r => r.InsertAsync(It.IsAny<{EntityName}>()))
                .ThrowsAsync(new DbUpdateException("Database update failed"));

            // Act & Assert
            await Assert.ThrowsAsync<DbUpdateException>(() => _service.Add{EntityName}Async(entity));
            _mockRepository.Verify(r => r.InsertAsync(It.IsAny<{EntityName}>()), Times.Once);
        }

        // Add additional test cases for:
        // - ビジネスロジックテスト
        // - ステータス遷移検証
        // - 計算ロジック検証
    }
}
