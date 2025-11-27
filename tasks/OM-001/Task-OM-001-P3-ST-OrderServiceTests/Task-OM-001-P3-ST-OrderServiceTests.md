# コード生成タスク: Task OM-001-P3-ST - OrderService テストコード作成

## 1. 概要

- **ゴール:** OrderService（注文サービス）のxUnit単体テストコード作成（TDDファースト）
- **対象ファイル:**
  - `Test/Unit/OrderManagement/OrderServiceTests.cs` - OrderServiceテストクラス
- **リポジトリ:** `order_be`

## 1.1. order_beリポジトリに持っていく必要なファイル

**このコード生成指示書:**
- `docs/tasks/OM-001/Task-OM-001-P3-ST-OrderServiceTests/Task-OM-001-P3-ST-OrderServiceTests.md` → order_beリポジトリの `docs/` フォルダにコピー

**必須コンテキスト情報:**
- `docs/tasks/OM-001/Task-OM-001-P3-ST-OrderServiceTests/pbi-tasks-order-management-v3.md` → order_beリポジトリの `docs/` フォルダにコピー
  - PBI OM-001の完全なBusiness Rules、Acceptance Criteriaを含む
- `docs/tasks/OM-001/Task-OM-001-P3-ST-OrderServiceTests/database-schema.sql` → order_beリポジトリの `docs/` フォルダにコピー
  - Order ServiceのDDL定義（order、order_itemテーブル）を参照

**参考実装ファイル（コーディングパターン参照用）:**
- `docs/tasks/OM-001/Task-OM-001-P3-ST-OrderServiceTests/reference/CandidateService.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Service層のコーディングパターン・例外ハンドリング参照
- `docs/tasks/OM-001/Task-OM-001-P3-ST-OrderServiceTests/reference/CandidateRepository.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Repository層のインターフェース参照

**実施手順:**
1. 上記ファイルをorder_beリポジトリの適切な場所にコピー
2. `Task-OM-001-P3-ST-OrderServiceTests.md`の「最終コード生成プロンプト」を使用してテストコード生成
3. 生成されたテストコードをorder_beプロジェクトに配置
4. TDDサイクル: テスト実行（Red） → 実装（Green） → リファクタリング

## 2. 実装の指針

**STEP 1: テストクラス構造**
- xUnit使用（Fact/Theory属性）
- Moqを使用したRepositoryのMock化
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`
- Arrange-Act-Assert パターン

**STEP 2: テスト対象メソッド（TDD目的で先に定義）**
- `CreateOrderAsync(Order order)` - 注文作成（IN_ORDER状態）
- `GetOrderByIdAsync(int orderId)` - 注文ID取得
- `GetOrdersByStatusAsync(string status)` - ステータス別注文取得
- `UpdateOrderStatusAsync(int orderId, string newStatus)` - 注文ステータス更新
- `CalculateTotalAsync(List<OrderItem> items)` - 合計金額計算

**STEP 3: ビジネスロジックテスト**
- 注文作成時のIN_ORDER状態初期化
- 注文合計金額の計算ロジック（数量×単価×(1-割引率)）
- 注文ステータス遷移検証（IN_ORDER → CONFIRMED → PAID）
- Repository協調処理（Mock検証）

**STEP 4: 例外処理テスト**
- InvalidOperationException → そのまま再スロー（Repository層から）
- NpgsqlException, TimeoutException → そのまま再スロー（Repository層から）
- EntityNotFoundException → そのまま再スロー（Repository層から）

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**Business Rule: 注文作成**
- 注文作成時はIN_ORDER状態で初期化
- Order（注文）は複数のOrderItem（注文明細）を持つ（1対多リレーション）
- member_card_noはnullable（ゲスト注文の場合null）
- totalは注文明細の合計金額（自動計算）

**Business Rule: 注文状態管理**
- 注文状態（status）は 'IN_ORDER', 'CONFIRMED', 'PAID' の3種類
- 状態遷移: IN_ORDER → CONFIRMED → PAID
- IN_ORDER状態の注文のみ編集可能

**Business Rule: 合計金額計算**
- total = Σ(quantity × product_price × (1 - product_discount_percent / 100))
- decimal型の精度: NUMERIC(10,2)

**Acceptance Criteria: OrderService**
**Given:** OrderRepositoryがMock化済み
**When:** CreateOrderAsync実行
**Then:**
- IN_ORDER状態で注文が作成される
- Repository.CreateOrderAsyncが呼ばれる
- 合計金額が正しく計算される
- 例外処理が適切に実行される

**Given:** IN_ORDER状態の注文が存在
**When:** GetOrderByIdAsync実行
**Then:**
- Repository.GetOrderByIdAsyncが呼ばれる
- 注文データが返される
- 存在しない注文の場合、EntityNotFoundExceptionがスローされる

**Given:** 複数注文が存在（IN_ORDER、CONFIRMED、PAID混在）
**When:** GetOrdersByStatusAsync("IN_ORDER")実行
**Then:**
- Repository.GetOrdersByStatusAsyncが呼ばれる
- IN_ORDER状態の注文のみ取得される

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
```

### 3.3. 関連API仕様

（このタスクはServiceテストコードのみ。API仕様は不要）

### 3.4. 関連アーキテクチャ & 既存コード

#### 参考Service実装 (CandidateService.cs)

```csharp
using Candidate_BE.Models;
using Candidate_BE.Repository;
using Candidate_BE.Exceptions;
using Npgsql;
using System.Net.Sockets;

namespace Candidate_BE.Service
{
    public class CandidateService
    {
        private readonly CandidateRepository _repository;

        public CandidateService(CandidateRepository repository)
        {
            _repository = repository;
        }

        public async Task<Candidate> AddCandidateAsync(Candidate entity)
        {
            try
            {
                var inserted = await _repository.InsertAsync(entity);
                return inserted;
            }
            catch (InvalidOperationException)
            {
                // Repository層で変換されたビジネスロジック例外をそのまま再スロー
                throw;
            }
            catch (NpgsqlException)
            {
                // データベース接続エラーをそのまま再スロー
                throw;
            }
            catch (SocketException)
            {
                // ネットワーク接続エラーをそのまま再スロー
                throw;
            }
            catch (TimeoutException)
            {
                // タイムアウトエラーをそのまま再スロー
                throw;
            }
            catch (Exception ex)
            {
                // 予期しない例外は新しい例外でラップ
                throw new Exception($"Service error: {ex.Message}", ex);
            }
        }
    }
}
```

**適用パターン:**
1. **Repository注入**: コンストラクタで`Repository`を受け取る
2. **非同期処理**: すべての操作は`async/await`パターン
3. **例外ハンドリング**: Repository層の例外をそのまま再スロー
4. **ビジネスロジック**: 合計金額計算、状態管理などの処理
5. **薄いService層**: ビジネスロジックを最小限に、Repository協調

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0、xUnit、Moqに精通したシニアソフトウェアエンジニアです。

**ゴール:**
Order Service用のOrderService テストコード（xUnit + Moq）を作成してください。

**生成対象ファイル:**
1. `Test/Unit/OrderManagement/OrderServiceTests.cs` - OrderServiceテストクラス

**要件:**
1. 上記の「実装の指針」に厳密に従ってください。
2. 「関連コンテキスト」で提供されたビジネスルール、Acceptance Criteria、DBスキーマをすべて満たすように実装してください。
3. 参考コード（CandidateService.cs）のコーディングスタイルを完全に踏襲してください。
4. TDD目的のため、テストコードが先に実装され、Serviceの実装仕様を定義します。
5. xUnit、Moq（Repositoryのコピー化）を使用してください。

**OrderServiceTests.cs の実装要件:**
- namespace: OrderManagement.Tests.Unit
- xUnit使用（Fact/Theory属性）
- Moqを使用したOrderRepositoryのMock化
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`

**テスト対象メソッド（OrderServiceのインターフェース定義）:**
1. `public async Task<Order> CreateOrderAsync(Order order)`
   - IN_ORDER状態の注文作成
   - 合計金額自動計算
   - Repository.CreateOrderAsyncを呼び出し

2. `public async Task<Order> GetOrderByIdAsync(int orderId)`
   - order_idで注文取得
   - Repository.GetOrderByIdAsyncを呼び出し
   - 存在しない場合、EntityNotFoundExceptionスロー

3. `public async Task<List<Order>> GetOrdersByStatusAsync(string status)`
   - 指定ステータスの注文一覧取得
   - Repository.GetOrdersByStatusAsyncを呼び出し

4. `public async Task<Order> UpdateOrderStatusAsync(int orderId, string newStatus)`
   - 注文ステータス更新
   - 状態遷移検証（IN_ORDER → CONFIRMED → PAID）
   - Repository.UpdateOrderAsyncを呼び出し

5. `public async Task<decimal> CalculateTotalAsync(List<OrderItem> items)`
   - 合計金額計算（ビジネスロジック）
   - total = Σ(quantity × product_price × (1 - product_discount_percent / 100))

**テストケース一覧:**
1. **CreateOrderAsync_ValidOrder_ReturnsCreatedOrder**
   - Given: 有効な注文データ（2つのOrderItem）
   - When: CreateOrderAsync実行
   - Then: IN_ORDER状態で注文作成、Repository.CreateOrderAsyncが1回呼ばれる

2. **CreateOrderAsync_CalculatesTotalCorrectly**
   - Given: 注文明細（quantity=2, price=100, discount=10%）
   - When: CreateOrderAsync実行
   - Then: total = 2 × 100 × 0.9 = 180.00

3. **GetOrderByIdAsync_ExistingOrder_ReturnsOrder**
   - Given: order_id=1の注文が存在（Repositoryから返却）
   - When: GetOrderByIdAsync(1)実行
   - Then: 注文データ返却、Repository.GetOrderByIdAsyncが1回呼ばれる

4. **GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException**
   - Given: RepositoryがEntityNotFoundExceptionをスロー
   - When: GetOrderByIdAsync(999)実行
   - Then: EntityNotFoundExceptionが再スローされる

5. **GetOrdersByStatusAsync_InOrderStatus_ReturnsInOrderOrders**
   - Given: Repository がIN_ORDER状態の注文リストを返却
   - When: GetOrdersByStatusAsync("IN_ORDER")実行
   - Then: 注文リスト返却、Repository.GetOrdersByStatusAsyncが1回呼ばれる

6. **UpdateOrderStatusAsync_ValidTransition_ReturnsUpdatedOrder**
   - Given: IN_ORDER状態の注文が存在
   - When: UpdateOrderStatusAsync(1, "CONFIRMED")実行
   - Then: CONFIRMED状態に更新、Repository.UpdateOrderAsyncが1回呼ばれる

7. **CalculateTotalAsync_MultipleItems_ReturnsCorrectTotal**
   - Given: 複数OrderItem（異なる価格・割引率）
   - When: CalculateTotalAsync実行
   - Then: 正しい合計金額が返却される

8. **CreateOrderAsync_RepositoryThrowsInvalidOperationException_RethrowsException**
   - Given: RepositoryがInvalidOperationExceptionをスロー
   - When: CreateOrderAsync実行
   - Then: InvalidOperationExceptionが再スローされる

**Mock設定:**
- OrderRepositoryをMock化
- 各テストで必要なRepository.メソッドの戻り値を設定
- Verify()で呼び出し回数を検証

**特記事項:**
- テストコードのみを作成（OrderService実装は次のタスク）
- テストは「Red」状態で完了（OrderServiceが未実装のため）
- OrderService実装時に満たすべきインターフェース・ビジネスロジックを定義
- decimal型の精度計算に注意（四捨五入処理）

**生成するコード:**

以下、テストコードを生成してください。
```
