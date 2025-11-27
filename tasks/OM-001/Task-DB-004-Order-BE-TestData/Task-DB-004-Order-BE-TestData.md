# コード生成タスク: Task DB-004 Order BE - テストデータ・クリーンアップ機能

## 1. 概要

- **ゴール:** Order Service用の単体テスト・統合テスト用初期データ投入・クリーンアップ処理実装
- **対象ファイル:**
  - `OrderManagement/TestData/OrderTestDataSeeder.cs` - テストデータ投入クラス
  - `OrderManagement/TestData/OrderTestDataCleaner.cs` - クリーンアップクラス
- **リポジトリ:** `order_be`

## 1.1. order_beリポジトリに持っていく必要なファイル

**このコード生成指示書:**
- `docs/tasks/OM-001/Task-DB-004-Order-BE-TestData/Task-DB-004-Order-BE-TestData.md` → order_beリポジトリの `docs/` フォルダにコピー

**参考ファイル（コンテキスト情報）:**
- `docs/tasks/OM-001/Task-DB-004-Order-BE-TestData/database-schema.sql` → order_beリポジトリの `docs/` フォルダにコピー
  - Order ServiceのDDL定義（order、order_itemテーブル）を参照
  - テストデータの構造理解用

**参考実装ファイル（コーディングパターン参照用）:**
- `docs/tasks/OM-001/Task-DB-004-Order-BE-TestData/reference/CandidateRepository.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Repository層のコーディングパターン参照

**実施手順:**
1. 上記ファイルをorder_beリポジトリの適切な場所にコピー
2. `Task-DB-004-Order-BE-TestData.md`の「最終コード生成プロンプト」を使用してコード生成
3. 生成されたコードをorder_beプロジェクトに配置
4. 単体テストから利用可能にする

## 2. 実装の指針

**STEP 1: OrderTestDataSeeder実装**
- IN_ORDER状態の注文データ作成メソッド
- 複数OrderItemを含む注文データ生成
- member_card_noあり/なし両パターン対応
- 非同期処理パターン（async/await）

**STEP 2: OrderTestDataCleaner実装**
- order_item、orderテーブルのデータ削除
- トランザクション処理
- 外部キー制約を考慮した削除順序（order_item → order）
- 非同期処理パターン（async/await）

**STEP 3: 統合テスト向けメソッド**
- SeedTestOrdersAsync(): テスト用注文データ一括作成
- CleanupTestDataAsync(): テストデータ一括削除
- エラーハンドリング

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**Business Rule: テストデータ**
- IN_ORDER状態の注文データを作成可能
- ゲスト注文（member_card_no = null）とメンバー注文の両方に対応
- 注文明細（OrderItem）を複数含む注文を作成可能
- テストデータのクリーンアップは外部キー制約を考慮

**Business Rule: データ整合性**
- order_id、order_item_idは自動採番（SERIAL PRIMARY KEY）
- totalはNOT NULL、NUMERIC(10,2)
- statusはCHECK制約で制限（'IN_ORDER', 'CONFIRMED', 'PAID'）
- order_idは外部キー（ON DELETE CASCADE想定）

**Acceptance Criteria:**
**Given:** order_beプロジェクトが作成済み、OrderDbContextが構築済み
**When:** テストデータSeeder/Cleanerを実行
**Then:**
- IN_ORDER状態の注文データが作成される
- 注文明細データが関連付けて作成される
- クリーンアップ実行で全テストデータが削除される
- トランザクション処理により整合性が保たれる

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

（このタスクではAPI実装は不要。テストデータ投入・クリーンアップのみ）

### 3.4. 関連アーキテクチャ & 既存コード

#### 参考Repository実装 (CandidateRepository.cs)

```csharp
using Candidate_BE.Models;
using Candidate_BE.Data;
using Candidate_BE.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net.Sockets;

namespace Candidate_BE.Repository
{
    public class CandidateRepository
    {
        private readonly SkillDbContext _context;
        private readonly ILogger<CandidateRepository> _logger;

        public CandidateRepository(SkillDbContext context, ILogger<CandidateRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Candidate> InsertAsync(Candidate candidate)
        {
            _context.Candidates.Add(candidate);
            try
            {
                await _context.SaveChangesAsync();
                return candidate;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Insert failed due to update issue.");
                throw new InvalidOperationException("Insert failed due to database constraint", ex);
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during insert.");
                throw;
            }
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during insert.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during insert.");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during insert.");
                throw new Exception($"Repository error during insert: {ex.Message}", ex);
            }
        }
    }
}
```

**適用パターン:**
1. **DbContext注入**: コンストラクタで`DbContext`と`ILogger`を受け取る
2. **非同期処理**: すべてのDB操作は`async/await`パターン
3. **例外ハンドリング**: `DbUpdateException`, `NpgsqlException`, `TimeoutException`などを適切に処理
4. **ログ出力**: すべての例外でログ出力（`_logger.LogError`, `_logger.LogWarning`）
5. **トランザクション**: `SaveChangesAsync()`でコミット

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0、Entity Framework Coreに精通したシニアソフトウェアエンジニアです。

**ゴール:**
Order Service用のテストデータSeeder・Cleanerクラスを作成してください。

**生成対象ファイル:**
1. `OrderManagement/TestData/OrderTestDataSeeder.cs` - テストデータ投入クラス
2. `OrderManagement/TestData/OrderTestDataCleaner.cs` - クリーンアップクラス

**要件:**
1. 上記の「実装の指針」に厳密に従ってください。
2. 「関連コンテキスト」で提供されたDBスキーマ、参考コードをすべて満たすように実装してください。
3. 参考コード（CandidateRepository.cs）のコーディングスタイルを完全に踏襲してください。
4. 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。
5. Entity Framework Core 8.0のベストプラクティスに従ってください。

**OrderTestDataSeeder.cs の実装要件:**
- namespace: OrderManagement.TestData
- コンストラクタ: `OrderDbContext`と`ILogger<OrderTestDataSeeder>`を受け取る
- メソッド:
  - `public async Task<Order> SeedSingleOrderAsync(string? memberCardNo = null, decimal total = 1000.00m, string status = "IN_ORDER")`
    - 単一注文データ作成（OrderItem 2件含む）
    - created_atは現在時刻
    - product_id, product_name, product_price, product_discount_percent, quantityは固定値でOK
  - `public async Task<List<Order>> SeedTestOrdersAsync(int count = 3)`
    - 複数注文データ一括作成
    - ゲスト注文とメンバー注文を混在
- エラーハンドリング:
  - DbUpdateException → InvalidOperationException変換
  - NpgsqlException, TimeoutException → そのまま再スロー
  - すべての例外をログ出力

**OrderTestDataCleaner.cs の実装要件:**
- namespace: OrderManagement.TestData
- コンストラクタ: `OrderDbContext`と`ILogger<OrderTestDataCleaner>`を受け取る
- メソッド:
  - `public async Task CleanupTestDataAsync()`
    - order_item、orderテーブルの全データ削除
    - 外部キー制約を考慮した削除順序（order_item → order）
    - トランザクション処理
  - `public async Task CleanupOrdersByStatusAsync(string status)`
    - 特定ステータスの注文のみ削除
- エラーハンドリング:
  - DbUpdateException → InvalidOperationException変換
  - NpgsqlException, TimeoutException → そのまま再スロー
  - すべての例外をログ出力

**特記事項:**
- テーブル名"order"はPostgreSQLの予約語のため、Entity定義では[Table("order")]で明示
- decimal型は適切な精度（total: decimal(10,2)、product_discount_percent: decimal(5,2)）
- テストデータのproduct_id、product_name等は固定値でOK（例: product_id=1, product_name="カフェラテ", product_price=450.00m, quantity=2）
- Seeder/Cleanerはテスト専用のため、本番環境では使用しない想定

**生成するコード:**

以下、各ファイルのコードを生成してください。各ファイルは明確に区切って出力してください。
```
