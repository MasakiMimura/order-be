# コード生成タスク: Task OM-001-P3-R - OrderRepository実装

## 1. 概要

- **ゴール:** OrderRepository（注文リポジトリ）の実装（TDD準拠）
- **対象ファイル:**
  - `OrderManagement/Repository/OrderRepository.cs` - OrderRepositoryクラス
- **リポジトリ:** `order_be`

## 1.1. order_beリポジトリに持っていく必要なファイル

**このコード生成指示書:**
- `docs/tasks/OM-001/Task-OM-001-P3-R-OrderRepository/Task-OM-001-P3-R-OrderRepository.md` → order_beリポジトリの `docs/` フォルダにコピー

**参考ファイル（コンテキスト情報）:**
- `docs/tasks/OM-001/Task-OM-001-P3-R-OrderRepository/database-schema.sql` → order_beリポジトリの `docs/` フォルダにコピー
  - Order ServiceのDDL定義（order、order_itemテーブル）を参照

**参考実装ファイル（コーディングパターン参照用）:**
- `docs/tasks/OM-001/Task-OM-001-P3-R-OrderRepository/reference/CandidateRepository.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Repository層のコーディングパターン・例外ハンドリング参照

**実施手順:**
1. 上記ファイルをorder_beリポジトリの適切な場所にコピー
2. `Task-OM-001-P3-R-OrderRepository.md`の「最終コード生成プロンプト」を使用してコード生成
3. 生成されたコードをorder_beプロジェクトに配置
4. OrderRepositoryTests（既存テスト）を実行してTDD検証（Green確認）

## 2. 実装の指針

**STEP 1: クラス構造**
- namespace: OrderManagement.Repository
- DbContext（OrderDbContext）とILoggerを注入
- 非同期処理パターン（async/await）

**STEP 2: CRUD操作実装**
- `CreateOrderAsync(Order order)` - 注文作成（IN_ORDER状態）
- `GetOrderByIdAsync(int orderId)` - 注文ID取得（OrderItemをInclude）
- `GetOrdersByStatusAsync(string status)` - ステータス別注文取得
- `UpdateOrderAsync(Order order)` - 注文更新
- `DeleteOrderAsync(int orderId)` - 注文削除

**STEP 3: 例外ハンドリング**
- DbUpdateException → InvalidOperationExceptionに変換
- NpgsqlException、TimeoutException → そのまま再スロー
- EntityNotFoundException → エンティティ未発見時
- すべての例外でログ出力

**STEP 4: TDD準拠**
- OrderRepositoryTests（既存テスト）で定義されたインターフェース・例外処理・CRUD操作に基づいて実装
- テスト実行してGreen（成功）になることを確認

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
**Given:** OrderDbContextが構築済み、Order/OrderItemエンティティが実装済み
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

（このタスクはRepository実装のみ。API仕様は不要）

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
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during insert (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during insert.");
                throw new Exception($"Repository error during insert: {ex.Message}", ex);
            }
        }

        public async Task<Candidate?> GetByIdAsync(int id)
        {
            try
            {
                var candidate = await _context.Candidates
                    .Include(c => c.Clouds)
                    .Include(c => c.Databases)
                    .Include(c => c.FrameworksBackend)
                    .Include(c => c.FrameworksFrontend)
                    .Include(c => c.OS)
                    .Include(c => c.ProgrammingLanguages)
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
            catch (SocketException ex)
            {
                _logger.LogError(ex, "Network connection error during candidate retrieval.");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Database connection timeout during candidate retrieval.");
                throw;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("transient failure") || ex.Message.Contains("connection"))
            {
                _logger.LogError(ex, "Database connection issue during candidate retrieval (transient failure).");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during candidate retrieval.");
                throw new Exception($"Repository error during candidate retrieval: {ex.Message}", ex);
            }
        }

        public async Task<IEnumerable<Candidate>> GetAllCandidatesAsync()
        {
            try
            {
                var candidates = await _context.Candidates
                    .Include(c => c.Clouds)
                    .Include(c => c.Databases)
                    .ToListAsync();

                if (!candidates.Any())
                    throw new EntityNotFoundException("No candidates found in the database.");

                return candidates;
            }
            catch (EntityNotFoundException)
            {
                throw;
            }
            catch (NpgsqlException ex)
            {
                _logger.LogError(ex, "PostgreSQL connection error during candidates retrieval.");
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
6. **EntityNotFoundException**: エンティティ未発見時にカスタム例外をスロー

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0、Entity Framework Coreに精通したシニアソフトウェアエンジニアです。

**ゴール:**
Order Service用のOrderRepositoryクラスを作成してください。

**生成対象ファイル:**
1. `OrderManagement/Repository/OrderRepository.cs` - OrderRepositoryクラス

**要件:**
1. 上記の「実装の指針」に厳密に従ってください。
2. 「関連コンテキスト」で提供されたビジネスルール、Acceptance Criteria、DBスキーマをすべて満たすように実装してください。
3. 参考コード（CandidateRepository.cs）のコーディングスタイルを完全に踏襲してください。
4. OrderRepositoryTests（既存テスト）で定義されたインターフェース・例外処理・CRUD操作に基づいて実装してください。
5. 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**OrderRepository.cs の実装要件:**
- namespace: OrderManagement.Repository
- コンストラクタ: `OrderDbContext`と`ILogger<OrderRepository>`を受け取る
- メソッド:
  1. `public async Task<Order> CreateOrderAsync(Order order)`
     - IN_ORDER状態の注文作成
     - OrderItemも同時に作成（_context.Orders.Add(order)で自動的に追加される）
     - 戻り値: 作成された注文（order_id自動採番済み）
     - 例外処理: DbUpdateException → InvalidOperationException変換、NpgsqlException/TimeoutException → 再スロー

  2. `public async Task<Order?> GetOrderByIdAsync(int orderId)`
     - order_idで注文取得
     - OrderItemをInclude（.Include(o => o.Items)）
     - 存在しない場合、EntityNotFoundExceptionスロー
     - 例外処理: EntityNotFoundException → 再スロー、NpgsqlException/TimeoutException → 再スロー

  3. `public async Task<List<Order>> GetOrdersByStatusAsync(string status)`
     - 指定ステータスの注文一覧取得
     - OrderItemをInclude（.Include(o => o.Items)）
     - 戻り値: 注文リスト（空リストの場合もOK、例外スローしない）
     - 例外処理: NpgsqlException/TimeoutException → 再スロー

  4. `public async Task<Order> UpdateOrderAsync(Order order)`
     - 注文更新（statusやtotal更新）
     - 既存注文をGetOrderByIdAsyncで取得し、_context.Entry(existing).CurrentValues.SetValues(order)で更新
     - OrderItemの追加・削除も可能（UpdateChildCollectionメソッドパターン使用）
     - 戻り値: 更新された注文
     - 例外処理: DbUpdateException → InvalidOperationException変換、EntityNotFoundException → 再スロー

  5. `public async Task DeleteOrderAsync(int orderId)`
     - 注文削除（OrderItemもCASCADE削除）
     - 既存注文をGetOrderByIdAsyncで取得し、_context.Orders.Remove(existing)で削除
     - 戻り値: なし
     - 例外処理: EntityNotFoundException → 再スロー、DbUpdateException → InvalidOperationException変換

**例外ハンドリング:**
- DbUpdateException → InvalidOperationException変換（"Operation failed due to database constraint"メッセージ）
- NpgsqlException → そのまま再スロー（Controller層で503処理）
- SocketException → そのまま再スロー
- TimeoutException → そのまま再スロー（"Database connection timeout"ログ）
- InvalidOperationException（transient failure） → そのまま再スロー
- EntityNotFoundException → そのまま再スロー
- Exception → 新しい例外でラップ（"Repository error during [operation]: {ex.Message}"）

**ログ出力:**
- すべての例外で_logger.LogError または _logger.LogWarning
- DbUpdateException → LogWarning
- その他の例外 → LogError

**特記事項:**
- OrderDbContextは既に実装済みと仮定
- Order、OrderItemエンティティは既に実装済みと仮定
- EntityNotFoundExceptionカスタム例外は既に実装済みと仮定
- Include機能でOrderItemを読み込む（.Include(o => o.Items)）
- created_atはDateTime?型、データベース側でDEFAULT NOW()設定
- UpdateChildCollectionメソッドは参考実装（CandidateRepository.cs）のパターンを踏襲

**生成するコード:**

以下、OrderRepository.csのコードを生成してください。
```
