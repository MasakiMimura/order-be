# コード生成タスク: OR-002-1-E Product・Category Entity実装

## 1. 概要

- **ゴール:** Product・Category Entityクラスを作成し、データベーススキーマに完全準拠したNavigation Property・データアノテーションを適切に設定する
- **対象ファイル:**
  - `Models/Product.cs`
  - `Models/Category.cs`
- **リポジトリ:** `backend`

## 2. 実装の指針

- **実装パターン参照**: Entity実装パターン（0.1. Backend実装パターン）
- **Repositoryテストで定義されたプロパティ・リレーション・制約に基づいて実装**
- **期待される成果**: データベーススキーマに完全準拠し、Navigation Property・データアノテーションを適切に設定したEntityクラス
- **必須要素**:
  - **Product Entity**:
    - [Table("product")][Column]アトリビュート
    - ProductId, ProductName, Price, IsCampaign, CampaignDiscountPercent, CategoryId, RecipeId, IsActive, CreatedAt, UpdatedAt
    - Navigation Property: Category（多対1）
  - **Category Entity**:
    - [Table("category")][Column]アトリビュート
    - CategoryId, CategoryName, DisplayOrder, CreatedAt, UpdatedAt
    - Navigation Property: Products（1対多）

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**PBI OR-002: カテゴリ・商品表示機能**

**Business Rules:**
- カテゴリの動的表示: カテゴリは商品マスタから取得され、動的に表示される
- 商品カード表示項目: 商品カードには名前、価格、キャンペーン情報、画像が表示される
- 初期表示時の選択状態: 初期表示時はカテゴリID=1（最初のカテゴリ）を選択状態とする
- 商品0件の場合の表示: カテゴリに商品が0件の場合は「商品がありません」を表示する
- カテゴリ数が多い場合の対応: 横スクロールで対応

**Acceptance Criteria:**
- Given レジモードが起動した, When 商品一覧エリアが表示される, Then カテゴリID=1が選択状態として表示される
- Given カテゴリタブエリアが表示されている, When カテゴリ数が多い, Then 横スクロールで全カテゴリが表示される
- Given カテゴリが選択されている, When 該当カテゴリに商品が存在する, Then 商品カード（名前、価格、キャンペーン情報、画像）が表示される
- Given カテゴリが選択されている, When 該当カテゴリに商品が0件, Then 「商品がありません」が表示される
- Given 商品にキャンペーンが設定されている, When 商品カードが表示される, Then キャンペーン情報（割引率、元値、割引後価格）が表示される
- Given Product BE APIが正常にレスポンスを返す, When 商品・カテゴリ情報を取得, Then 1秒以内に商品一覧が表示される

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
CREATE INDEX idx_product_recipe_id ON product(recipe_id);
CREATE INDEX idx_product_category_id ON product(category_id);
CREATE INDEX idx_product_is_active ON product(is_active);
```

### 3.3. 関連API仕様

このタスクはEntity実装のため、直接的なAPI仕様の参照は不要です。
ただし、Entityが提供するデータは以下のAPIで使用されます：

- GET /api/v1/products（商品・カテゴリ一覧取得）
  - レスポンス: カテゴリ一覧、商品一覧、価格、キャンペーン情報

### 3.4. 関連アーキテクチャ & 既存コード

**Entity クラスの役割（backend-architecture-layers.md より）:**

Repository層がデータベースとのやり取りに使用するデータ構造を定義します。
- SQLやORMを通じて実データベースにアクセス
- Entity Framework Core のCode Firstパターンに準拠
- [Table][Column]アトリビュートによるテーブル・カラムマッピング
- Navigation Propertyによるリレーション定義

**層間の依存関係:**
```
Controller → Service → Repository → Database
    ↓          ↓           ↓
  HTTP     Business    Data Access
 Request    Logic       (CRUD)
```

---

## 4. 品質基準

### 4.1. Entity実装の品質基準

**必須要件:**
- [Table]アトリビュートによるテーブル名マッピング
- [Column]アトリビュートによるカラム名マッピング（スネークケース→パスカルケース変換）
- [Key]アトリビュートによる主キー設定
- [Required]アトリビュートによる必須フィールド設定
- [MaxLength]アトリビュートによる文字列長制限
- Navigation Propertyによる双方向リレーション設定
- XMLドキュメントコメント（<summary>タグ）

**データ型マッピング:**
- SERIAL → int（自動採番）
- VARCHAR(n) → string + [MaxLength(n)]
- INTEGER → int
- BOOLEAN → bool
- TIMESTAMPTZ → DateTime
- DECIMAL(p, s) → decimal

**命名規則:**
- テーブル名: スネークケース（例: `category`, `product`）
- C#プロパティ名: パスカルケース（例: `CategoryId`, `ProductName`）
- カラムマッピング: [Column("snake_case_name")]

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
以下のファイルのコードを生成してください：
- `Models/Product.cs`
- `Models/Category.cs`

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたDBスキーマをすべて満たすように実装してください。
- **汎用パターン（`patterns/backend/entity-pattern.cs`）を厳密に参照してください。**
  - プレースホルダー（`{EntityName}`, `{ProjectName}`等）を実際の値に置換してください。
  - パターンファイルの構造・命名規則・コーディングスタイルを完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**プレースホルダー置換:**
- `{ProjectName}` → `ProductService`（または実際のプロジェクト名）
- `{EntityName}` → `Product` / `Category`
- `{table_name}` → `product` / `category`
- `{id_column}` → `product_id` / `category_id`

**Product Entity 必須プロパティ:**
- ProductId (int, PK, product_id)
- ProductName (string, Required, MaxLength(255), product_name)
- RecipeId (int, Required, recipe_id)
- CategoryId (int, Required, FK, category_id)
- Price (int, Required, price)
- IsCampaign (bool, is_campaign, default: false)
- CampaignDiscountPercent (int, campaign_discount_percent, default: 0)
- IsActive (bool, is_active, default: true)
- CreatedAt (DateTime, created_at)
- UpdatedAt (DateTime, updated_at)
- Category (Navigation Property, 多対1)

**Category Entity 必須プロパティ:**
- CategoryId (int, PK, category_id)
- CategoryName (string, Required, MaxLength(255), Unique, category_name)
- DisplayOrder (int, display_order, default: 0)
- CreatedAt (DateTime, created_at)
- UpdatedAt (DateTime, updated_at)
- Products (Navigation Property, 1対多, ICollection<Product>)

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/database-schema.sql`
- 汎用パターン: `patterns/backend/entity-pattern.cs`
- ルール定義: `rules/backend-architecture-layers.md`

**生成するコード:**

```csharp
// Models/Category.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    /// <summary>
    /// カテゴリエンティティ
    /// データベーステーブル: category
    /// </summary>
    [Table("category")]
    public class Category
    {
        /// <summary>
        /// Primary Key: カテゴリの一意識別子
        /// </summary>
        [Key]
        [Column("category_id")]
        public int CategoryId { get; set; }

        /// <summary>
        /// カテゴリ名
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("category_name")]
        public string CategoryName { get; set; } = string.Empty;

        /// <summary>
        /// 表示順序
        /// </summary>
        [Column("display_order")]
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// 作成日時
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Navigation Property: このカテゴリに属する商品一覧（1対多）
        /// </summary>
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
```

```csharp
// Models/Product.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProductService.Models
{
    /// <summary>
    /// 商品エンティティ
    /// データベーステーブル: product
    /// </summary>
    [Table("product")]
    public class Product
    {
        /// <summary>
        /// Primary Key: 商品の一意識別子
        /// </summary>
        [Key]
        [Column("product_id")]
        public int ProductId { get; set; }

        /// <summary>
        /// 商品名
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("product_name")]
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// レシピID（外部キー）
        /// </summary>
        [Column("recipe_id")]
        public int RecipeId { get; set; }

        /// <summary>
        /// カテゴリID（外部キー）
        /// </summary>
        [Column("category_id")]
        public int CategoryId { get; set; }

        /// <summary>
        /// 価格（税込）
        /// </summary>
        [Column("price")]
        public int Price { get; set; }

        /// <summary>
        /// キャンペーン対象フラグ
        /// </summary>
        [Column("is_campaign")]
        public bool IsCampaign { get; set; } = false;

        /// <summary>
        /// キャンペーン割引率（0-100%）
        /// </summary>
        [Column("campaign_discount_percent")]
        public int CampaignDiscountPercent { get; set; } = 0;

        /// <summary>
        /// 有効フラグ
        /// </summary>
        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 作成日時
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新日時
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Navigation Property: 所属カテゴリ（多対1）
        /// </summary>
        public Category? Category { get; set; }
    }
}
```
```

---

## 6. チェックリスト

- [ ] データベーススキーマ（database-schema.sql）に従ったプロパティ定義
- [ ] Navigation Propertyの双方向設定（Product ↔ Category）
- [ ] 主キー（ProductId, CategoryId）の設定
- [ ] 外部キー（CategoryId）の設定
- [ ] データアノテーション（[Required][MaxLength]等）の設定
- [ ] [Table][Column]アトリビュートの設定
- [ ] XMLドキュメントコメントの記載
- [ ] プロパティの初期値設定（bool, int のデフォルト値）
