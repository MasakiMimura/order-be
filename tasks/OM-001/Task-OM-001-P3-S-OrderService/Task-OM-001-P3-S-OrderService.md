# コード生成タスク: Task OM-001-P3-S - OrderService実装

## 1. 概要

- **ゴール:** OrderService（注文サービス）の実装（TDD準拠）
- **対象ファイル:**
  - `OrderManagement/Service/OrderService.cs` - OrderServiceクラス
- **リポジトリ:** `order_be`

## 1.1. order_beリポジトリに持っていく必要なファイル

**このコード生成指示書:**
- `docs/tasks/OM-001/Task-OM-001-P3-S-OrderService/Task-OM-001-P3-S-OrderService.md` → order_beリポジトリの `docs/` フォルダにコピー

**必須コンテキスト情報:**
- `docs/tasks/OM-001/Task-OM-001-P3-S-OrderService/pbi-tasks-order-management-v3.md` → order_beリポジトリの `docs/` フォルダにコピー
  - PBI OM-001の完全なBusiness Rules、Acceptance Criteriaを含む
- `docs/tasks/OM-001/Task-OM-001-P3-S-OrderService/database-schema.sql` → order_beリポジトリの `docs/` フォルダにコピー
  - Order ServiceのDDL定義（order、order_itemテーブル）を参照

**参考実装ファイル（コーディングパターン参照用）:**
- `docs/tasks/OM-001/Task-OM-001-P3-S-OrderService/reference/CandidateService.cs` → order_beリポジトリの `docs/reference/` フォルダにコピー
  - Service層のコーディングパターン・例外ハンドリング参照

**実施手順:**
1. 上記ファイルをorder_beリポジトリの適切な場所にコピー
2. `Task-OM-001-P3-S-OrderService.md`の「最終コード生成プロンプト」を使用してコード生成
3. 生成されたコードをorder_beプロジェクトに配置
4. OrderServiceTests（既存テスト）を実行してTDD検証（Green確認）

## 2. 実装の指針

**STEP 1: クラス構造**
- namespace: OrderManagement.Service
- OrderRepositoryを注入
- 非同期処理パターン（async/await）

**STEP 2: ビジネスロジック実装**
- `CreateOrderAsync(Order order)` - 注文作成（IN_ORDER状態、合計金額計算）
- `GetOrderByIdAsync(int orderId)` - 注文ID取得
- `GetOrdersByStatusAsync(string status)` - ステータス別注文取得
- `UpdateOrderStatusAsync(int orderId, string newStatus)` - 注文ステータス更新
- `CalculateTotalAsync(List<OrderItem> items)` - 合計金額計算

**STEP 3: Repository協調処理**
- Repository.CreateOrderAsyncを呼び出し
- Repository.GetOrderByIdAsyncを呼び出し
- Repository.GetOrdersByStatusAsyncを呼び出し
- Repository.UpdateOrderAsyncを呼び出し

**STEP 4: 例外ハンドリング**
- InvalidOperationException → そのまま再スロー（Repository層から）
- NpgsqlException、TimeoutException → そのまま再スロー（Repository層から）
- EntityNotFoundException → そのまま再スロー（Repository層から）
- 予期しない例外 → 新しい例外でラップ

**STEP 5: TDD準拠**
- OrderServiceTests（既存テスト）で定義されたインターフェース・ビジネスロジックに基づいて実装
- テスト実行してGreen（成功）になることを確認

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
- 四捨五入処理: Math.Round(value, 2)

**Acceptance Criteria: OrderService**
**Given:** OrderRepositoryが実装済み、OrderServiceTests（既存テスト）が存在
**When:** CreateOrderAsync実行
**Then:**
- IN_ORDER状態で注文が作成される
- 合計金額が正しく計算される
- Repository.CreateOrderAsyncが呼ばれる
- 例外処理が適切に実行される
- すべてのテストがGreen（成功）になる

**Given:** IN_ORDER状態の注文が存在
**When:** GetOrderByIdAsync実行
**Then:**
- Repository.GetOrderByIdAsyncが呼ばれる
- 注文データが返される
- 存在しない注文の場合、EntityNotFoundExceptionがスローされる

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

（このタスクはService実装のみ。API仕様は不要）

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
5. **薄いService層**: 複雑なビジネスロジックがない場合、Repositoryを薄くラップ

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
Order Service用のOrderServiceクラスを作成してください。

**生成対象ファイル:**
1. `OrderManagement/Service/OrderService.cs` - OrderServiceクラス

**要件:**
1. 上記の「実装の指針」に厳密に従ってください。
2. 「関連コンテキスト」で提供されたビジネスルール、Acceptance Criteria、DBスキーマをすべて満たすように実装してください。
3. 参考コード（CandidateService.cs）のコーディングスタイルを完全に踏襲してください。
4. OrderServiceTests（既存テスト）で定義されたインターフェース・ビジネスロジックに基づいて実装してください。
5. 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**OrderService.cs の実装要件:**
- namespace: OrderManagement.Service
- コンストラクタ: `OrderRepository`を受け取る
- メソッド:
  1. `public async Task<Order> CreateOrderAsync(Order order)`
     - IN_ORDER状態で注文作成
     - 合計金額自動計算（CalculateTotalAsyncを呼び出し）
     - order.Status = "IN_ORDER" 設定
     - order.Total = await CalculateTotalAsync(order.Items) 設定
     - Repository.CreateOrderAsyncを呼び出し
     - 戻り値: 作成された注文
     - 例外処理: InvalidOperationException, NpgsqlException, TimeoutException → 再スロー

  2. `public async Task<Order> GetOrderByIdAsync(int orderId)`
     - Repository.GetOrderByIdAsyncを呼び出し
     - 戻り値: 注文データ
     - 例外処理: EntityNotFoundException → 再スロー

  3. `public async Task<List<Order>> GetOrdersByStatusAsync(string status)`
     - Repository.GetOrdersByStatusAsyncを呼び出し
     - 戻り値: 注文リスト
     - 例外処理: NpgsqlException, TimeoutException → 再スロー

  4. `public async Task<Order> UpdateOrderStatusAsync(int orderId, string newStatus)`
     - Repository.GetOrderByIdAsyncで既存注文取得
     - order.Status = newStatus 更新
     - Repository.UpdateOrderAsyncを呼び出し
     - 戻り値: 更新された注文
     - 例外処理: EntityNotFoundException → 再スロー

  5. `public async Task<decimal> CalculateTotalAsync(List<OrderItem> items)`
     - 合計金額計算（ビジネスロジック）
     - total = Σ(quantity × product_price × (1 - product_discount_percent / 100))
     - Math.Round(total, 2) で四捨五入
     - 戻り値: 合計金額（decimal）
     - 例外処理: ArgumentNullExceptionをスロー（items == null の場合）

**例外ハンドリング:**
- InvalidOperationException → そのまま再スロー（Repository層から）
- NpgsqlException → そのまま再スロー（Controller層で503処理）
- SocketException → そのまま再スロー
- TimeoutException → そのまま再スロー
- EntityNotFoundException → そのまま再スロー（Repository層から）
- Exception → 新しい例外でラップ（"Service error: {ex.Message}"）

**特記事項:**
- OrderRepository、Order、OrderItemエンティティは既に実装済みと仮定
- EntityNotFoundExceptionカスタム例外は既に実装済みと仮定
- decimal型の精度計算に注意（Math.Round(value, 2)）
- CalculateTotalAsyncはビジネスロジックメソッド（Repository不要）
- status値の検証はController層で実施（Service層では検証不要）

**生成するコード:**

以下、OrderService.csのコードを生成してください。
```
