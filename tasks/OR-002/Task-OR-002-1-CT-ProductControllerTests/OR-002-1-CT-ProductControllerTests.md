# コード生成タスク: OR-002-1-CT ProductController テストコード作成

## 1. 概要

- **ゴール:** ProductController の HTTP統合テストコードを TDD方式で作成する（実装コードは作成しない）
- **対象ファイル:** `Test/Unit/Product/ProductControllerTests.cs`
- **リポジトリ:** `backend`

## 2. 実装の指針

1. **TDD方式**: テストコードのみを作成し、実装コード（ProductController, ProductService等）は作成しない
2. **テストは「Red」状態（失敗する状態）で完了することが正しい**
3. テストコードで、実装すべきインターフェース（HTTPエンドポイント、リクエスト/レスポンス形式、エラーハンドリング）を明確に定義する
4. **GET /api/v1/products エンドポイント**の動作を検証するテストを作成
5. **CustomWebApplicationFactory + InMemoryDatabase パターン**を使用
6. **AAA（Arrange-Act-Assert）パターン**を厳守
7. **Given-When-Then形式のコメント**を各テストメソッドに記載
8. **テストメソッド命名規則**: `MethodName_Scenario_ExpectedBehavior`

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**対象PBI**: OR-002 カテゴリ・商品表示機能

**Business Rules**:
- 商品・カテゴリ一覧は GET /api/v1/products で取得
- categoryId クエリパラメータでカテゴリ別フィルタリング可能
- is_active=true の商品のみ返却
- キャンペーン商品は isCampaign=true, campaignDiscountPercent でマーク
- レスポンスは products 配列と categories 配列を含む

**Acceptance Criteria**:
- AC1: GET /api/v1/products → HTTPステータスコード200、商品・カテゴリ一覧返却
- AC2: GET /api/v1/products?categoryId=1 → HTTPステータスコード200、カテゴリ1の商品のみ返却
- AC3: NpgsqlException 発生時 → HTTPステータスコード503
- AC4: TimeoutException 発生時 → HTTPステータスコード503
- AC5: Exception 発生時 → HTTPステータスコード500

### 3.2. 関連データベーススキーマ

```sql
-- カテゴリマスタ
CREATE TABLE category (
    category_id SERIAL PRIMARY KEY,
    category_name VARCHAR(255) UNIQUE NOT NULL,
    display_order INTEGER DEFAULT 0 NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 商品
CREATE TABLE product (
    product_id SERIAL PRIMARY KEY,
    product_name VARCHAR(255) NOT NULL,
    recipe_id INTEGER NOT NULL REFERENCES recipe(recipe_id),
    category_id INTEGER NOT NULL REFERENCES category(category_id),
    price INTEGER NOT NULL CHECK (price >= 0),
    is_campaign BOOLEAN DEFAULT FALSE NOT NULL,
    campaign_discount_percent INTEGER DEFAULT 0 CHECK (campaign_discount_percent >= 0 AND campaign_discount_percent <= 100),
    is_active BOOLEAN DEFAULT TRUE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- インデックス
CREATE INDEX idx_product_category_id ON product(category_id);
CREATE INDEX idx_product_is_active ON product(is_active);
```

### 3.3. 関連API仕様

**エンドポイント**: GET /api/v1/products

**認証**: X-API-Key: shop-system-key

**Request**:
```http
GET /api/v1/products?categoryId=&search=
Headers: X-API-Key: shop-system-key
```

**Response Success (200 OK)**:
```json
{
  "products": [
    {
      "productId": 1,
      "productName": "エスプレッソ",
      "price": 300,
      "isCampaign": false,
      "campaignDiscountPercent": 0,
      "categoryId": 1,
      "categoryName": "ドリンク",
      "recipeId": 1,
      "recipeName": "エスプレッソ"
    }
  ],
  "categories": [
    {"categoryId": 1, "categoryName": "ドリンク"},
    {"categoryId": 2, "categoryName": "フード"}
  ]
}
```

**Response Error**:
```json
{"error": "InternalServerError", "message": "商品データの取得に失敗しました"}
```

### 3.4. テスト対象エンドポイント一覧

| HTTPメソッド | エンドポイント | 説明 |
|-------------|---------------|------|
| GET | /api/v1/products | 商品・カテゴリ一覧取得 |
| GET | /api/v1/products?categoryId={id} | カテゴリ別商品取得 |

---

## 4. 品質基準（order-be 同等）

### 4.1. テストコードの品質基準

**必須要件**:
- AAA パターン（Arrange-Act-Assert）の厳守
- Given-When-Then 形式のコメント
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`

**GET エンドポイントのテストケース**:
1. **正常系テスト**: 有効なリクエストで200 OKが返されることを検証
2. **レスポンスボディ検証**: レスポンスに products, categories フィールドが含まれることを検証
3. **categoryId パラメータテスト**: categoryId 指定で該当カテゴリの商品のみ返却されることを検証
4. **DB接続失敗テスト（503）**: DB接続エラー時に503 Service Unavailableが返されることを検証
5. **空のcategoryIdテスト**: 存在しないcategoryIdで空の products 配列が返されることを検証

**テストケース数の目安**: 5〜7テスト

### 4.2. CustomWebApplicationFactory パターン

```csharp
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 既存のDbContext関連サービスを完全削除
            var descriptorsToRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<ProductDbContext>) ||
                            d.ServiceType == typeof(ProductDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // DbContextOptionsを直接作成（InMemoryDatabase）
            var options = new DbContextOptionsBuilder<ProductDbContext>()
                .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
                .Options;

            services.AddSingleton<DbContextOptions<ProductDbContext>>(options);
            services.AddScoped<ProductDbContext>();
        });
    }
}
```

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
`Test/Unit/Product/ProductControllerTests.cs`のテストコードを生成してください。

**品質基準: order-be 同等**
- テストケース数の目安: 5〜7テスト程度
- AAA パターン（Arrange-Act-Assert）の厳守
- Given-When-Then 形式のコメント
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたビジネスルール、DBスキーマ、API仕様をすべて満たすように実装してください。
- **汎用パターン（`patterns/`）を厳密に参照してください。**
  - プレースホルダー（`{EntityName}`, `{ProjectName}`, `{DbContextName}`等）を実際の値に置換してください。
  - パターンファイルの構造・命名規則・コーディングスタイルを完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**[テストコード作成タスクの追加要件]**
- **テストコードのみを作成し、テスト対象の実装コード（ProductController, ProductService等）は作成しないでください。**
- テスト対象のクラスは次のタスクで実装されるため、テストは「Red」状態（失敗する状態）で完了します。
- テストコードで、実装すべきインターフェース（HTTPエンドポイント、レスポンス形式、エラーハンドリング）を明確に定義してください。
- **AAA パターン（Arrange-Act-Assert）に厳密に従って記述してください。**
  - 詳細は `rules/backend-test-patterns.md` を参照
- **Given-When-Then 形式のコメント**を各テストメソッドに記載してください。
- **テストメソッド命名規則**: `MethodName_Scenario_ExpectedBehavior`
- **Controller層テスト**: `rules/backend-controller-test-standard.md` を参照
  - WebApplicationFactoryを使用したHTTP統合テスト
  - CustomWebApplicationFactory + InMemoryDatabaseパターンを使用

**テストケース一覧（必須）:**

1. **GetProducts_ValidRequest_Returns200OK**
   - Given: 有効なリクエスト
   - When: GET /api/v1/products を実行
   - Then: 200 OK が返される

2. **GetProducts_ValidRequest_ReturnsProductsAndCategories**
   - Given: 有効なリクエスト
   - When: GET /api/v1/products を実行
   - Then: レスポンスボディに products, categories フィールドが含まれる

3. **GetProducts_WithCategoryId_ReturnsFilteredProducts**
   - Given: categoryId=1 のクエリパラメータ
   - When: GET /api/v1/products?categoryId=1 を実行
   - Then: 200 OK が返され、categoryId=1 の商品のみ返却される

4. **GetProducts_NonExistentCategoryId_ReturnsEmptyProducts**
   - Given: 存在しないcategoryId=999
   - When: GET /api/v1/products?categoryId=999 を実行
   - Then: 200 OK が返され、空の products 配列が返却される

5. **GetProducts_DatabaseConnectionFailure_Returns503ServiceUnavailable**
   - Given: データベース接続が失敗する環境
   - When: GET /api/v1/products を実行
   - Then: 503 Service Unavailable が返される

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/` ディレクトリ
- 汎用パターン: `patterns/` ディレクトリ（必須）
- ルール定義: `rules/` ディレクトリ

**プレースホルダー置換:**
- `{ProjectName}` → `ProductBE`
- `{EntityName}` → `Product`
- `{DomainName}` → `Product`
- `{DbContextName}` → `ProductDbContext`
- `{route_prefix}` → `products`
- `{TestDatabaseName}` → `ProductTestDb`
- `{ProjectFileName}` → `ProductBE`

**生成するファイル:**

1. `Test/Unit/Product/ProductControllerTests.cs` - テストクラス本体
2. `Test/Unit/Product/CustomWebApplicationFactory.cs` - テスト用Factory（必要に応じて）

```csharp
// ここに生成されたコードを記述
```

```

---

## 6. 出力ファイル一覧

| ファイル | 説明 |
|---------|------|
| `Test/Unit/Product/ProductControllerTests.cs` | ProductController 統合テストクラス |
| `Test/Unit/Product/CustomWebApplicationFactory.cs` | カスタムWebApplicationFactory |

---

## 7. チェックリスト

- [ ] CustomWebApplicationFactory を実装
- [ ] AAA（Arrange-Act-Assert）パターンを使用
- [ ] Given-When-Then形式のコメントを記載
- [ ] GET /api/v1/products のテストケースを作成
- [ ] categoryId クエリパラメータのテストケースを作成
- [ ] エラーハンドリングのテストケースを作成（503 Service Unavailable）
- [ ] レスポンスJSONの形式検証を実装（products, categories フィールド）
