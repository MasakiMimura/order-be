# コード生成タスク: OR-002-1-C ProductController実装

## 1. 概要

- **ゴール:** ProductController（商品・カテゴリ一覧取得API）の実装
- **対象ファイル:** `Controllers/ProductController.cs`
- **リポジトリ:** `backend`

## 2. 実装の指針

- **実装パターン参照**: → 0.1. Backend実装パターン > Controller実装パターン
- **Controllerテストで定義されたエンドポイント・レスポンス形式に基づいて実装**
- **期待される成果**: ASP.NET Core Web APIパターンに完全準拠し、統一エラーハンドリング・HTTPステータスコード統一を適切に実装したControllerクラス
- **必須要素**:
  - [ApiController][Route("api/v1/[controller]")]アトリビュート
  - コンストラクタインジェクション（IProductServiceの注入）
  - [HttpGet] GetProducts([FromQuery] int? categoryId) アクション
  - 統一エラーハンドリング（try-catch-return パターン）
  - HTTPステータスコード統一（200/500/503）
  - X-API-Key ヘッダー認証（認証ミドルウェア使用）
  - XMLドキュメントコメント（<summary>タグ）
- **エラーハンドリング参照**: → 0.5. エラーハンドリングパターン > Backend エラーハンドリング
- **チェックリスト**:
  - [ ] [ApiController][Route]アトリビュートの設定
  - [ ] コンストラクタでIProductServiceを注入
  - [ ] GET /api/v1/products エンドポイント実装
  - [ ] categoryId クエリパラメータ処理
  - [ ] 統一エラーハンドリングの実装（try-catch-return）
  - [ ] HTTPステータスコードの統一（200/500/503）
  - [ ] XMLドキュメントコメントの記載

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**PBI OR-002: カテゴリ・商品表示機能**

このPBIでは、カテゴリ・商品表示機能に必要な以下の機能を実装します：
- GET /api/v1/products（商品・カテゴリ一覧取得）
- カテゴリタブコンポーネント（横スクロール対応）
- 商品カードコンポーネント（名前、価格、キャンペーン情報、画像）
- カテゴリ選択時の商品フィルタリング
- 商品0件時の「商品がありません」メッセージ表示

**受理条件（Acceptance Criteria）**:
- **AC1**: Given レジモードが起動した, When 商品一覧エリアが表示される, Then カテゴリID=1が選択状態として表示される
- **AC2**: Given カテゴリタブエリアが表示されている, When カテゴリ数が多い, Then 横スクロールで全カテゴリが表示される
- **AC3**: Given カテゴリが選択されている, When 該当カテゴリに商品が存在する, Then 商品カード（名前、価格、キャンペーン情報、画像）が表示される
- **AC4**: Given カテゴリが選択されている, When 該当カテゴリに商品が0件, Then 「商品がありません」が表示される
- **AC5**: Given 商品にキャンペーンが設定されている, When 商品カードが表示される, Then キャンペーン情報（割引率、元値、割引後価格）が表示される
- **AC6**: Given Product BE APIが正常にレスポンスを返す, When 商品・カテゴリ情報を取得, Then 1秒以内に商品一覧が表示される

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

**GET /api/v1/products エンドポイント**

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

**レジ画面初期化・商品選択時の使用例**

```http
# Request - 商品情報取得
GET /api/v1/products
Headers: X-API-Key: shop-system-key

# Response Success
{
  "categories": [
    {"categoryId": 1, "categoryName": "ドリンク"},
    {"categoryId": 2, "categoryName": "フード"}
  ],
  "products": [
    {
      "productId": 1,
      "productName": "エスプレッソ",
      "price": 300,
      "isCampaign": false,
      "campaignDiscountPercent": 0,
      "categoryId": 1
    }
  ]
}
```

### 3.4. 関連アーキテクチャ & 既存コード

**Backend レイヤー間連携**
```
Controller → Service → Repository → DbContext → Database
    ↓           ↓            ↓
  HTTP     Business    Data Access
 Request    Logic       (CRUD)
```

**依存性注入パターン**
- **Controller**: コンストラクタで Service を注入
- **Service**: コンストラクタで IRepository を注入
- **Repository**: コンストラクタで DbContext を注入

**エラーハンドリングパターン（統一）**
- **NpgsqlException**: データベース接続エラー → HTTP 503 Service Unavailable
- **TimeoutException**: タイムアウトエラー → HTTP 503 Service Unavailable
- **SocketException**: ネットワーク接続エラー → HTTP 503 Service Unavailable
- **EntityNotFoundException**: エンティティ未検出 → HTTP 404 Not Found
- **InvalidOperationException**: バリデーションエラー → HTTP 400 Bad Request
- **DbUpdateException**: データベース更新エラー → HTTP 500 Internal Server Error
- **Exception**: その他の例外 → HTTP 500 Internal Server Error

---

## 4. 品質基準（order-be 同等）

### 4.1. 実装コードの品質基準

**必須要件:**
- 依存性注入パターン（コンストラクタインジェクション）
- 統一エラーハンドリング（try-catch-return パターン）
- XMLドキュメントコメント（<summary>タグ）
- 非同期処理パターン（async/await、Task<T>）

**エラーハンドリングパターン:**
- `EntityNotFoundException` → HTTP 404 Not Found
- `InvalidOperationException` (DbUpdateExceptionを含む) → HTTP 400 Bad Request
- `NpgsqlException` → HTTP 500 Internal Server Error
- `SocketException` / `TimeoutException` → HTTP 503 Service Unavailable

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
`Controllers/ProductController.cs`のコードを生成してください。

**品質基準: order-be 同等**
- ASP.NET Core Web API パターンに完全準拠
- 統一エラーハンドリング（try-catch-return パターン）
- HTTPステータスコード統一（200/500/503）
- XMLドキュメントコメント（<summary>タグ）

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたビジネスルール、DBスキーマ、API仕様をすべて満たすように実装してください。
- **汎用パターン（`patterns/`）を厳密に参照してください。**
  - プレースホルダー（`{EntityName}`, `{ProjectName}`, `{DbContextName}`等）を実際の値に置換してください。
  - パターンファイルの構造・命名規則・コーディングスタイルを完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**実装する機能:**
1. **GET /api/v1/products エンドポイント**
   - クエリパラメータ: `categoryId` (オプション)
   - レスポンス: `{ products: [...], categories: [...] }`
   - 正常時: HTTP 200 OK

2. **コンストラクタインジェクション**
   - IProductService を注入

3. **エラーハンドリング**
   - NpgsqlException → HTTP 500 Internal Server Error
   - SocketException → HTTP 503 Service Unavailable
   - TimeoutException → HTTP 503 Service Unavailable
   - DbUpdateException → HTTP 500 Internal Server Error
   - Exception → HTTP 500 Internal Server Error

**プレースホルダー置換:**
- `{ProjectName}` → `ProductBE` (または実際のプロジェクト名)
- `{EntityName}` → `Product`
- `{route_prefix}` → `products`

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/` ディレクトリ
- 汎用パターン: `patterns/` ディレクトリ（必須）
- ルール定義: `rules/` ディレクトリ

**生成するコード:**

```csharp
// ここに生成されたコードを記述
```

```
