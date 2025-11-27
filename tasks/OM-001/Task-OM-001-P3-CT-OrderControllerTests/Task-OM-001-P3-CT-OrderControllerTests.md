# コード生成タスク: Task OM-001-P3-CT OrderController テストコード作成

## 1. 概要

- **ゴール:** OrderController（注文管理）のHTTP統合テストコードを作成し、POST /api/v1/orders エンドポイントの動作を検証する
- **対象ファイル:** `Test/Unit/OrderManagement/OrderControllerTests.cs`
- **リポジトリ:** `order_be`

## 2. 実装の指針

### TDD方式の重要性

このタスクは**TDD（テスト駆動開発）方式**で実施します：

1. **テストコードのみを作成**し、テスト対象の実装コード（OrderController, OrderService, OrderRepository等）は作成しない
2. テストは「Red」状態（失敗する状態）で完了することが正しい
3. テストコードで、実装すべきインターフェース（メソッドシグネチャ、戻り値、例外処理）を明確に定義する
4. 必要最小限の例外クラス（EntityNotFoundException等）のみ作成可能

### Controller Test の特別な要件

- **WebApplicationFactory + InMemoryDatabase パターン**を使用した統合テスト
- HTTPリクエスト・レスポンスの実際の動作を検証
- エンドツーエンドのテストケース作成
- CustomWebApplicationFactory を使用してテスト用のアプリケーション環境を構築

### テストメソッド命名規則

`MethodName_Scenario_ExpectedBehavior`

例: `CreateOrder_ValidRequest_Returns201Created`

### AAAパターンの適用

原則として以下の3段階構造で記述：
- **Arrange（準備）**: テストデータ・前提条件の設定
- **Act（実行）**: テスト対象メソッドの呼び出し
- **Assert（検証）**: 期待値と実際の結果の比較

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**PBI: OM-001 - レジモード起動機能 - パーツ3（注文パネル）**

**ビジネスルール:**
- レジモード起動時に、IN_ORDER状態の注文を自動的に作成する
- 会員カード番号はオプション（null可）
- 注文作成時は合計金額0、アイテム数0で初期化される
- ステータスはIN_ORDERで開始される

**受理条件（Given-When-Then形式）:**

**Given:** レジモードが起動される
**When:** POST /api/v1/orders が実行される
**Then:**
- 新規注文が作成され、orderId が返される
- ステータスは IN_ORDER である
- 合計金額は 0 である
- items は空配列である

### 3.2. 関連データベーススキーマ

```sql
-- 注文テーブル
CREATE TABLE "order" (
    order_id SERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    member_card_no VARCHAR(20), -- nullable
    total NUMERIC(10,2) NOT NULL,
    status VARCHAR(16) NOT NULL CHECK (status IN ('IN_ORDER', 'CONFIRMED', 'PAID'))
);

-- 注文明細テーブル
CREATE TABLE order_item (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL REFERENCES "order"(order_id),
    product_id INTEGER NOT NULL,
    product_name VARCHAR(255) NOT NULL,
    product_price NUMERIC(10,2) NOT NULL,
    product_discount_percent NUMERIC(5,2) DEFAULT 0,
    quantity INTEGER NOT NULL
);

-- インデックス
CREATE INDEX idx_order_created_at ON "order"(created_at);
CREATE INDEX idx_order_member_card_no ON "order"(member_card_no);
CREATE INDEX idx_order_status ON "order"(status);
CREATE INDEX idx_order_item_order_id ON order_item(order_id);
```

### 3.3. 関連API仕様

**エンドポイント:** POST /api/v1/orders

**認証方式:** APIキー認証（店内システム）
- ヘッダー: `X-API-Key: shop-system-key`

**HTTPリクエスト:**

```http
POST /api/v1/orders
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "memberCardNo": null
}
```

**HTTPレスポンス（成功）:**

```http
Status: 201 Created
Content-Type: application/json
Body: {
  "orderId": 123,
  "status": "IN_ORDER",
  "total": 0,
  "items": []
}
```

**HTTPレスポンス（エラー）:**

統一エラーレスポンス形式：

```json
{
  "message": "エラーメッセージ",
  "detail": "詳細情報"
}
```

**エラーステータスコード:**
- 400 Bad Request: 無効なリクエストデータ
- 401 Unauthorized: APIキー認証失敗
- 500 Internal Server Error: 内部サーバーエラー
- 503 Service Unavailable: データベース接続失敗

---

## 4. テストケース設計

### POST /api/v1/orders に対するテストケース

1. **正常系テスト（ゲスト注文）**
   - テスト名: `CreateOrder_ValidRequest_Returns201Created`
   - 検証内容: HTTPステータスコードが201であることを確認

2. **正常系テスト（会員注文）+ レスポンスボディ検証**
   - テスト名: `CreateOrder_WithMemberCardNo_ReturnsOrderWithRequiredFields`
   - 検証内容: 
     - HTTPステータスコードが201であることを確認
     - レスポンスに orderId（> 0）, status（"IN_ORDER"）, total（0）, items（空配列）が含まれることを確認

3. **400エラーテスト（無効なJSON）**
   - テスト名: `CreateOrder_InvalidJson_Returns400BadRequest`
   - 検証内容:
     - JSON構文エラー（`{ invalid json }`）で400が返されることを検証
     - エラーレスポンスに`message`と`detail`フィールドが含まれることを検証
   - **注意**: ASP.NET Coreミドルウェアでの処理のため、Controllerに到達しない

4. **DB接続失敗テスト（503）**
   - テスト名: `CreateOrder_DatabaseConnectionFailure_Returns503ServiceUnavailable`
   - 検証内容: DB接続エラー時に503が返されることを確認

**合計テストケース数: 4つ**

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
`Test/Unit/OrderManagement/OrderControllerTests.cs` のテストコードを生成してください。

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたビジネスルール、DBスキーマ、API仕様をすべて満たすように実装してください。
- 汎用パターンファイル（`patterns/backend/`）のコーディングスタイルを完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**TDD方式の重要な注意事項:**
- **テストコードのみを作成し、テスト対象の実装コード（OrderController, OrderService, OrderRepository等）は作成しないでください。**
- テスト対象のクラスは次のタスクで実装されるため、テストは「Red」状態（失敗する状態）で完了します。
- テストコードで、実装すべきインターフェース（メソッドシグネチャ、戻り値、例外処理）を明確に定義してください。
- 必要最小限の例外クラス（EntityNotFoundException等）のみ作成可能です。

**テストパターン:**
- **原則としてAAAパターン（Arrange-Act-Assert）に従って記述してください。**
- 詳細は `rules/backend-test-patterns.md` を参照
- 例外テストやシンプルな検証など、パターンが適さない場合は柔軟に対応可能です。
- 重要なのは、テストの意図が明確で、可読性が高いことです。

**Controller層テスト（最重要）:**
- **`rules/backend-controller-test-standard.md` を必ず参照してください**
- WebApplicationFactoryを使用したHTTP統合テスト
- CustomWebApplicationFactory + InMemoryDatabaseパターンを使用
- **`context/api-integration-design-v2.md`から該当エンドポイント（POST /api/v1/orders）の仕様を抽出**

**CustomWebApplicationFactory 実装の重要な注意事項:**

`patterns/backend/custom-webapplication-factory.cs` を参考に、以下の手順でCustomWebApplicationFactoryを実装してください：

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
                .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
                .Options;

            // Step 3: Singleton + Scopedで明示的に登録
            services.AddSingleton<DbContextOptions<OrderDbContext>>(options);
            services.AddScoped<OrderDbContext>();
        });
    }
}
```

**重要**: `AddDbContext`を使用すると二重プロバイダー登録エラーが発生するため、上記のパターンに従ってください。

**実装するテストケース:**

上記「4. テストケース設計」に記載された4つのテストケースを実装してください：
1. CreateOrder_ValidRequest_Returns201Created
2. CreateOrder_WithMemberCardNo_ReturnsOrderWithRequiredFields
3. CreateOrder_InvalidJson_Returns400BadRequest
4. CreateOrder_DatabaseConnectionFailure_Returns503ServiceUnavailable

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/` ディレクトリ
  - `context/database-schema.sql` - データベーススキーマ定義
  - `context/api-integration-design-v2.md` - API仕様書
- 汎用パターン: `patterns/` ディレクトリ（必須）
  - `patterns/backend/controller-test-pattern.cs` - Controller テストパターン
  - `patterns/backend/custom-webapplication-factory.cs` - テスト用WebApplicationFactory
- ルール定義: `rules/` ディレクトリ
  - `rules/backend-architecture-layers.md` - アーキテクチャ層の役割定義
  - `rules/backend-test-patterns.md` - テストパターン（AAAパターン等）
  - `rules/backend-controller-test-standard.md` - Controllerテスト標準パターン

**生成するコードの骨格:**

```csharp
// Test/Unit/OrderManagement/OrderControllerTests.cs

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace OrderManagement.Tests.Unit.OrderManagement
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // 上記のパターンに従って実装
        }
    }

    public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OrderControllerTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateOrder_ValidRequest_Returns201Created()
        {
            // Arrange
            var request = new { memberCardNo = (string)null };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/v1/orders", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        // 残りのテストメソッドを実装してください
    }
}
```

**注意事項:**
- 各テストメソッドはArrange-Act-Assertパターンに従って実装してください
- レスポンスボディの検証には`JsonSerializer.Deserialize`を使用してください
- DB接続失敗テストでは、新しいWebApplicationFactoryを作成し、不正な接続文字列を設定してください
- テストの意図が明確になるように、XMLコメント（`<summary>`）を各テストメソッドに追加してください
```

---

## 6. 参考情報

### ファイル構成

このタスクディレクトリには、コード生成に必要なすべての情報が含まれています：

```
docs/tasks/OM-001/Task-OM-001-P3-CT-OrderControllerTests/
├── Task-OM-001-P3-CT-OrderControllerTests.md  # この指示書
├── context/                                    # コンテキスト情報
│   ├── database-schema.sql                     # データベーススキーマ定義
│   └── api-integration-design-v2.md            # API仕様書
├── patterns/                                   # 汎用パターン（必須）
│   └── backend/
│       ├── controller-test-pattern.cs          # Controllerテストパターン
│       └── custom-webapplication-factory.cs    # テスト用WebApplicationFactory
└── rules/                                      # ルール定義
    ├── backend-architecture-layers.md          # アーキテクチャ層の役割定義
    ├── backend-test-patterns.md                # テストパターン（AAAパターン等）
    └── backend-controller-test-standard.md     # Controllerテスト標準パターン
```

### 別リポジトリへの移動

このタスクディレクトリ全体を別リポジトリにコピーして使用できます：

```bash
cp -r docs/tasks/OM-001/Task-OM-001-P3-CT-OrderControllerTests /path/to/order_be/docs/tasks/
```

すべての必要情報（指示書、コンテキスト、汎用パターン、ルール定義）が1つのディレクトリにまとまっているため、即座にタスクを実施できます。

---

**最終更新日**: 2025-11-09
