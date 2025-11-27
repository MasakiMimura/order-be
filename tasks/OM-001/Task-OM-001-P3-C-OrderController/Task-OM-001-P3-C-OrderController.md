# コード生成タスク: Task OM-001-P3-C OrderController実装

## 1. 概要

- **ゴール:** Order管理のAPIエンドポイント（OrderController）を実装する
- **対象ファイル:** `OrderManagement/Controllers/OrderController.cs`
- **リポジトリ:** `order_be`

## 2. 実装の指針

PBIタスクファイルから抽出した実装指針：

**参考**: `docs/candidate_be/Controllers/CandidateController.cs` - 完全準拠

**実装指針**: Controllerテストで定義されたエンドポイント・レスポンス形式に基づいて実装

**必須要素**:
- 統一エラーハンドリング（try-catch-return パターン）
- HTTPステータスコード統一（POST → 201, GET → 200, PUT → 200等）
- [ApiController][Route]アトリビュート
- 以下のエンドポイント実装:
  - POST /api/v1/orders（注文作成）
  - POST /api/v1/orders/{id}/items（注文アイテム追加）
  - PUT /api/v1/orders/{id}/confirm（注文確定）
  - PUT /api/v1/orders/{id}/pay（決済処理）

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

PBI定義ファイル（`docs/tasks/pbi-task-OM-001.md`）から抽出した、このタスクに関連するBusiness RulesとAcceptance Criteria：

#### パーツ3: 注文（右側）- OrderPanel.tsx

**実装要否判定（新規プロジェクト基準）**:
- **Controller**: 必要 (理由: 新規APIエンドポイント POST /api/v1/orders の提供が必要)

**テストコード作成タスク（実装前・TDDファースト）**:

**Task OM-001-P3-CT: OrderController テストコード作成**【~~order_be~~】
- **ファイル**: `Test/Unit/OrderManagement/OrderControllerTests.cs`
- **参考**: `docs/candidate_be/Controllers/CandidateController.cs` のパターン
- **TDD目的**: APIエンドポイントのインターフェースとレスポンス形式を先に定義
- **テスト内容**:
  - POST /api/v1/orders のHTTPステータスコード（201、400、500、503）
  - リクエスト・レスポンス形式検証
  - APIキー認証・エラーハンドリング（統一エラーレスポンス）
  - WebApplicationFactoryを使用した統合テスト

**実装タスク（テストコード後・TDD準拠）**:

**Task OM-001-P3-C: OrderController実装**【~~order_be~~】（本タスク）
- **ファイル**: `OrderManagement/Controllers/OrderController.cs`
- **参考**: `docs/candidate_be/Controllers/CandidateController.cs` - 完全準拠
- **実装指針**: Controllerテストで定義されたエンドポイント・レスポンス形式に基づいて実装
- **必須要素**:
  - 統一エラーハンドリング（try-catch-return パターン）
  - HTTPステータスコード統一
  - [ApiController][Route]アトリビュート
  - POST /api/v1/orders エンドポイント実装

---

### 3.2. 関連データベーススキーマ

`context/database-schema.sql`から抽出した関連テーブル：

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

-- インデックス
CREATE INDEX idx_order_created_at ON "order"(created_at);
CREATE INDEX idx_order_member_card_no ON "order"(member_card_no);
CREATE INDEX idx_order_status ON "order"(status);
CREATE INDEX idx_order_item_order_id ON order_item(order_id);
CREATE INDEX idx_order_item_product_id ON order_item(product_id);
```

---

### 3.3. 関連API仕様

`context/api-integration-design-v2.md`から抽出したOrderControllerの全エンドポイント：

#### エンドポイント1: POST /api/v1/orders（注文作成）

**HTTPリクエスト・レスポンス詳細**:
```http
# Request - 注文作成
POST /api/v1/orders
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "memberCardNo": null
}

# Response - 注文作成成功
HTTP 201 Created
{
  "orderId": 123,
  "status": "IN_ORDER",
  "total": 0,
  "items": []
}

# Response - 注文作成エラー
HTTP 400 Bad Request
{
  "message": "Invalid data provided",
  "detail": "..."
}

HTTP 500 Internal Server Error
{
  "message": "Database service unavailable",
  "detail": "..."
}

HTTP 503 Service Unavailable
{
  "message": "Network connection unavailable",
  "detail": "..."
}
```

#### エンドポイント2: POST /api/v1/orders/{id}/items（注文アイテム追加）

**HTTPリクエスト・レスポンス詳細**:
```http
# Request - 注文アイテム追加
POST /api/v1/orders/123/items
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "productId": 1,
  "quantity": 2
}

# Response - 注文アイテム追加成功
HTTP 200 OK
{
  "orderId": 123,
  "status": "IN_ORDER",
  "total": 600.00,
  "items": [
    {
      "orderItemId": 1,
      "productId": 1,
      "productName": "エスプレッソ",
      "productPrice": 300.00,
      "productDiscountPercent": 0,
      "quantity": 2
    }
  ]
}

# Response - 注文アイテム追加エラー
HTTP 404 Not Found
{
  "message": "Order not found"
}

HTTP 400 Bad Request
{
  "message": "Invalid data provided",
  "detail": "..."
}
```

#### エンドポイント3: PUT /api/v1/orders/{id}/confirm（注文確定）

**HTTPリクエスト・レスポンス詳細**:
```http
# Request - 注文確定（在庫確認後）
PUT /api/v1/orders/123/confirm
Headers: X-API-Key: shop-system-key

# Response - 注文確定成功
HTTP 200 OK
{
  "orderId": 123,
  "status": "CONFIRMED",
  "total": 750.00,
  "confirmed": true,
  "confirmedAt": "2025-09-01T10:30:00Z"
}

# Response - 注文確定エラー
HTTP 400 Bad Request
{
  "error": "OrderConfirmationFailed",
  "message": "注文確定に失敗しました",
  "details": "注文が既に確定済みまたは無効な状態です"
}

HTTP 404 Not Found
{
  "message": "Order not found"
}
```

#### エンドポイント4: PUT /api/v1/orders/{id}/pay（決済処理）

**HTTPリクエスト・レスポンス詳細**:
```http
# Request - 決済完了処理
PUT /api/v1/orders/123/pay
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "paymentMethod": "POINT",
  "memberCardNo": "ABC123DEF456",
  "pointTransactionId": "pt_789"
}

# Response - 決済完了成功
HTTP 200 OK
{
  "orderId": 123,
  "status": "PAID",
  "total": 750.00,
  "paymentMethod": "POINT",
  "pointsUsed": 750,
  "memberNewBalance": 250,
  "paidAt": "2025-09-01T10:45:45Z",
  "paid": true
}

# Response - 決済完了エラー
HTTP 400 Bad Request
{
  "error": "PaymentProcessingFailed",
  "message": "決済処理に失敗しました",
  "details": "ポイント減算または在庫消費が完了していません"
}

HTTP 404 Not Found
{
  "message": "Order not found"
}

HTTP 500 Internal Server Error
{
  "error": "InsufficientPoints",
  "message": "ポイントが不足しています",
  "details": {
    "required": 750,
    "available": 500,
    "shortage": 250
  }
}
```

---

### 3.4. 関連アーキテクチャ & 既存コード

#### アーキテクチャ層の役割（`rules/backend-architecture-layers.md`）

**Controller（コントローラー層）**

**役割**: HTTPリクエスト/レスポンスの処理（APIエンドポイント）

**責務**: リクエスト受付・バリデーション・Service呼び出し・レスポンス返却

**特徴**:
- HTTPメソッド（GET/POST/PUT/DELETE）に対応
- リクエストパラメータ・ボディのバリデーション
- 適切なHTTPステータスコード返却
- 統一されたエラーレスポンス形式
- 認証・認可の処理（APIキー、JWT等）

**テスト方法**: WebApplicationFactory を使用したHTTP統合テスト
- InMemoryDatabaseと組み合わせ
- 実際のHTTPリクエスト送信・レスポンス検証
- エンドツーエンドの動作確認

---

## 4. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
`OrderManagement/Controllers/OrderController.cs`のコードを生成してください。

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたビジネスルール、DBスキーマ、API仕様をすべて満たすように実装してください。
- 参考コードのコーディングスタイル（命名規則、DIパターン、例外処理など）を完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**重要事項:**
- 以下の4つのエンドポイントをすべて実装してください:
  1. POST /api/v1/orders（注文作成）
  2. POST /api/v1/orders/{id}/items（注文アイテム追加）
  3. PUT /api/v1/orders/{id}/confirm（注文確定）
  4. PUT /api/v1/orders/{id}/pay（決済処理）
- 統一エラーハンドリング（try-catch-return パターン）を使用
- HTTPステータスコードを適切に返却（POST → 201, GET → 200, PUT → 200等）
- [ApiController][Route("api/v1/orders")]アトリビュートを使用
- OrderServiceへの依存性注入パターンを使用
- NpgsqlException、SocketException、TimeoutException、DbUpdateException等の例外処理を統一
- エラーレスポンス形式: `{ "message": "...", "detail": "..." }` 形式

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/` ディレクトリ
  - `context/database-schema.sql` - データベーススキーマ
  - `context/api-integration-design-v2.md` - API設計書
- 汎用パターン: `patterns/` ディレクトリ（必須）
  - `patterns/backend/controller-pattern.cs` - Controllerパターンテンプレート
- プロジェクト固有参考実装: `reference/` ディレクトリ（CoffeeShopの場合のみ、オプション）
  - `reference/Controllers/CandidateController.cs` - 参考Controller実装
- ルール定義: `rules/` ディレクトリ
  - `rules/backend-architecture-layers.md` - アーキテクチャ層の役割定義

**生成するコード:**

```csharp
// ここに生成されたコードを記述
```
```

---

## 5. 実装チェックリスト

実装完了後、以下の項目を確認してください：

- [ ] 4つのエンドポイント（POST /api/v1/orders, POST /api/v1/orders/{id}/items, PUT /api/v1/orders/{id}/confirm, PUT /api/v1/orders/{id}/pay）がすべて実装されている
- [ ] [ApiController][Route("api/v1/orders")]アトリビュートが設定されている
- [ ] OrderServiceへの依存性注入が実装されている
- [ ] 統一エラーハンドリング（try-catch-return パターン）が実装されている
- [ ] HTTPステータスコードが適切に返却されている（POST → 201, PUT → 200等）
- [ ] エラーレスポンス形式が統一されている（`{ "message": "...", "detail": "..." }`）
- [ ] NpgsqlException、SocketException、TimeoutException、DbUpdateException等の例外処理が実装されている
- [ ] リクエストパラメータ・ボディのバリデーションが実装されている
- [ ] 参考実装（CandidateController.cs）のコーディングスタイルに準拠している

---

## 6. 別リポジトリへの移動

このタスクディレクトリ全体を別リポジトリにコピーして使用できます：

```bash
cp -r docs/tasks/OM-001/Task-OM-001-P3-C-OrderController /path/to/order_be/docs/tasks/
```

すべての必要情報（指示書、コンテキスト、汎用パターン、参考実装、ルール定義）が1つのディレクトリにまとまっているため、即座にタスクを実施できます。

---

**作成日時**: 2025-11-09
**タスクID**: Task OM-001-P3-C
**PBI-ID**: OM-001
