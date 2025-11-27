# コード生成タスク: Task OM-001-P3-E - OrderEntity実装

## 1. 概要

- **ゴール:** Order（注文）、OrderItem（注文明細）エンティティの実装（TDD準拠）
- **対象ファイル:**
  - `OrderManagement/Models/Order.cs` - 注文エンティティ
  - `OrderManagement/Models/OrderItem.cs` - 注文明細エンティティ
- **リポジトリ:** `order_be`

## 1.1. order_beリポジトリに持っていく必要なファイル

**このコード生成指示書:**
- `docs/tasks/OM-001/Task-OM-001-P3-E-OrderEntity/Task-OM-001-P3-E-OrderEntity.md` → order_beリポジトリの `docs/` フォルダにコピー

**参考ファイル（コンテキスト情報）:**
- `docs/tasks/OM-001/Task-OM-001-P3-E-OrderEntity/database-schema.sql` → order_beリポジトリの `docs/` フォルダにコピー
  - Order ServiceのDDL定義（order、order_itemテーブル）を参照

**参考実装ファイル（コーディングパターン参照用）:**
- `docs/tasks/OM-001/Task-OM-001-P3-E-OrderEntity/reference/Candidate.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Entity層のコーディングパターン参照

**実施手順:**
1. 上記ファイルをorder_beリポジトリの適切な場所にコピー
2. `Task-OM-001-P3-E-OrderEntity.md`の「最終コード生成プロンプト」を使用してコード生成
3. 生成されたコードをorder_beプロジェクトに配置
4. OrderRepositoryTests（既存）を実行してTDD検証

## 2. 実装の指針

**STEP 1: Order.cs実装**
- namespace: OrderManagement.Models
- [Table("order")]アトリビュート（PostgreSQL予約語対応）
- プロパティ定義（[Column]アトリビュート使用）
- Navigation Property（List<OrderItem> Items）

**STEP 2: OrderItem.cs実装**
- namespace: OrderManagement.Models
- [Table("order_item")]アトリビュート
- プロパティ定義（[Column]アトリビュート使用）
- 外部キー（OrderId）設定

**STEP 3: データアノテーション**
- [Required]、[MaxLength]、[Column(TypeName = "decimal(precision,scale)")]等
- decimal型の精度指定（total: 10,2、product_discount_percent: 5,2）
- DateTime?型（nullable）の適切な使用

**STEP 4: TDD準拠**
- OrderRepositoryTests（既存テスト）で定義されたプロパティ・リレーションに基づいて実装
- テスト実行してGreen（成功）になることを確認

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**Business Rule: テーブル設計**
- Order（注文）は複数のOrderItem（注文明細）を持つ（1対多リレーション）
- 注文状態（status）は 'IN_ORDER', 'CONFIRMED', 'PAID' の3種類
- member_card_noはnullable（ゲスト注文の場合null）

**Business Rule: データ整合性**
- order_id、order_item_idは自動採番（SERIAL PRIMARY KEY）
- totalはNOT NULL、NUMERIC(10,2)
- statusはCHECK制約で制限（アプリケーション層で検証）
- order_idは外部キー（ON DELETE CASCADE想定）

**Acceptance Criteria:**
**Given:** OrderRepositoryTests（既存テスト）が実行可能
**When:** Order、OrderItemエンティティを実装
**Then:**
- [Table][Column]アトリビュートが正しく設定される
- Order-OrderItem Navigation Propertyが設定される
- decimal型の精度が正しく指定される
- すべてのテストがGreen（成功）になる

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
    product_id INTEGER NOT NULL, -- Product Serviceへの参照（外部キー制約なし）
    product_name VARCHAR(255) NOT NULL, -- スナップショット保存
    product_price NUMERIC(10,2) NOT NULL, -- スナップショット保存
    product_discount_percent NUMERIC(5,2) DEFAULT 0, -- スナップショット保存
    quantity INTEGER NOT NULL
);

-- インデックス作成
CREATE INDEX idx_order_created_at ON "order"(created_at);
CREATE INDEX idx_order_member_card_no ON "order"(member_card_no);
CREATE INDEX idx_order_status ON "order"(status);
CREATE INDEX idx_order_item_order_id ON order_item(order_id);
CREATE INDEX idx_order_item_product_id ON order_item(product_id);
```

### 3.3. 関連API仕様

（このタスクはEntity実装のみ。API仕様は不要）

### 3.4. 関連アーキテクチャ & 既存コード

#### 参考Entity実装 (Candidate.cs)

```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Candidate_BE.Models
{
    [Table("candidates")]
    public class Candidate
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("type")]
        [Required]
        [MaxLength(50)]
        public string Type { get; set; }

        [Column("years_experience")]
        public float? YearsExperience { get; set; }

        [Column("is_approved")]
        public bool IsApproved { get; set; }

        [Column("job_title")]
        [MaxLength(255)]
        public string JobTitle { get; set; }

        // Navigation Properties
        public List<Cloud> Clouds { get; set; }
        public List<Database> Databases { get; set; }
        public List<FrameworkBackend> FrameworksBackend { get; set; }
        public List<FrameworkFrontend> FrameworksFrontend { get; set; }
        public List<OS> OS { get; set; }
        public List<ProgrammingLanguage> ProgrammingLanguages { get; set; }
    }

    [Table("cloud")]
    public class Cloud
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }

    [Table("databases")]
    public class Database
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
```

**適用パターン:**
1. **[Table]アトリビュート**: テーブル名明示（snake_case）
2. **[Column]アトリビュート**: カラム名明示（snake_case）
3. **[Required]アトリビュート**: NOT NULL制約
4. **[MaxLength]アトリビュート**: VARCHAR制約
5. **Navigation Property**: List<T>でリレーション表現（1対多）
6. **nullable型**: int?, float?, DateTime?でNULL許可カラム表現

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0、Entity Framework Coreに精通したシニアソフトウェアエンジニアです。

**ゴール:**
Order Service用のOrder、OrderItemエンティティを作成してください。

**生成対象ファイル:**
1. `OrderManagement/Models/Order.cs` - 注文エンティティ
2. `OrderManagement/Models/OrderItem.cs` - 注文明細エンティティ

**要件:**
1. 上記の「実装の指針」に厳密に従ってください。
2. 「関連コンテキスト」で提供されたビジネスルール、DBスキーマをすべて満たすように実装してください。
3. 参考コード（Candidate.cs）のコーディングスタイルを完全に踏襲してください。
4. OrderRepositoryTests（既存テスト）で定義されたプロパティ・リレーション・制約に基づいて実装してください。
5. 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**Order.cs の実装要件:**
- namespace: OrderManagement.Models
- [Table("order")]アトリビュート（PostgreSQL予約語のため）
- プロパティ:
  - OrderId (order_id, int, PRIMARY KEY)
    - [Column("order_id")]
    - [Key]アトリビュート
  - CreatedAt (created_at, DateTime?, nullable)
    - [Column("created_at")]
  - MemberCardNo (member_card_no, string?, nullable, MaxLength=20)
    - [Column("member_card_no")]
    - [MaxLength(20)]
  - Total (total, decimal, NOT NULL)
    - [Column("total", TypeName = "decimal(10,2)")]
    - [Required]
  - Status (status, string, NOT NULL, MaxLength=16)
    - [Column("status")]
    - [Required]
    - [MaxLength(16)]
- Navigation Property:
  - `public List<OrderItem> Items { get; set; }`

**OrderItem.cs の実装要件:**
- namespace: OrderManagement.Models
- [Table("order_item")]アトリビュート
- プロパティ:
  - OrderItemId (order_item_id, int, PRIMARY KEY)
    - [Column("order_item_id")]
    - [Key]アトリビュート
  - OrderId (order_id, int, FOREIGN KEY)
    - [Column("order_id")]
    - [Required]
  - ProductId (product_id, int)
    - [Column("product_id")]
    - [Required]
  - ProductName (product_name, string, NOT NULL, MaxLength=255)
    - [Column("product_name")]
    - [Required]
    - [MaxLength(255)]
  - ProductPrice (product_price, decimal, NOT NULL)
    - [Column("product_price", TypeName = "decimal(10,2)")]
    - [Required]
  - ProductDiscountPercent (product_discount_percent, decimal?, nullable)
    - [Column("product_discount_percent", TypeName = "decimal(5,2)")]
  - Quantity (quantity, int, NOT NULL)
    - [Column("quantity")]
    - [Required]

**特記事項:**
- テーブル名"order"はPostgreSQLの予約語のため、[Table("order")]で明示
- すべてのカラム名は[Column]アトリビュートでsnake_case指定
- decimal型は[Column(TypeName = "decimal(precision,scale)")]で精度指定
- Navigation Property（List<OrderItem>）はOrderクラスに定義
- OrderItemからOrderへの逆参照（Navigation Property）は不要
- Status値のCHECK制約はアプリケーション層で検証（EF Coreの制約は使用しない）
- created_atはDateTime?型（nullable）、DEFAULT NOW()はデータベース側で設定
- product_discount_percentはdecimal?型（nullable）、DEFAULT 0はデータベース側で設定

**生成するコード:**

以下、各ファイルのコードを生成してください。各ファイルは明確に区切って出力してください。
```
