# API連携・統合設計書

## 作成情報
- **作成コマンド**: `/create-api-design`
- **入力ファイル**:
  - docs/requirements-specification.md
  - docs/database-schema.sql
- **作成日時**: 2025-12-29

## ドメイン変換テーブル

```
"単位管理" → "UnitManagement"
"材料管理" → "MaterialManagement"
"在庫管理" → "StockManagement"
"レシピ管理" → "RecipeManagement"
"カテゴリ管理" → "CategoryManagement"
"商品管理" → "ProductManagement"
"注文管理" → "OrderManagement"
"ユーザー管理" → "UserManagement"
"ポイント管理" → "PointManagement"
```

## PBI略称テーブル

```
"UnitManagement" → "UN"
"MaterialManagement" → "MM"
"StockManagement" → "ST"
"RecipeManagement" → "RC"
"CategoryManagement" → "CT"
"ProductManagement" → "PR"
"OrderManagement" → "OR"
"UserManagement" → "US"
"PointManagement" → "PT"
```

## 概要・認証方式

### システム構成
- **Product Service**: 単位、材料、在庫履歴、レシピ、カテゴリ、商品管理
- **Order Service**: 注文ステートマシン、決済処理
- **User Service**: 会員CRUD、認証、ポイント台帳

### 認証方式
- **店内システム**: APIキー認証（内部ネットワーク）
- **Web会員**: JWT認証（公開インターネット）

### Frontend主導の設計原則
```
✅ 正しいパターン: Frontend → 複数Backend個別呼び出し
❌ 禁止パターン: Backend → Backend直接呼び出し
```

## 業務フロー別シーケンス図

### 1. Product Service（店内マスタ管理）

#### 1.1 単位管理 - 単位一覧表示

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE

    Note over FE: フロントステージ: 単位管理画面表示
    FE->>FE: 単位管理画面表示

    Note over FE,BE: バックステージ: 単位データ取得
    FE->>BE: GET /api/v1/units（単位一覧取得用）
    BE->>FE: Response: 単位一覧

    Note over FE: フロントステージ: 単位リスト表示
    FE->>FE: 単位リスト表示
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
GET /api/v1/units
Headers: X-API-Key: shop-system-key

# Response Success
{
  "units": [
    {"unitId": 1, "unitCode": "g", "unitName": "グラム"},
    {"unitId": 2, "unitCode": "ml", "unitName": "ミリリットル"}
  ]
}

# Response Error
{"error": "Unauthorized", "message": "APIキーが無効です"}
```

#### 1.2 材料管理 - 材料登録

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE

    Note over FE: フロントステージ: 材料登録フォーム表示
    FE->>FE: 新規登録ボタンクリック・フォーム表示

    Note over FE,BE: バックステージ: 単位マスタ取得
    FE->>BE: GET /api/v1/units（単位選択肢取得用）
    BE->>FE: Response: 単位一覧

    Note over FE: フロントステージ: フォーム入力
    FE->>FE: 材料名入力・単位選択

    Note over FE,BE: バックステージ: 材料登録処理
    FE->>BE: POST /api/v1/materials（材料登録用）
    BE->>FE: Response: 登録結果

    Note over FE: フロントステージ: 結果表示
    FE->>FE: 成功メッセージ表示・一覧画面に戻る
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
POST /api/v1/materials
Headers: X-API-Key: shop-system-key
Body: {"materialName": "コーヒー豆", "unitId": 1}

# Response Success
{"materialId": 1, "materialName": "コーヒー豆", "unitId": 1, "created": true}

# Response Error
{
  "error": "ValidationError",
  "message": "入力値が無効です",
  "details": [{"field": "materialName", "error": "材料名は必須です"}]
}
```

#### 1.3 材料管理 - 削除前依存関係チェック

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE

    Note over FE: フロントステージ: 削除確認
    FE->>FE: 削除ボタンクリック・確認ダイアログ表示

    Note over FE,BE: バックステージ: 削除可能性チェック
    FE->>BE: GET /api/v1/materials/{id}/dependencies（削除可能性確認用）
    BE->>FE: Response: 関連データ存在確認結果

    Note over FE,BE: バックステージ: 材料削除処理
    FE->>BE: DELETE /api/v1/materials/{id}（材料削除用）
    BE->>FE: Response: 削除結果

    Note over FE: フロントステージ: 結果表示
    FE->>FE: 成功メッセージ表示・一覧更新
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - 削除可能性確認
GET /api/v1/materials/1/dependencies
Headers: X-API-Key: shop-system-key

# Response - 削除可能
{
  "canDelete": true,
  "materialId": 1,
  "dependencies": {"recipeIngredients": 0, "stockTransactions": 0}
}

# Response - 削除不可
{
  "canDelete": false,
  "materialId": 1,
  "dependencies": {"recipeIngredients": 2, "stockTransactions": 15},
  "message": "この材料はレシピ2件、在庫履歴15件で使用されているため削除できません"
}

# Request - 材料削除
DELETE /api/v1/materials/1
Headers: X-API-Key: shop-system-key

# Response Success
{"deleted": true, "materialId": 1}

# Response Error
{
  "error": "DependencyError",
  "message": "関連データが存在するため削除できません",
  "dependencies": {"recipeIngredients": 2, "stockTransactions": 15}
}
```

#### 1.4 在庫管理 - 在庫一覧表示

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE

    Note over FE: フロントステージ: 在庫管理画面表示
    FE->>FE: 在庫管理画面表示

    Note over FE,BE: バックステージ: 在庫データ取得
    FE->>BE: GET /api/v1/stocks（在庫一覧取得用）
    BE->>FE: Response: 在庫一覧・現在量・最終更新日

    Note over FE: フロントステージ: 在庫リスト表示
    FE->>FE: 在庫状況表示・不足警告表示
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
GET /api/v1/stocks
Headers: X-API-Key: shop-system-key

# Response Success
{
  "stocks": [
    {
      "materialId": 1,
      "materialName": "コーヒー豆",
      "unitName": "グラム",
      "currentQuantity": 500.000,
      "lastUpdated": "2024-01-15T10:30:00Z"
    }
  ]
}

# Response Error
{"error": "InternalServerError", "message": "在庫データの取得に失敗しました"}
```

#### 1.5 在庫管理 - 入出庫処理

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE

    Note over FE: フロントステージ: 入出庫ダイアログ表示
    FE->>FE: 入庫/出庫ボタンクリック・ダイアログ表示

    Note over FE: フロントステージ: データ入力
    FE->>FE: 数量・理由入力

    Note over FE,BE: バックステージ: 在庫更新処理
    FE->>BE: POST /api/v1/stocks/transactions（在庫取引記録用）
    BE->>FE: Response: 更新結果・新しい在庫量

    Note over FE: フロントステージ: 結果表示
    FE->>FE: 成功メッセージ・在庫一覧更新
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
POST /api/v1/stocks/transactions
Headers: X-API-Key: shop-system-key
Body: {
  "materialId": 1,
  "txType": "IN",
  "quantity": 100.000,
  "reason": "材料仕入れ"
}

# Response Success
{
  "txId": 123,
  "materialId": 1,
  "newQuantity": 600.000,
  "processed": true
}

# Response Error
{
  "error": "ValidationError",
  "details": [{"field": "quantity", "error": "数量は0より大きい値を入力してください"}]
}
```

#### 1.6 商品管理 - 商品一覧表示

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE

    Note over FE: フロントステージ: 商品管理画面表示
    FE->>FE: 商品管理画面表示・カテゴリフィルター

    Note over FE,BE: バックステージ: 商品・カテゴリデータ取得
    FE->>BE: GET /api/v1/products（商品一覧取得用）
    BE->>FE: Response: 商品一覧・カテゴリ・レシピ情報

    Note over FE: フロントステージ: 商品リスト表示
    FE->>FE: カテゴリ別商品表示・キャンペーン情報表示
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
GET /api/v1/products?categoryId=&search=
Headers: X-API-Key: shop-system-key

# Response Success
{
  "products": [
    {
      "productId": 1,
      "productName": "エスプレッソ",
      "price": 300,
      "isCampaign": false,
      "campaignDiscountPercent": 0,
      "categoryId": 1,
      "categoryName": "ドリンク",
      "recipeId": 1,
      "recipeName": "エスプレッソ"
    }
  ],
  "categories": [
    {"categoryId": 1, "categoryName": "ドリンク"},
    {"categoryId": 2, "categoryName": "フード"}
  ]
}

# Response Error
{"error": "InternalServerError", "message": "商品データの取得に失敗しました"}
```

### 2. Order Service（注文・決済処理）

#### 2.1 レジ画面初期化・商品選択

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant Order_BE as Order BE
    participant Product_BE as Product BE

    Note over FE: フロントステージ: レジ画面表示
    FE->>FE: レジ画面表示・カテゴリタブ準備

    Note over FE,Product_BE: バックステージ: 商品・カテゴリ情報取得
    FE->>Product_BE: GET /api/v1/products（商品・カテゴリ一覧取得）
    Product_BE->>FE: Response: カテゴリ一覧・商品一覧・価格・キャンペーン情報

    Note over FE,Order_BE: バックステージ: 注文作成
    FE->>Order_BE: POST /api/v1/orders（注文作成用）
    Order_BE->>FE: Response: 新規注文ID・ステータス=IN_ORDER

    Note over FE: フロントステージ: 商品選択UI表示
    FE->>FE: カテゴリ別商品表示・カート表示
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - 商品情報取得
GET /api/v1/products
Headers: X-API-Key: shop-system-key

# Response Success
{
  "categories": [
    {"categoryId": 1, "categoryName": "ドリンク"},
    {"categoryId": 2, "categoryName": "フード"}
  ],
  "products": [
    {
      "productId": 1,
      "productName": "エスプレッソ",
      "price": 300,
      "isCampaign": false,
      "campaignDiscountPercent": 0,
      "categoryId": 1
    }
  ]
}

# Request - 注文作成
POST /api/v1/orders
Headers: X-API-Key: shop-system-key
Body: {"memberCardNo": null}

# Response Success
{
  "orderId": 123,
  "status": "IN_ORDER",
  "total": 0,
  "items": []
}
```

#### 2.2 注文確定処理

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant Order_BE as Order BE
    participant Product_BE as Product BE

    Note over FE: フロントステージ: 確定ボタンクリック
    FE->>FE: 注文確定ボタンクリック・確認ダイアログ表示

    Note over FE,Product_BE: バックステージ: Frontend主導で在庫確認
    FE->>Product_BE: POST /api/v1/stocks/availability-check（在庫可用性確認）
    Product_BE->>FE: Response: 在庫状況・不足情報

    Note over FE,Order_BE: バックステージ: 注文確定API呼び出し
    FE->>Order_BE: PUT /api/v1/orders/{id}/confirm（注文確定用）
    Order_BE->>FE: Response: 確定結果・ステータス=CONFIRMED

    Note over FE: フロントステージ: 結果表示
    FE->>FE: 確定完了表示・決済ボタン有効化
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - 在庫可用性確認
POST /api/v1/stocks/availability-check
Headers: X-API-Key: shop-system-key
Body: {
  "items": [
    {"productId": 1, "quantity": 2},
    {"productId": 2, "quantity": 1}
  ]
}

# Response - 在庫充足
{
  "available": true,
  "details": [
    {
      "productId": 1,
      "materials": [
        {
          "materialId": 1,
          "materialName": "コーヒー豆",
          "required": 40.000,
          "available": 100.000,
          "sufficient": true
        }
      ]
    }
  ]
}

# Response - 在庫不足
{
  "available": false,
  "details": [
    {
      "materialId": 1,
      "materialName": "コーヒー豆",
      "required": 60.000,
      "available": 30.000,
      "shortage": 30.000
    }
  ]
}

# Request - 注文確定
PUT /api/v1/orders/123/confirm
Headers: X-API-Key: shop-system-key

# Response Success
{
  "orderId": 123,
  "status": "CONFIRMED",
  "total": 750,
  "confirmed": true,
  "confirmedAt": "2025-09-01T10:30:00Z"
}

# Response Error
{
  "error": "OrderConfirmationFailed",
  "message": "注文確定に失敗しました"
}
```

#### 2.3 会員カード照会

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant User_BE as User BE

    Note over FE: フロントステージ: 会員カード入力
    FE->>FE: 会員カード番号入力・照会ボタンクリック

    Note over FE,User_BE: バックステージ: 会員情報取得
    FE->>User_BE: GET /api/v1/users/by-card/{cardNo}（会員照会・残高確認用）
    User_BE->>FE: Response: 会員情報・ポイント残高

    Note over FE: フロントステージ: 会員情報表示
    FE->>FE: 会員名・ポイント残高表示・支払方法選択UI有効化
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
GET /api/v1/users/by-card/ABC123DEF456
Headers: X-API-Key: shop-system-key

# Response Success
{
  "user": {
    "userId": 1,
    "lastName": "田中",
    "firstName": "太郎",
    "cardNo": "ABC123DEF456",
    "email": "tanaka@example.com",
    "pointBalance": 1000,
    "isDeleted": false
  }
}

# Response Error
{
  "error": "UserNotFound",
  "message": "指定された会員カード番号のユーザーが見つかりません",
  "cardNo": "ABC123DEF456"
}
```

#### 2.4 決済処理（ポイント使用）

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant Order_BE as Order BE
    participant User_BE as User BE
    participant Product_BE as Product BE

    Note over FE: フロントステージ: 支払方法選択
    FE->>FE: 支払方法選択（POINT）・決済ボタンクリック

    Note over FE,User_BE: バックステージ: Frontend主導でポイント処理
    FE->>User_BE: POST /api/v1/points/redemption（ポイント減算処理）
    User_BE->>FE: Response: ポイント減算結果

    Note over FE,Product_BE: バックステージ: Frontend主導で在庫減算
    FE->>Product_BE: POST /api/v1/stocks/consumption（在庫消費処理）
    Product_BE->>FE: Response: 在庫減算結果

    Note over FE,Order_BE: バックステージ: 注文完了処理
    FE->>Order_BE: PUT /api/v1/orders/{id}/pay（決済処理用）
    Order_BE->>FE: Response: 決済完了・ステータス=PAID

    Note over FE: フロントステージ: 決済完了表示
    FE->>FE: 決済完了・レシート表示・新規注文準備
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - ポイント減算
POST /api/v1/points/redemption
Headers: X-API-Key: shop-system-key
Body: {
  "memberCardNo": "ABC123DEF456",
  "points": 750,
  "orderId": 123,
  "reason": "ORDER_PAYMENT"
}

# Response Success
{
  "success": true,
  "pointsRedeemed": 750,
  "previousBalance": 1000,
  "currentBalance": 250,
  "transactionId": "pt_789"
}

# Response Error
{
  "success": false,
  "error": "InsufficientPoints",
  "message": "ポイント残高が不足しています",
  "details": {"required": 750, "available": 500, "shortage": 250}
}

# Request - 在庫消費
POST /api/v1/stocks/consumption
Headers: X-API-Key: shop-system-key
Body: {
  "orderId": 123,
  "items": [
    {"productId": 1, "quantity": 2},
    {"productId": 2, "quantity": 1}
  ]
}

# Response Success
{
  "success": true,
  "orderId": 123,
  "stockTransactions": [
    {
      "txId": 789,
      "materialId": 1,
      "txType": "OUT",
      "quantity": -60.000,
      "reason": "ORDER_PAYMENT",
      "orderId": 123
    }
  ],
  "consumedMaterials": [
    {
      "materialId": 1,
      "materialName": "コーヒー豆",
      "consumed": 60.000,
      "previousStock": 1000.000,
      "remainingStock": 940.000
    }
  ]
}

# Response Error
{
  "success": false,
  "error": "InsufficientStock",
  "message": "在庫が不足しています",
  "details": [
    {
      "materialId": 1,
      "materialName": "コーヒー豆",
      "required": 60.000,
      "available": 30.000,
      "shortage": 30.000
    }
  ]
}

# Request - 決済完了処理
PUT /api/v1/orders/123/pay
Headers: X-API-Key: shop-system-key
Body: {
  "paymentMethod": "POINT",
  "memberCardNo": "ABC123DEF456",
  "pointTransactionId": "pt_789"
}

# Response Success
{
  "orderId": 123,
  "status": "PAID",
  "total": 750,
  "paymentMethod": "POINT",
  "pointsUsed": 750,
  "memberNewBalance": 250,
  "paidAt": "2025-09-01T10:45:45Z",
  "paid": true
}

# Response Error
{
  "error": "PaymentProcessingFailed",
  "message": "決済処理に失敗しました"
}
```

#### 2.5 決済処理（OTHER支払い・ポイント付与）

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant Order_BE as Order BE
    participant User_BE as User BE
    participant Product_BE as Product BE

    Note over FE: フロントステージ: 支払方法選択
    FE->>FE: 支払方法選択（OTHER）・決済ボタンクリック

    Note over FE,Product_BE: バックステージ: Frontend主導で在庫減算
    FE->>Product_BE: POST /api/v1/stocks/consumption（在庫消費処理）
    Product_BE->>FE: Response: 在庫減算結果

    Note over FE,User_BE: バックステージ: Frontend主導でポイント付与
    FE->>User_BE: POST /api/v1/points/accrual（ポイント付与処理）
    User_BE->>FE: Response: ポイント付与結果

    Note over FE,Order_BE: バックステージ: 注文完了処理
    FE->>Order_BE: PUT /api/v1/orders/{id}/pay（決済処理用）
    Order_BE->>FE: Response: 決済完了・ステータス=PAID

    Note over FE: フロントステージ: 決済完了表示
    FE->>FE: 決済完了・レシート表示・新規注文準備
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - ポイント付与
POST /api/v1/points/accrual
Headers: X-API-Key: shop-system-key
Body: {
  "memberCardNo": "ABC123DEF456",
  "points": 75,
  "orderId": 123,
  "reason": "ORDER_PAYMENT",
  "baseAmount": 750
}

# Response Success
{
  "success": true,
  "pointsEarned": 75,
  "previousBalance": 1000,
  "currentBalance": 1075,
  "transactionId": "pt_earn_890"
}

# Response Error
{
  "success": false,
  "error": "UserNotFound",
  "message": "指定された会員が見つかりません"
}

# Request - 決済完了処理
PUT /api/v1/orders/123/pay
Headers: X-API-Key: shop-system-key
Body: {
  "paymentMethod": "OTHER",
  "memberCardNo": "ABC123DEF456",
  "pointEarnTransactionId": "pt_earn_890"
}

# Response Success
{
  "orderId": 123,
  "status": "PAID",
  "total": 750,
  "paymentMethod": "OTHER",
  "pointsEarned": 75,
  "memberNewBalance": 1075,
  "paidAt": "2025-09-01T11:00:30Z",
  "paid": true
}
```

### 3. User Service（会員管理）

#### 3.1 会員登録

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as User BE

    Note over FE: フロントステージ: 会員登録フォーム
    FE->>FE: 会員登録フォーム表示・入力

    Note over FE,BE: バックステージ: 会員登録処理
    FE->>BE: POST /api/v1/users/register（会員登録用）
    BE->>FE: Response: 登録結果・カード番号発行

    Note over FE: フロントステージ: 登録完了表示
    FE->>FE: 登録完了・カード番号表示・ログイン誘導
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
POST /api/v1/users/register
Content-Type: application/json
Body: {
  "lastName": "田中",
  "firstName": "太郎",
  "gender": "M",
  "email": "tanaka@example.com",
  "password": "Password123!"
}

# Response Success
{
  "userId": 1,
  "cardNo": "ABC123DEF456",
  "email": "tanaka@example.com",
  "firstName": "太郎",
  "lastName": "田中",
  "registered": true
}

# Response Error
{
  "error": "ValidationError",
  "message": "入力値が無効です",
  "details": [
    {"field": "email", "error": "このメールアドレスは既に登録されています"}
  ]
}
```

#### 3.2 ログイン・認証

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as User BE

    Note over FE: フロントステージ: ログインフォーム
    FE->>FE: ログインフォーム表示・認証情報入力

    Note over FE,BE: バックステージ: 認証処理
    FE->>BE: POST /api/v1/auth/login（認証処理用）
    BE->>FE: Response: JWTトークン・会員情報

    Note over FE: フロントステージ: ログイン完了
    FE->>FE: JWTトークン保存・マイページ画面表示
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
POST /api/v1/auth/login
Content-Type: application/json
Body: {
  "email": "tanaka@example.com",
  "password": "Password123!"
}

# Response Success
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "userId": 1,
    "email": "tanaka@example.com",
    "firstName": "太郎",
    "lastName": "田中",
    "cardNo": "ABC123DEF456",
    "pointBalance": 1250
  }
}

# Response Error
{
  "error": "AuthenticationFailed",
  "message": "メールアドレスまたはパスワードが正しくありません"
}
```

#### 3.3 マイページ・ポイント確認

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as User BE

    Note over FE: フロントステージ: マイページ表示
    FE->>FE: マイページ表示・JWT認証確認

    Note over FE,BE: バックステージ: 会員情報・ポイント取得
    FE->>BE: GET /api/v1/users/me（会員情報取得用）
    BE->>FE: Response: 会員情報・ポイント残高

    Note over FE,BE: バックステージ: ポイント履歴取得
    FE->>BE: GET /api/v1/points/history（ポイント履歴用）
    BE->>FE: Response: ポイント履歴・取引詳細

    Note over FE: フロントステージ: 情報表示
    FE->>FE: プロフィール・ポイント残高・履歴表示
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - 会員情報
GET /api/v1/users/me
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Response Success
{
  "userId": 1,
  "email": "tanaka@example.com",
  "firstName": "太郎",
  "lastName": "田中",
  "gender": "M",
  "cardNo": "ABC123DEF456",
  "pointBalance": 1250,
  "createdAt": "2024-01-01T00:00:00Z"
}

# Request - ポイント履歴
GET /api/v1/points/history?limit=10
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Response Success
{
  "history": [
    {
      "ledgerId": 123,
      "type": "ACCRUAL",
      "points": 75,
      "orderId": 456,
      "occurredAt": "2024-01-15T14:30:00Z",
      "description": "お買い物でポイント獲得"
    },
    {
      "ledgerId": 122,
      "type": "REDEMPTION",
      "points": -500,
      "orderId": 455,
      "occurredAt": "2024-01-14T16:00:00Z",
      "description": "ポイント使用"
    }
  ]
}
```

## 統合シーケンス図

```mermaid
sequenceDiagram
    participant Customer as 顧客
    participant ShopFE as 店内FE
    participant WebFE as Web FE
    participant OrderBE as Order BE
    participant ProductBE as Product BE
    participant UserBE as User BE

    Note over Customer,UserBE: 会員登録・ログイン（Web）
    Customer->>WebFE: 会員登録
    WebFE->>UserBE: POST /api/v1/users/register
    UserBE->>WebFE: カード番号発行

    Note over Customer,UserBE: 店舗での注文・決済（Frontend主導）
    Customer->>ShopFE: 来店・注文
    ShopFE->>ProductBE: GET /api/v1/products
    ShopFE->>OrderBE: POST /api/v1/orders
    ShopFE->>OrderBE: POST /api/v1/orders/{id}/items
    ShopFE->>ProductBE: POST /api/v1/stocks/availability-check
    ShopFE->>OrderBE: PUT /api/v1/orders/{id}/confirm
    ShopFE->>UserBE: GET /api/v1/users/by-card/{cardNo}
    ShopFE->>UserBE: POST /api/v1/points/redemption or /accrual
    ShopFE->>ProductBE: POST /api/v1/stocks/consumption
    ShopFE->>OrderBE: PUT /api/v1/orders/{id}/pay

    Note over Customer,UserBE: ポイント確認（Web）
    Customer->>WebFE: マイページ確認
    WebFE->>UserBE: GET /api/v1/users/me
    WebFE->>UserBE: GET /api/v1/points/history
```

## 実装チェックリスト

### Product Service API
- [ ] GET /api/v1/units - 単位一覧取得
- [ ] POST /api/v1/units - 単位登録
- [ ] GET /api/v1/materials - 材料一覧取得
- [ ] POST /api/v1/materials - 材料登録
- [ ] GET /api/v1/materials/{id}/dependencies - 削除可能性確認
- [ ] DELETE /api/v1/materials/{id} - 材料削除
- [ ] GET /api/v1/stocks - 在庫一覧取得
- [ ] POST /api/v1/stocks/transactions - 在庫取引記録
- [ ] POST /api/v1/stocks/availability-check - 在庫確認
- [ ] POST /api/v1/stocks/consumption - 在庫消費
- [ ] GET /api/v1/products - 商品一覧取得
- [ ] POST /api/v1/products - 商品登録
- [ ] GET /api/v1/categories - カテゴリ一覧取得
- [ ] POST /api/v1/categories - カテゴリ登録

### Order Service API
- [ ] POST /api/v1/orders - 注文作成
- [ ] POST /api/v1/orders/{id}/items - 注文アイテム追加
- [ ] PUT /api/v1/orders/{id}/confirm - 注文確定
- [ ] PUT /api/v1/orders/{id}/pay - 決済処理

### User Service API
- [ ] POST /api/v1/users/register - 会員登録
- [ ] POST /api/v1/auth/login - ログイン
- [ ] GET /api/v1/users/me - 会員情報取得
- [ ] GET /api/v1/users/by-card/{cardNo} - カード番号検索
- [ ] POST /api/v1/points/accrual - ポイント加算
- [ ] POST /api/v1/points/redemption - ポイント減算
- [ ] GET /api/v1/points/history - ポイント履歴

### Frontend実装
- [ ] 単位管理画面（一覧・登録・編集・削除）
- [ ] 材料管理画面（一覧・登録・編集・削除）
- [ ] 在庫管理画面（一覧・入出庫・調整）
- [ ] レシピ管理画面（一覧・登録・編集）
- [ ] カテゴリ管理画面（一覧・登録・編集）
- [ ] 商品管理画面（一覧・登録・編集）
- [ ] レジシステム（商品選択・注文・決済）
- [ ] 会員登録画面
- [ ] ログイン画面
- [ ] マイページ（プロフィール・ポイント履歴）

### 認証・セキュリティ
- [ ] APIキー認証（店内システム）
- [ ] JWT認証（Web会員）
- [ ] CORS設定
- [ ] Input Validation
- [ ] Error Handling

### データベース・パフォーマンス
- [ ] PostgreSQL接続設定
- [ ] Entity Framework設定
- [ ] インデックス作成
- [ ] Materialized View（在庫・ポイント残高）
- [ ] トランザクション制御

### テスト・品質
- [ ] Unit Tests（各API）
- [ ] Integration Tests（API連携）
- [ ] Frontend Tests
- [ ] End-to-End Tests（業務フロー）
- [ ] Performance Tests（負荷テスト）
