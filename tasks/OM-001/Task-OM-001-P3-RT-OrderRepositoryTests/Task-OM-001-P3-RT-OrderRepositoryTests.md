# コード生成タスク: Task OM-001-P3-RT - OrderRepository テストコード作成

## 1. 概要

- **ゴール:** OrderRepository（注文リポジトリ）のxUnit単体テストコード作成（TDDファースト）
- **対象ファイル:**
  - `Test/Unit/OrderManagement/OrderRepositoryTests.cs` - OrderRepositoryテストクラス
- **リポジトリ:** `order_be`

## 1.1. order_beリポジトリに持っていく必要なファイル

**このコード生成指示書:**
- `docs/tasks/OM-001/Task-OM-001-P3-RT-OrderRepositoryTests/Task-OM-001-P3-RT-OrderRepositoryTests.md` → order_beリポジトリの `docs/` フォルダにコピー

**参考ファイル（コンテキスト情報）:**
- `docs/tasks/OM-001/Task-OM-001-P3-RT-OrderRepositoryTests/database-schema.sql` → order_beリポジトリの `docs/` フォルダにコピー
  - Order ServiceのDDL定義（order、order_itemテーブル）を参照

**参考実装ファイル（コーディングパターン参照用）:**
- `docs/tasks/OM-001/Task-OM-001-P3-RT-OrderRepositoryTests/reference/CandidateRepository.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Repository層のコーディングパターン・例外ハンドリング参照
- `docs/tasks/OM-001/Task-OM-001-P3-RT-OrderRepositoryTests/reference/Candidate.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Entity層のコーディングパターン参照

**実施手順:**
1. 上記ファイルをorder_beリポジトリの適切な場所にコピー
2. `Task-OM-001-P3-RT-OrderRepositoryTests.md`の「最終コード生成プロンプト」を使用してテストコード生成
3. 生成されたテストコードをorder_beプロジェクトに配置
4. TDDサイクル: テスト実行（Red） → 実装（Green） → リファクタリング

## 2. 実装の指針

**STEP 1: テストクラス構造**
- xUnit使用（Fact/Theory属性）
- InMemoryDatabaseを使用した統合テスト
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`
- Arrange-Act-Assert パターン

**STEP 2: テスト対象メソッド（TDD目的で先に定義）**
- `CreateOrderAsync(Order order)` - 注文作成（IN_ORDER状態）
- `GetOrderByIdAsync(int orderId)` - 注文ID取得
- `GetOrdersByStatusAsync(string status)` - ステータス別注文取得
- `UpdateOrderAsync(Order order)` - 注文更新
- `DeleteOrderAsync(int orderId)` - 注文削除

**STEP 3: 例外処理テスト**
- NpgsqlException → そのまま再スロー
- TimeoutException → そのまま再スロー
- DbUpdateException → InvalidOperationExceptionに変換
- EntityNotFoundException → エンティティ未発見時

**STEP 4: データ整合性テスト**
- Order-OrderItem リレーション検証
- 注文ステータス（IN_ORDER、CONFIRMED、PAID）検証
- member_card_noのnullable検証
- decimal型精度検証（total、product_price、product_discount_percent）

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**Business Rule: 注文作成**
- 注文作成時はIN_ORDER状態で初期化
- Order（注文）は複数のOrderItem（注文明細）を持つ（1対多リレーション）
- member_card_noはnullable（ゲスト注文の場合null）
- totalはNOT NULL、NUMERIC(10,2)

**Business Rule: 注文状態管理**
- 注文状態（status）は 'IN_ORDER', 'CONFIRMED', 'PAID' の3種類
- 状態遷移: IN_ORDER → CONFIRMED → PAID
- IN_ORDER状態の注文のみ編集可能

**Business Rule: データ整合性**
- order_id、order_item_idは自動採番（SERIAL PRIMARY KEY）
- order_idは外部キー（OrderItem → Order、ON DELETE CASCADE想定）
- product_idは外部キー制約なし（Product Serviceへの参照）

**Acceptance Criteria: OrderRepository**
**Given:** OrderDbContextが構築済み、InMemoryDatabase使用
**When:** CreateOrderAsync実行
**Then:**
- IN_ORDER状態の注文が作成される
- OrderItemが関連付けて作成される
- order_idが自動採番される
- 例外処理が適切に実行される

**Given:** IN_ORDER状態の注文が存在
**When:** GetOrderByIdAsync実行
**Then:**
- 注文データが取得される
- OrderItemがIncludeされて取得される
- 存在しないorder_idの場合、EntityNotFoundExceptionがスローされる

**Given:** 複数注文が存在（IN_ORDER、CONFIRMED、PAID混在）
**When:** GetOrdersByStatusAsync("IN_ORDER")実行
**Then:**
- IN_ORDER状態の注文のみ取得される
- OrderItemがIncludeされて取得される

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

（このタスクはRepositoryテストコードのみ。API仕様は不要）

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
            // ...他の例外処理
        }

        public async Task<Candidate?> GetByIdAsync(int id)
        {
            try
            {
                var candidate = await _context.Candidates
                    .Include(c => c.Clouds)
                    .Include(c => c.Databases)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (candidate == null)
                    throw new EntityNotFoundException($"Candidate with ID {id} not found.");

                return candidate;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during candidate retrieval.");
                throw;
            }
            // ...他の例外処理
        }
    }
}
```

**適用パターン:**
1. **DbContext注入**: コンストラクタで`DbContext`と`ILogger`を受け取る
2. **非同期処理**: すべてのDB操作は`async/await`パターン
3. **例外ハンドリング**: `DbUpdateException`, `NpgsqlException`, `TimeoutException`などを適切に処理
4. **ログ出力**: すべての例外でログ出力（`_logger.LogError`, `_logger.LogWarning`）
5. **Include機能**: `Include()`でナビゲーションプロパティを読み込み

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
        public List<Database> Databases { get; set; }
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

**適用パターン:**
1. **[Table]アトリビュート**: テーブル名明示（snake_case）
2. **[Column]アトリビュート**: カラム名明示（snake_case）
3. **Navigation Property**: List<T>でリレーション表現

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0、Entity Framework Core、xUnitに精通したシニアソフトウェアエンジニアです。

**ゴール:**
Order Service用のOrderRepository テストコード（xUnit）を作成してください。

**生成対象ファイル:**
1. `Test/Unit/OrderManagement/OrderRepositoryTests.cs` - OrderRepositoryテストクラス

**要件:**
1. 上記の「実装の指針」に厳密に従ってください。
2. 「関連コンテキスト」で提供されたビジネスルール、Acceptance Criteria、DBスキーマをすべて満たすように実装してください。
3. 参考コード（CandidateRepository.cs、Candidate.cs）のコーディングスタイルを完全に踏襲してください。
4. TDD目的のため、テストコードが先に実装され、Repositoryの実装仕様を定義します。
5. xUnit、InMemoryDatabase、Moq（必要に応じて）を使用してください。

**OrderRepositoryTests.cs の実装要件:**
- namespace: OrderManagement.Tests.Unit
- xUnit使用（Fact/Theory属性）
- InMemoryDatabaseを使用した統合テスト
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`

**テスト対象メソッド（OrderRepositoryのインターフェース定義）:**
1. `public async Task<Order> CreateOrderAsync(Order order)`
   - IN_ORDER状態の注文作成
   - OrderItemも同時に作成
   - 戻り値: 作成された注文（order_id自動採番済み）

2. `public async Task<Order?> GetOrderByIdAsync(int orderId)`
   - order_idで注文取得
   - OrderItemをIncludeして取得
   - 存在しない場合、EntityNotFoundExceptionスロー

3. `public async Task<List<Order>> GetOrdersByStatusAsync(string status)`
   - 指定ステータスの注文一覧取得
   - OrderItemをIncludeして取得

4. `public async Task<Order> UpdateOrderAsync(Order order)`
   - 注文更新（statusやtotal更新）
   - OrderItemの追加・削除も可能

5. `public async Task DeleteOrderAsync(int orderId)`
   - 注文削除（OrderItemもCASCADE削除）

**テストケース一覧:**
1. **CreateOrderAsync_ValidOrder_ReturnsCreatedOrder**
   - Given: 有効な注文データ（IN_ORDER、2つのOrderItem含む）
   - When: CreateOrderAsync実行
   - Then: 注文作成成功、order_id自動採番、OrderItem関連付け

2. **CreateOrderAsync_GuestOrder_ReturnsCreatedOrderWithNullMemberCardNo**
   - Given: member_card_noがnullの注文データ（ゲスト注文）
   - When: CreateOrderAsync実行
   - Then: 注文作成成功、member_card_noがnull

3. **CreateOrderAsync_DbUpdateException_ThrowsInvalidOperationException**
   - Given: 制約違反を引き起こす注文データ
   - When: CreateOrderAsync実行
   - Then: InvalidOperationExceptionがスローされる

4. **GetOrderByIdAsync_ExistingOrder_ReturnsOrderWithItems**
   - Given: order_id=1の注文が存在
   - When: GetOrderByIdAsync(1)実行
   - Then: 注文データ取得、OrderItemがIncludeされている

5. **GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException**
   - Given: order_id=999の注文が存在しない
   - When: GetOrderByIdAsync(999)実行
   - Then: EntityNotFoundExceptionがスローされる

6. **GetOrdersByStatusAsync_InOrderStatus_ReturnsInOrderOrdersOnly**
   - Given: IN_ORDER、CONFIRMED、PAID状態の注文が混在
   - When: GetOrdersByStatusAsync("IN_ORDER")実行
   - Then: IN_ORDER状態の注文のみ取得

7. **UpdateOrderAsync_ValidUpdate_ReturnsUpdatedOrder**
   - Given: 既存注文のtotalを更新
   - When: UpdateOrderAsync実行
   - Then: 注文更新成功

8. **DeleteOrderAsync_ExistingOrder_DeletesOrderAndItems**
   - Given: order_id=1の注文が存在
   - When: DeleteOrderAsync(1)実行
   - Then: 注文とOrderItemが削除される

**InMemoryDatabase セットアップ:**
- OrderDbContext使用
- テストメソッドごとにDbContextを初期化
- テストデータはArrangeセクションで投入

**例外処理テスト:**
- NpgsqlException、TimeoutException、DbUpdateExceptionの各例外をMockして検証

**特記事項:**
- テーブル名"order"はPostgreSQLの予約語のため、Entity定義では[Table("order")]で明示
- decimal型は適切な精度（total: decimal(10,2)、product_discount_percent: decimal(5,2)）
- created_atはDateTime?型（nullable）
- OrderItemのproduct_idは外部キー制約なし（Product Serviceへの参照）

**生成するコード:**

以下、テストコードを生成してください。
```
