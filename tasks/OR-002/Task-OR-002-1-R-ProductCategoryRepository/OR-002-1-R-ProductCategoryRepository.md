# コード生成タスク: OR-002-1-R ProductRepository・CategoryRepository実装

## 1. 概要

- **ゴール:** Product Service のデータアクセス層（Repository）を実装し、商品・カテゴリ情報のデータベース取得機能を提供する
- **対象ファイル:**
  - `Repository/ProductRepository.cs`
  - `Repository/IProductRepository.cs`
  - `Repository/CategoryRepository.cs`
  - `Repository/ICategoryRepository.cs`
- **リポジトリ:** `backend`（Product Service）

## 2. 実装の指針

### 実装パターン参照
→ 0.1. Backend実装パターン > Repository実装パターン

### 実装指針
Repositoryテストで定義されたインターフェース・例外処理・CRUD操作に基づいて実装

### 期待される成果
Entity Framework Core操作パターンに完全準拠し、統一例外ハンドリング・非同期処理・Include/ThenIncludeを適切に実装したRepositoryクラス

### 必須要素
- **IProductRepository**:
  - `Task<IEnumerable<Product>> GetAllProductsAsync()`
  - `Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId)`
  - `Task<IEnumerable<Product>> GetActiveProductsAsync()`
- **ICategoryRepository**:
  - `Task<IEnumerable<Category>> GetAllCategoriesAsync()`
- Entity Framework Core操作パターン（DbContext使用）
- 非同期処理パターン（async/await、Task<T>）
- Include によるカテゴリ情報取得
- 統一例外ハンドリング（NpgsqlException処理）

### エラーハンドリング参照
→ 0.5. エラーハンドリングパターン > Backend エラーハンドリング

### チェックリスト
- [ ] IProductRepository、ICategoryRepository インターフェースの定義
- [ ] GetAllProductsAsync 実装（Include Category）
- [ ] GetProductsByCategoryIdAsync 実装
- [ ] GetActiveProductsAsync 実装（is_active=true フィルター）
- [ ] GetAllCategoriesAsync 実装（display_order順）
- [ ] 非同期処理パターンの適用（async/await）
- [ ] 例外ハンドリングの実装

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**OR-002: カテゴリ・商品表示機能**

#### Business Rules
- **カテゴリの動的表示**: カテゴリは商品マスタから取得され、動的に表示される
- **商品カード表示項目**: 商品カードには名前、価格、キャンペーン情報、画像が表示される
- **初期表示時の選択状態**: 初期表示時はカテゴリID=1（最初のカテゴリ）を選択状態とする
- **商品0件の場合の表示**: カテゴリに商品が0件の場合は「商品がありません」を表示する
- **カテゴリ数が多い場合の対応**: 横スクロールで対応

#### Acceptance Criteria
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

### 3.3. 関連Entity（前提として実装済み）

**Product Entity** (`Models/Product.cs`):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    [Table("product")]
    public class Product
    {
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        [Required]
        [Column("product_name")]
        [MaxLength(255)]
        public string ProductName { get; set; }

        [Column("recipe_id")]
        public int RecipeId { get; set; }

        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [Column("price")]
        public int Price { get; set; }

        [Column("is_campaign")]
        public bool IsCampaign { get; set; }

        [Column("campaign_discount_percent")]
        public int CampaignDiscountPercent { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation Property
        public Category Category { get; set; }
    }
}
```

**Category Entity** (`Models/Category.cs`):
```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    [Table("category")]
    public class Category
    {
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        [Required]
        [Column("category_name")]
        [MaxLength(255)]
        public string CategoryName { get; set; }

        [Column("display_order")]
        public int DisplayOrder { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        // Navigation Property
        public ICollection<Product> Products { get; set; }
    }
}
```

### 3.4. エラーハンドリングパターン

| 例外 | HTTPステータスコード |
|------|---------------------|
| NpgsqlException | 503 Service Unavailable |
| TimeoutException | 503 Service Unavailable |
| SocketException | 503 Service Unavailable |
| EntityNotFoundException | 404 Not Found |
| InvalidOperationException | 400 Bad Request |
| DbUpdateException | 500 Internal Server Error |
| Exception | 500 Internal Server Error |

---

## 4. 品質基準（order-be 同等）

### 4.1. Repository実装の品質基準

**必須要件:**
- Entity Framework Core 操作パターン（DbContext使用）
- 非同期処理パターン（async/await、Task<T>）
- Include/ThenInclude によるリレーションデータ取得
- 統一例外ハンドリング（NpgsqlException処理）

**例外処理パターン:**
```csharp
try
{
    // データベース操作
}
catch (NpgsqlException ex)
{
    _logger.LogError(ex, "PostgreSQL connection error.");
    throw;
}
catch (SocketException ex)
{
    _logger.LogError(ex, "Network connection error.");
    throw;
}
catch (TimeoutException ex)
{
    _logger.LogError(ex, "Database connection timeout.");
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error.");
    throw new Exception($"Repository error: {ex.Message}", ex);
}
```

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
以下のファイルのコードを生成してください：
- `Repository/IProductRepository.cs`
- `Repository/ProductRepository.cs`
- `Repository/ICategoryRepository.cs`
- `Repository/CategoryRepository.cs`

**品質基準: order-be 同等**
- Entity Framework Core 操作パターン（DbContext使用）
- 非同期処理パターン（async/await、Task<T>）
- Include によるリレーションデータ取得
- 統一例外ハンドリング（NpgsqlException処理）

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたビジネスルール、DBスキーマをすべて満たすように実装してください。
- **汎用パターン（`patterns/`）を厳密に参照してください。**
  - プレースホルダー（`{EntityName}`, `{ProjectName}`, `{DbContextName}`等）を実際の値に置換してください。
  - パターンファイルの構造・命名規則・コーディングスタイルを完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**IProductRepositoryインターフェース:**
```csharp
public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllProductsAsync();
    Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId);
    Task<IEnumerable<Product>> GetActiveProductsAsync();
}
```

**ICategoryRepositoryインターフェース:**
```csharp
public interface ICategoryRepository
{
    Task<IEnumerable<Category>> GetAllCategoriesAsync();
}
```

**ProductRepository実装要件:**
- GetAllProductsAsync: 全商品取得（Include Category）
- GetProductsByCategoryIdAsync: カテゴリID指定で商品取得
- GetActiveProductsAsync: is_active=trueの商品のみ取得（Include Category）

**CategoryRepository実装要件:**
- GetAllCategoriesAsync: 全カテゴリ取得（display_order順にソート）

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/` ディレクトリ
- 汎用パターン: `patterns/` ディレクトリ（必須）
- ルール定義: `rules/` ディレクトリ

**プレースホルダー置換例:**
汎用パターンファイル（`patterns/`）には以下のプレースホルダーが含まれています：
- `{ProjectName}` → `ProductService`
- `{EntityName}` → `Product` または `Category`
- `{DbContextName}` → `ProductDbContext`
- `{EntityPluralName}` → `Products` または `Categories`

これらを実際のプロジェクトに合わせて置換してください。

**生成するコード:**

```csharp
// IProductRepository.cs
// ここに生成されたコードを記述

// ProductRepository.cs
// ここに生成されたコードを記述

// ICategoryRepository.cs
// ここに生成されたコードを記述

// CategoryRepository.cs
// ここに生成されたコードを記述
```
```
