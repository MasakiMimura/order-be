# Controller テストケースの標準パターン

このドキュメントは、ASP.NET Core Controller の HTTP統合テスト作成時の標準パターンを定義します。Controllerテスト作成時は、`context/api-integration-design-v2.md` から API仕様を読み取り、以下のパターンに従ってテストケースを生成してください。

## HTTPメソッド別の標準ステータスコード

### POST（作成）
- **成功**: 201 Created
- **エラー**: 400/401/500/503

### GET（取得）
- **成功**: 200 OK
- **エラー**: 401/404/500/503

### PUT（更新）
- **成功**: 200 OK
- **エラー**: 400/401/404/500/503

### DELETE（削除）
- **成功**: 204 No Content
- **エラー**: 401/404/500/503

---

## 標準例外とHTTPステータスコードのマッピング

参考実装: `patterns/backend/controller-pattern.cs`

```
EntityNotFoundException           → 404 Not Found
InvalidOperationException (※1)   → 400 Bad Request
NpgsqlException                   → 500 Internal Server Error
DbUpdateException                 → 500 Internal Server Error
SocketException                   → 503 Service Unavailable
TimeoutException                  → 503 Service Unavailable
その他Exception                   → 500 Internal Server Error
認証エラー（APIキー不正）          → 401 Unauthorized
```

※1: DbUpdateExceptionをInnerExceptionに含む場合

---

## 1つのエンドポイントに対する基本テストケース

### 1. 正常系
**命名**: `{Method}_{Resource}_ValidRequest_Returns{SuccessCode}`

成功レスポンスの検証（ステータスコード + レスポンスボディ）

### 2. レスポンスボディ検証
**命名**: `{Method}_{Resource}_ValidRequest_Returns{Resource}With{Property}`

レスポンスに必要なフィールドが含まれることを検証

### 3. バリデーションエラー
**命名**: `{Method}_{Resource}_InvalidData_Returns400BadRequest`

入力データのバリデーションエラー

### 4. DB接続失敗
**命名**: `{Method}_{Resource}_DatabaseConnectionFailure_Returns503ServiceUnavailable`

データベース接続エラー時の503検証

### 5. GET専用テスト
**命名**: `Get{Resource}ById_Existing{Resource}_Returns200OK`

既存データの取得テスト（POST後にGETで取得確認）

---

## API設計書から読み取る情報

`context/api-integration-design-v2.md` から以下を読み取ってください：

### エンドポイント
例: `GET /api/v1/materials`, `POST /api/v1/orders`

### 認証方式
- 店内システム: `X-API-Key: shop-system-key`
- 会員システム: `Authorization: Bearer {JWT}`

### Request Body
JSON形式のサンプル

```json
{
  "materialName": "コーヒー豆",
  "unitId": 1
}
```

### Response Success
成功時のレスポンス形式

```json
{
  "materialId": 1,
  "materialName": "コーヒー豆",
  "unitId": 1
}
```

### Response Error
エラー時のレスポンス形式（統一形式）

```json
{
  "message": "Invalid data provided",
  "detail": "..."
}
```

---

## CustomWebApplicationFactory セットアップ（必須）

### 推奨パターン: DbContextOptionsを直接作成

**重要**: `AddDbContext`を使用すると二重プロバイダー登録エラーが発生するため、`DbContextOptions`を直接作成する方法を使用してください。

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Step 1: 既存のDbContext関連サービスを完全削除
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<OrderDbContext>) ||
                            d.ServiceType == typeof(OrderDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Step 2: DbContextOptionsを直接作成
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")  // ユニークな名前推奨
                .Options;

            // Step 3: Singleton + Scopedで明示的に登録
            services.AddSingleton<DbContextOptions<OrderDbContext>>(options);
            services.AddScoped<OrderDbContext>();
        });
    }
}
```

**なぜこのパターンが必要か**:
1. `AddDbContext`を使うと、PostgreSQLとInMemoryの二重プロバイダー登録エラーが発生
2. `DbContextOptions`を直接作成することで、プロバイダー管理をバイパス
3. `Guid.NewGuid()`で各テストクラスごとにユニークなDB名を使用（テスト独立性を保証）

### テストクラスでの使用方法

```csharp
public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrderControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    // テストメソッド...
}
```

---

## テスト実装の必須要件

### HTTPステータスコード検証
実際のHTTPステータスコードを検証

```csharp
Assert.Equal(HttpStatusCode.Created, response.StatusCode);
```

### レスポンスボディ検証
成功時・エラー時のレスポンス形式を検証

```csharp
var content = await response.Content.ReadAsStringAsync();
var result = JsonSerializer.Deserialize<OrderResponse>(content);
Assert.NotNull(result);
Assert.Equal("IN_ORDER", result.Status);
```

### エラーレスポンス形式の統一
すべてのエラーレスポンスは統一形式

```json
{
  "message": "エラーメッセージ",
  "detail": "詳細情報"
}
```

### InMemory Database の独立性
テストごとに独立したデータベース状態を保証

---

## 実装例（POST エンドポイント）

```csharp
[Fact]
public async Task CreateOrder_ValidRequest_Returns201Created()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new { memberCardNo = "ABC123" };
    var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json");

    // Act
    var response = await client.PostAsync("/api/v1/orders", content);

    // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
}
```

---

## テスト設計の重要な注意事項

### 1. フレームワークレベルのエラーはテスト対象外

以下のようなエラーは、Controllerに到達する前にASP.NET Coreミドルウェアで処理されるため、テスト対象外です：

```csharp
// ❌ このテストは期待通りに動作しない
[Fact]
public async Task CreateOrder_InvalidJson_Returns400BadRequest()
{
    var content = new StringContent(
        "{ invalid json }",  // ← 不正なJSON
        Encoding.UTF8,
        "application/json");

    var response = await _client.PostAsync("/api/v1/orders", content);
    // Controllerのエラーハンドリングは実行されない！
}
```

### 2. ビジネスルール違反をテストする

以下のようなビジネスルールの違反をテストしてください：

```csharp
// ✅ このテストは成功する
[Fact]
public async Task CreateOrder_InvalidData_Returns400BadRequest()
{
    var request = new { memberCardNo = new string('A', 100) };  // 20文字制限を超える
    var content = new StringContent(
        JsonSerializer.Serialize(request),
        Encoding.UTF8,
        "application/json");

    var response = await _client.PostAsync("/api/v1/orders", content);
    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
```

### 3. InMemoryDatabaseとデータ依存の注意

**新規作成テスト（POST）**:
- InMemoryDBは非常に有効
- データ依存がないため、シンプルに実装できる

**GET/PUT/DELETEテスト**:
- データ依存が発生するため注意が必要
- **推奨**: テストメソッド内でPOST → GET/PUT/DELETEの一連の流れを実装
- **パターン参照**: `patterns/backend/controller-test-pattern.cs`

---

## 参考資料

- **汎用パターン**: `patterns/backend/controller-test-pattern.cs`
- **CustomWebApplicationFactory**: `patterns/backend/custom-webapplication-factory.cs`
- **アーキテクチャ**: `backend-architecture-layers.md`
- **テストパターン**: `backend-test-patterns.md`
