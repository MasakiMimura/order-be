# コード生成タスク: OR-002-1-ST ProductService テストコード作成

## 1. 概要

- **ゴール:** ProductServiceのテストコードを作成し、ビジネスロジックとメソッドシグネチャを先に定義する（TDD方式）
- **対象ファイル:** `Test/Unit/Product/ProductServiceTests.cs`
- **リポジトリ:** `backend`

## 2. 実装の指針

### 実装パターン参照
→ 0.2. Backend テストパターン > Serviceユニットテストパターン

### 使用ツール
- xUnit
- Moq

### TDD目的
ビジネスロジックとメソッドシグネチャを先に定義

### 期待される成果
Service実装前に、ビジネスルール・Repository協調・レスポンス形式の仕様が明確に定義されたテストコード

### テスト内容
- 商品・カテゴリ一覧取得（GetProductsWithCategoriesAsync）
- カテゴリ別商品フィルタリング（categoryIdパラメータ）
- キャンペーン商品の割引後価格計算（price × (100 - campaign_discount_percent) / 100）
- レスポンス形式の検証（products配列、categories配列）
- 例外の透過的な伝播確認

### 必須テストケース
1. 正常系: 商品一覧とカテゴリ一覧が正しく取得される
2. 正常系: カテゴリID指定で該当商品のみフィルタリングされる
3. 正常系: キャンペーン商品の割引後価格が正しく計算される
4. 異常系: Repository からの例外が Service を透過して伝播する
5. 境界値: 商品が0件の場合、空のproducts配列が返される

### チェックリスト
- [ ] Moqを使用してIProductRepositoryをMock化
- [ ] AAA（Arrange-Act-Assert）パターンを使用
- [ ] Given-When-Then形式のコメントを記載
- [ ] GetProductsWithCategoriesAsync のテストケースを作成
- [ ] キャンペーン割引計算のテストケースを作成
- [ ] Repository協調処理のテストケースを作成

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**OR-002: カテゴリ・商品表示機能**

#### Business Rules
- **カテゴリの動的表示**: カテゴリは商品マスタから取得され、動的に表示される
- **商品カード表示項目**: 商品カードには名前、価格、キャンペーン情報、画像が表示される
- **初期表示時の選択状態**: 初期表示時はカテゴリID=1（最初のカテゴリ）を選択状態とする
- **商品0件の場合の表示**: カテゴリに商品が0件の場合は「商品がありません」を表示する
- **商品画像の遅延読み込み**: 対応しない
- **カテゴリ数が多い場合の対応**: 横スクロールで対応
- **商品カードの詳細情報表示（モーダル・ツールチップ等）**: 対応しない

#### Acceptance Criteria
- [ ] Given レジモードが起動した, When 商品一覧エリアが表示される, Then カテゴリID=1が選択状態として表示される
- [ ] Given カテゴリタブエリアが表示されている, When カテゴリ数が多い, Then 横スクロールで全カテゴリが表示される
- [ ] Given カテゴリが選択されている, When 該当カテゴリに商品が存在する, Then 商品カード（名前、価格、キャンペーン情報、画像）が表示される
- [ ] Given カテゴリが選択されている, When 該当カテゴリに商品が0件, Then 「商品がありません」が表示される
- [ ] Given 商品にキャンペーンが設定されている, When 商品カードが表示される, Then キャンペーン情報（割引率、元値、割引後価格）が表示される
- [ ] Given Product BE APIが正常にレスポンスを返す, When 商品・カテゴリ情報を取得, Then 1秒以内に商品一覧が表示される

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
CREATE INDEX idx_product_category_id ON product(category_id);
CREATE INDEX idx_product_is_active ON product(is_active);
```

### 3.3. 関連API仕様

このタスクはService層のテストであり、直接のAPI仕様はありませんが、Serviceが提供する機能は以下のAPIエンドポイントで使用されます：

- **GET /api/v1/products**: 商品・カテゴリ一覧取得
- **GET /api/v1/products?categoryId={id}**: カテゴリ別商品フィルタリング

レスポンス形式：
```json
{
  "products": [
    {
      "productId": 1,
      "productName": "エスプレッソ",
      "price": 300,
      "isCampaign": true,
      "campaignDiscountPercent": 10,
      "discountedPrice": 270,
      "categoryId": 1,
      "isActive": true
    }
  ],
  "categories": [
    {
      "categoryId": 1,
      "categoryName": "ドリンク",
      "displayOrder": 1
    }
  ]
}
```

### 3.4. 関連アーキテクチャ & 既存コード

#### Service層の役割
- **役割**: 業務ロジック（ビジネスルール）の実装
- **責務**: データの前処理・計算・検証・条件分岐
- **特徴**:
  - Repositoryを呼び出してデータ操作を行う
  - データの前処理・計算・検証・条件分岐などをまとめる
  - 受け入れ条件（入力チェック、ドメインルールの適用など）を定義
  - 「何をどう扱うか」の判断・手順を担う
- **テスト方法**: Moqを使用したRepositoryのMock化

#### 層間の依存関係
```
Controller → Service → Repository → Database
    ↓          ↓           ↓
  HTTP     Business    Data Access
 Request    Logic       (CRUD)
```

---

## 4. 品質基準

### 4.1. テストコードの品質基準

**必須要件:**
- AAA パターン（Arrange-Act-Assert）の厳守
- Given-When-Then 形式のコメント
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`

**Service テストの場合:**
- Moq を使用したRepository Mock化
- ビジネスルール・バリデーションロジックの検証
- Entity↔DTO変換の正確性検証（該当する場合）

### 4.2. TDD方式の注意事項

**重要な注意事項:**
- **テストコードのみを作成し、実装コードは作成しない。**
- テスト対象のクラス（ProductService）は、次のタスクで実装される想定。
- テストは「Red」状態（失敗する状態）で完了することが正しい。
- テストコードで、実装すべきインターフェース（メソッドシグネチャ、戻り値、例外等）を定義する。
- 必要最小限の例外クラス（EntityNotFoundException等）のみ作成可能。

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
`Test/Unit/Product/ProductServiceTests.cs`のテストコードを生成してください。

**品質基準:**
- AAA パターン（Arrange-Act-Assert）の厳守
- Given-When-Then 形式のコメント
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`

**要件:**
- 上記の「実装の指針」に厳密に従ってください。
- 添付の「関連コンテキスト」で提供されたビジネスルール、DBスキーマをすべて満たすように実装してください。
- **汎用パターン（`patterns/`）を厳密に参照してください。**
  - プレースホルダー（`{EntityName}`, `{ProjectName}`, `{DbContextName}`等）を実際の値に置換してください。
  - パターンファイルの構造・命名規則・コーディングスタイルを完全に踏襲してください。
- 不要なコメントは含めず、クリーンで読みやすいコードを生成してください。

**[テストコード作成タスクの追加要件]**
- **テストコードのみを作成し、テスト対象の実装コード（ProductService）は作成しないでください。**
- テスト対象のクラスは次のタスクで実装されるため、テストは「Red」状態（失敗する状態）で完了します。
- テストコードで、実装すべきインターフェース（メソッドシグネチャ、戻り値、例外処理）を明確に定義してください。
- 必要最小限の例外クラス（EntityNotFoundException等）のみ作成可能です。
- **AAA パターン（Arrange-Act-Assert）に厳密に従って記述してください。**
  - 詳細は `rules/backend-test-patterns.md` を参照
  - 例外テストやシンプルな検証など、パターンが適さない場合は柔軟に対応可能です。
  - 重要なのは、テストの意図が明確で、可読性が高いことです。
- **Given-When-Then 形式のコメント**を各テストメソッドに記載してください。
- **テストメソッド命名規則**: `MethodName_Scenario_ExpectedBehavior`
- **Service層テスト**: Moqを使用したRepositoryのMock化

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/` ディレクトリ
- 汎用パターン: `patterns/` ディレクトリ（必須）
- ルール定義: `rules/` ディレクトリ

**プレースホルダー置換:**
汎用パターンファイル（`patterns/`）には以下のプレースホルダーが含まれています：
- `{ProjectName}` → `ProductBE`
- `{EntityName}` → `Product`
- `{DomainName}` → `Product`
- `{EntityDescription}` → `商品`
- `{EntityPluralName}` → `Products`

これらを実際のプロジェクトに合わせて置換してください。

**必須テストケース:**
1. `GetProductsWithCategoriesAsync_NoFilter_ReturnsAllProductsAndCategories`
   - Given: Repository に商品とカテゴリが存在する
   - When: GetProductsWithCategoriesAsync を categoryId=null で実行
   - Then: 全商品と全カテゴリが返される

2. `GetProductsWithCategoriesAsync_WithCategoryId_ReturnsFilteredProducts`
   - Given: Repository に複数カテゴリの商品が存在する
   - When: GetProductsWithCategoriesAsync を categoryId=1 で実行
   - Then: カテゴリID=1 の商品のみ返される

3. `GetProductsWithCategoriesAsync_CampaignProduct_CalculatesDiscountedPrice`
   - Given: キャンペーン商品（is_campaign=true, campaign_discount_percent=10, price=300）が存在する
   - When: GetProductsWithCategoriesAsync を実行
   - Then: 割引後価格（discountedPrice=270）が正しく計算される

4. `GetProductsWithCategoriesAsync_EmptyProducts_ReturnsEmptyProductsArray`
   - Given: Repository に商品が0件
   - When: GetProductsWithCategoriesAsync を実行
   - Then: 空のproducts配列が返される

5. `GetProductsWithCategoriesAsync_RepositoryThrowsNpgsqlException_PropagatesException`
   - Given: Repository が NpgsqlException をスロー
   - When: GetProductsWithCategoriesAsync を実行
   - Then: NpgsqlException が Service を透過して伝播する

6. `GetProductsWithCategoriesAsync_RepositoryThrowsTimeoutException_PropagatesException`
   - Given: Repository が TimeoutException をスロー
   - When: GetProductsWithCategoriesAsync を実行
   - Then: TimeoutException が Service を透過して伝播する

**キャンペーン割引計算式:**
`discountedPrice = price × (100 - campaign_discount_percent) / 100`

例: price=300, campaign_discount_percent=10 の場合
`discountedPrice = 300 × (100 - 10) / 100 = 300 × 90 / 100 = 270`

**生成するコード:**

```csharp
// ここに生成されたテストコードを記述
```

```

---

## 6. ディレクトリ構成

```
tasks/OR-002/OR-002-1-ST-ProductServiceTests/
├── OR-002-1-ST-ProductServiceTests.md  # タスク実施指示書（本ファイル）
├── context/                             # コンテキスト情報
│   └── database-schema.sql
├── patterns/                            # 汎用パターン（必須）
│   └── backend/
│       ├── service-pattern.cs
│       └── service-test-pattern.cs
└── rules/                               # ルールファイル
    ├── backend-architecture-layers.md
    └── backend-test-patterns.md
```
