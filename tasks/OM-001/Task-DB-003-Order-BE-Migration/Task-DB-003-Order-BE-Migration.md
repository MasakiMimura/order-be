# コード生成タスク: Task DB-003 Order BE - DDL実装・Migration作成

## 1. 概要

- **ゴール:** Order（注文）、OrderItem（注文アイテム）テーブルのEntity Framework Core Migration作成
- **対象ファイル:**
  - Entity Framework Core Migrationファイル
  - DbContextクラス
- **リポジトリ:** `order_be`

## 1.1. order_beリポジトリに持っていく必要なファイル

**このコード生成指示書:**
- `docs/tasks/OM-001/Task-DB-003-Order-BE-Migration.md` → order_beリポジトリの `docs/` フォルダにコピー

**参考ファイル（コンテキスト情報）:**
- `docs/database-schema.sql` → order_beリポジトリの `docs/` フォルダにコピー
  - Order ServiceのDDL定義（order、order_itemテーブル）を参照
  - 既存スキーマとの整合性確認用

**参考実装ファイル（コーディングパターン参照用）:**
- `docs/candidate_be/Data/SkillDbContext.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
- `docs/candidate_be/Models/Candidate.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー

**実施手順:**
1. 上記ファイルをorder_beリポジトリの適切な場所にコピー
2. `Task-DB-003-Order-BE-Migration.md`の「最終コード生成プロンプト」を使用してコード生成
3. 生成されたコードをorder_beプロジェクトに配置
4. `dotnet ef migrations add InitialCreate`を実行
5. `dotnet ef database update`でデータベース作成

## 2. 実装の指針

**STEP 1: DbContext作成**
- Entity Framework Core 8.0のDbContextクラス実装
- Order、OrderItemエンティティの登録
- PostgreSQL接続設定
- OnModelCreatingでリレーション・制約を定義

**STEP 2: Entity定義**
- Order.cs: 注文エンティティ（order_id, created_at, member_card_no, total, status）
- OrderItem.cs: 注文明細エンティティ（order_item_id, order_id, product_id, product_name, product_price, product_discount_percent, quantity）
- Navigation Property設定（Order.Items）

**STEP 3: Migration作成**
- `dotnet ef migrations add InitialCreate`コマンドで初期Migrationを作成
- Up/Downメソッドの確認
- インデックス作成の追加

**STEP 4: DDL出力**
- Migrationから生成されるDDLスクリプトを確認
- database-schema.sqlとの整合性確認

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
- statusはCHECK制約で制限
- order_idは外部キー（ON DELETE CASCADE想定）

**Acceptance Criteria:**
**Given:** order_beプロジェクトが作成済み
**When:** Entity Framework Core Migrationを実行
**Then:**
- Orderテーブルが作成される
- OrderItemテーブルが作成される
- 外部キー制約が設定される
- インデックスが作成される
- データベース接続が成功する

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

（このタスクではAPI実装は不要。データベーススキーマのみ）

### 3.4. 関連アーキテクチャ & 既存コード

#### 参考DbContext実装 (SkillDbContext.cs)

```csharp
using Microsoft.EntityFrameworkCore;
using Candidate_BE.Models;

namespace Candidate_BE.Data
{
    public class SkillDbContext : DbContext
    {
        public SkillDbContext(DbContextOptions<SkillDbContext> options) : base(options)
        {
        }

        public DbSet<Candidate> Candidates { get; set; }
        public DbSet<Cloud> Clouds { get; set; }
        public DbSet<Database> Databases { get; set; }
        public DbSet<FrameworkBackend> FrameworksBackend { get; set; }
        public DbSet<FrameworkFrontend> FrameworksFrontend { get; set; }
        public DbSet<OS> OS { get; set; }
        public DbSet<ProgrammingLanguage> ProgrammingLanguages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // リレーション設定
            modelBuilder.Entity<Candidate>()
                .HasMany(c => c.Clouds)
                .WithOne()
                .HasForeignKey(cl => cl.CandidateId);

            modelBuilder.Entity<Candidate>()
                .HasMany(c => c.Databases)
                .WithOne()
                .HasForeignKey(d => d.CandidateId);

            // インデックス設定
            modelBuilder.Entity<Candidate>()
                .HasIndex(c => c.Type);
        }
    }
}
```

**適用パターン:**
1. **DbContext継承**: `DbContext`を継承
2. **コンストラクタ**: `DbContextOptions<T>`を受け取る
3. **DbSet定義**: 各エンティティを`DbSet<T>`プロパティで公開
4. **OnModelCreating**: リレーション・インデックス・制約を定義
5. **HasMany-WithOne**: 1対多リレーション定義
6. **HasForeignKey**: 外部キー指定
7. **HasIndex**: インデックス作成

#### 参考Entity実装 (Candidate.cs)

```csharp
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Candidate_BE.Models
{
    [Table("candidates")]
    public class Candidate
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("type")]
        public string Type { get; set; }

        public List<Cloud> Clouds { get; set; }
    }

    [Table("cloud")]
    public class Cloud
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("candidate_id")]
        public int CandidateId { get; set; }

        [Column("name")]
        public string Name { get; set; }
    }
}
```

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0、Entity Framework Coreに精通したシニアソフトウェアエンジニアです。

**ゴール:**
Order Service用のDbContext、Entity、Migrationを作成してください。

**生成対象ファイル:**
1. `OrderManagement/Data/OrderDbContext.cs` - DbContextクラス
2. `OrderManagement/Models/Order.cs` - 注文エンティティ
3. `OrderManagement/Models/OrderItem.cs` - 注文明細エンティティ
4. Migration作成手順（コマンド）

**要件:**
1. 上記の「実装の指針」に厳密に従ってください。
2. 「関連コンテキスト」で提供されたDBスキーマ、参考コードをすべて満たすように実装してください。
3. 参考コード（SkillDbContext.cs、Candidate.cs）のコーディングスタイルを完全に踏襲してください。
4. 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。
5. Entity Framework Core 8.0のベストプラクティスに従ってください。

**OrderDbContext.cs の実装要件:**
- namespace: OrderManagement.Data
- DbContextを継承
- コンストラクタ: `DbContextOptions<OrderDbContext>`を受け取る
- DbSet定義:
  - `public DbSet<Order> Orders { get; set; }`
  - `public DbSet<OrderItem> OrderItems { get; set; }`
- OnModelCreatingメソッド:
  - Order-OrderItem リレーション設定（1対多、HasMany-WithOne-HasForeignKey）
  - インデックス設定:
    - Order.CreatedAt
    - Order.MemberCardNo
    - Order.Status
    - OrderItem.OrderId
    - OrderItem.ProductId

**Order.cs の実装要件:**
- namespace: OrderManagement.Models
- [Table("order")]アトリビュート
- プロパティ:
  - OrderId (order_id, int, PRIMARY KEY)
  - CreatedAt (created_at, DateTime?, nullable)
  - MemberCardNo (member_card_no, string, nullable, MaxLength=20)
  - Total (total, decimal, [Column(TypeName = "decimal(10,2)")])
  - Status (status, string, NOT NULL, MaxLength=16)
- Navigation Property:
  - `public List<OrderItem> Items { get; set; }`

**OrderItem.cs の実装要件:**
- namespace: OrderManagement.Models
- [Table("order_item")]アトリビュート
- プロパティ:
  - OrderItemId (order_item_id, int, PRIMARY KEY)
  - OrderId (order_id, int, FOREIGN KEY)
  - ProductId (product_id, int)
  - ProductName (product_name, string, NOT NULL, MaxLength=255)
  - ProductPrice (product_price, decimal, [Column(TypeName = "decimal(10,2)")])
  - ProductDiscountPercent (product_discount_percent, decimal?, nullable, [Column(TypeName = "decimal(5,2)")])
  - Quantity (quantity, int, NOT NULL)

**Migration作成手順:**
プロジェクトルートで以下のコマンドを実行してください：
```bash
# Migration作成
dotnet ef migrations add InitialCreate --project OrderManagement --startup-project OrderManagement

# データベース更新
dotnet ef database update --project OrderManagement --startup-project OrderManagement
```

**特記事項:**
- PostgreSQL接続文字列は appsettings.json に設定
- Status値のCHECK制約はアプリケーション層で検証（EF Coreの制約は使用しない）
- テーブル名"order"はPostgreSQLの予約語のため、[Table("order")]で明示
- すべてのカラム名は[Column]アトリビュートでsnake_case指定
- decimal型は[Column(TypeName = "decimal(precision,scale)")]で精度指定

**生成するコード:**

以下、各ファイルのコードを生成してください。各ファイルは明確に区切って出力してください。
```
