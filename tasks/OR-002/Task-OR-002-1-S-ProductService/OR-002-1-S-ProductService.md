# コード生成タスク: OR-002-1-S ProductService実装

## 1. 概要

- **ゴール:** ProductService実装（商品・カテゴリ一覧取得、カテゴリ別フィルタリング、レスポンス形式生成）
- **対象ファイル:** `Service/ProductService.cs`
- **リポジトリ:** `backend`

## 2. 実装の指針

### 実装パターン参照
→ 0.1. Backend実装パターン > Service実装パターン

### 実装指針
Serviceテストで定義されたビジネスルール・メソッドシグネチャに基づいて実装

### 期待される成果
ビジネスロジック層をカプセル化し、Repository協調・レスポンス形式生成・例外の透過的伝播を適切に実装したServiceクラス

### 必須要素
- コンストラクタインジェクション（IProductRepository、ICategoryRepositoryの注入）
- GetProductsWithCategoriesAsync(int? categoryId) メソッド
- カテゴリ別商品フィルタリングロジック
- レスポンス形式生成（products配列、categories配列）
- 例外の透過的な伝播（catch-throw パターン）

### 依存性注入参照
→ 0.6. 依存性注入・協調パターン

### チェックリスト
- [ ] コンストラクタでIProductRepository、ICategoryRepositoryを注入
- [ ] GetProductsWithCategoriesAsync 実装
- [ ] categoryId パラメータによるフィルタリング処理
- [ ] レスポンス形式（products、categories）の生成
- [ ] 例外の透過的な伝播の実装

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

#### PBI OR-002: カテゴリ・商品表示機能

**Business Rules:**
- カテゴリの動的表示: カテゴリは商品マスタから取得され、動的に表示される
- 商品カード表示項目: 商品カードには名前、価格、キャンペーン情報、画像が表示される
- 初期表示時の選択状態: 初期表示時はカテゴリID=1（最初のカテゴリ）を選択状態とする
- 商品0件の場合の表示: カテゴリに商品が0件の場合は「商品がありません」を表示する
- カテゴリ数が多い場合の対応: 横スクロールで対応

**Acceptance Criteria:**
- [ ] Given レジモードが起動した, When 商品一覧エリアが表示される, Then カテゴリID=1が選択状態として表示される
- [ ] Given カテゴリタブエリアが表示されている, When カテゴリ数が多い, Then 横スクロールで全カテゴリが表示される
- [ ] Given カテゴリが選択されている, When 該当カテゴリに商品が存在する, Then 商品カード（名前、価格、キャンペーン情報、画像）が表示される
- [ ] Given カテゴリが選択されている, When 該当カテゴリに商品が0件, Then 「商品がありません」が表示される
- [ ] Given 商品にキャンペーンが設定されている, When 商品カードが表示される, Then キャンペーン情報（割引率、元値、割引後価格）が表示される
- [ ] Given Product BE APIが正常にレスポンスを返す, When 商品・カテゴリ情報を取得, Then 1秒以内に商品一覧が表示される

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

**GET /api/v1/products（商品・カテゴリ一覧取得）**

```http
# Request
GET /api/v1/products?categoryId=&search=
Headers: X-API-Key: shop-system-key

# Response Success
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

# Response Error
{"error": "InternalServerError", "message": "商品データの取得に失敗しました"}
```

### 3.4. 関連アーキテクチャ & 既存コード

#### Serviceテストで定義されたテスト内容（TDDベース）
- 商品・カテゴリ一覧取得（GetProductsWithCategoriesAsync）
- カテゴリ別商品フィルタリング（categoryIdパラメータ）
- キャンペーン商品の割引後価格計算（price × (100 - campaign_discount_percent) / 100）
- レスポンス形式の検証（products配列、categories配列）
- 例外の透過的な伝播確認

#### Serviceテストで定義されたテストケース
- 正常系: 商品一覧とカテゴリ一覧が正しく取得される
- 正常系: カテゴリID指定で該当商品のみフィルタリングされる
- 正常系: キャンペーン商品の割引後価格が正しく計算される
- 異常系: Repository からの例外が Service を透過して伝播する
- 境界値: 商品が0件の場合、空のproducts配列が返される

#### 依存するリポジトリインターフェース（想定）
- **IProductRepository**:
  - Task<IEnumerable<Product>> GetAllProductsAsync()
  - Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId)
  - Task<IEnumerable<Product>> GetActiveProductsAsync()
- **ICategoryRepository**:
  - Task<IEnumerable<Category>> GetAllCategoriesAsync()

#### レイヤー間連携パターン
```
Controller → Service → Repository → DbContext → Database
    ↓           ↓            ↓
  DTO変換    ビジネス    CRUD操作
            ロジック
```

---

## 4. 品質基準（order-be / coffee-shop-fe 同等）

### 4.1. 実装コードの品質基準

**必須要件:**
- 依存性注入パターン（コンストラクタインジェクション）
- 統一エラーハンドリング（try-catch-throw パターン）
- XMLドキュメントコメント（<summary>タグ）
- 非同期処理パターン（async/await、Task<T>）

**Service層の責務:**
- Repository層を呼び出してデータ操作を行う
- データの前処理・計算・検証・条件分岐を担う
- 受け入れ条件（入力チェック、ドメインルールの適用など）を定義
- 「何をどう扱うか」の判断・手順を担う

**エラーハンドリングパターン:**
- `InvalidOperationException` → Controller層でBad Request (400)として処理
- `EntityNotFoundException` → Controller層でNot Found (404)として処理
- `NpgsqlException` → Controller層でService Unavailable (503)として処理
- `SocketException` / `TimeoutException` → Controller層でService Unavailable (503)として処理
- `DbUpdateException` → Controller層でService Unavailable (503)として処理
- 予期しない`Exception` → 新しい例外でラップしてService層の文脈情報を追加

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
`Service/ProductService.cs` のコードを生成してください。

**品質基準: order-be 同等**
- 依存性注入パターン（コンストラクタインジェクション）
- 統一エラーハンドリング（try-catch-throw パターン）
- XMLドキュメントコメント（<summary>タグ）
- 非同期処理パターン（async/await、Task<T>）

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたビジネスルール、DBスキーマ、API仕様をすべて満たすように実装してください。
- **汎用パターン（`patterns/backend/service-pattern.cs`）を厳密に参照してください。**
  - プレースホルダー（`{EntityName}`, `{ProjectName}`等）を実際の値に置換してください。
  - パターンファイルの構造・命名規則・コーディングスタイルを完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**実装すべきメソッド:**

1. **GetProductsWithCategoriesAsync(int? categoryId)**
   - Repository層から商品一覧とカテゴリ一覧を取得
   - categoryIdが指定されている場合、該当カテゴリの商品のみフィルタリング
   - categoryIdがnullの場合、アクティブな全商品を返却
   - レスポンス形式（products配列、categories配列）を生成して返却
   - 例外の透過的な伝播（NpgsqlException, TimeoutException, SocketException, DbUpdateException）

**レスポンス形式の例:**
```csharp
public class ProductListResponse
{
    public IEnumerable<ProductDto> Products { get; set; }
    public IEnumerable<CategoryDto> Categories { get; set; }
}

public class ProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int Price { get; set; }
    public bool IsCampaign { get; set; }
    public int CampaignDiscountPercent { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public int RecipeId { get; set; }
    public string RecipeName { get; set; }
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
}
```

**注意事項:**
- IProductRepository と ICategoryRepository をコンストラクタインジェクションで注入
- Product Entity には Category への Navigation Property が含まれる想定
- is_active = true の商品のみを返却する
- カテゴリ一覧は display_order 順で返却する
- 例外はService層で catch して再スローする（透過的伝播）

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/` ディレクトリ
- 汎用パターン: `patterns/backend/service-pattern.cs`（必須）
- ルール定義: `rules/` ディレクトリ

**プレースホルダー置換:**
- `{ProjectName}` → `ProductBE`（または実際のプロジェクト名）
- `{EntityName}` → `Product`
- `{DomainName}` → `Product`
- `{EntityPluralName}` → `Products`

**生成するコード:**

```csharp
// ここに生成されたコードを記述
```
```

---

## 6. 補足情報

### 6.1. 依存するRepository インターフェース（想定実装済み）

**IProductRepository:**
```csharp
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId);
    Task<IEnumerable<Product>> GetActiveProductsAsync();
}
```

**ICategoryRepository:**
```csharp
public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
}
```

### 6.2. Entity 構造（想定）

**Product Entity:**
```csharp
public class Product
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public int RecipeId { get; set; }
    public int CategoryId { get; set; }
    public int Price { get; set; }
    public bool IsCampaign { get; set; }
    public int CampaignDiscountPercent { get; set; }
    public bool IsActive { get; set; }

    // Navigation Property
    public Category Category { get; set; }
    public Recipe Recipe { get; set; }
}
```

**Category Entity:**
```csharp
public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; }
    public int DisplayOrder { get; set; }

    // Navigation Property
    public ICollection<Product> Products { get; set; }
}
```

### 6.3. ディレクトリ構成

```
tasks/OR-002/OR-002-1-S-ProductService/
├── OR-002-1-S-ProductService.md     # この指示書
├── context/                          # コンテキスト情報
│   ├── database-schema.sql
│   └── api-integration-design.md
├── patterns/                         # 汎用パターン
│   └── backend/
│       ├── service-pattern.cs
│       └── service-test-pattern.cs
└── rules/                            # ルールファイル
    ├── backend-architecture-layers.md
    └── backend-test-patterns.md
```
