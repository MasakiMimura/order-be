# コード生成タスク: Task OR-002-1-RT ProductRepository テストコード作成

## 1. 概要

- **ゴール:** ProductRepository（商品リポジトリ）のテストコードをTDD方式で作成。Repository実装前に、メソッドシグネチャ・CRUD操作・例外処理の仕様を明確に定義する。
- **対象ファイル:** `Test/Unit/Product/ProductRepositoryTests.cs`
- **リポジトリ:** `backend`

## 2. 実装の指針

- **実装パターン参照**: → 0.2. Backend テストパターン > Repository統合テストパターン
- **使用ツール**: xUnit、InMemoryDatabase
- **TDD目的**: Repositoryのインターフェースと動作仕様を先に定義
- **期待される成果**: Repository実装前に、メソッドシグネチャ・CRUD操作・例外処理の仕様が明確に定義されたテストコード

**重要な注意事項（TDD方式）:**
- **テストコードのみを作成し、実装コードは作成しない。**
- テスト対象のクラス（ProductRepository等）は、次のタスクで実装される想定。
- テストは「Red」状態（失敗する状態）で完了することが正しい。
- テストコードで、実装すべきインターフェース（メソッドシグネチャ、戻り値、例外等）を定義する。
- 必要最小限の例外クラス（EntityNotFoundException等）のみ作成可能。

---

## 3. 関連コンテキスト

### 3.1. 関連ビジネスルール & 受理条件

**OR-002: カテゴリ・商品表示機能**

**User Story:**
- **As a** 店舗スタッフ
- **I want to** カテゴリタブエリアで商品一覧を確認したい
- **So that** 顧客が注文する商品を素早く選択できる

**Business Rules:**
- **カテゴリの動的表示**: カテゴリは商品マスタから取得され、動的に表示される
- **商品カード表示項目**: 商品カードには名前、価格、キャンペーン情報、画像が表示される
- **初期表示時の選択状態**: 初期表示時はカテゴリID=1（最初のカテゴリ）を選択状態とする
- **商品0件の場合の表示**: カテゴリに商品が0件の場合は「商品がありません」を表示する

**Acceptance Criteria:**
- [ ] Given レジモードが起動した, When 商品一覧エリアが表示される, Then カテゴリID=1が選択状態として表示される
- [ ] Given カテゴリが選択されている, When 該当カテゴリに商品が存在する, Then 商品カード（名前、価格、キャンペーン情報、画像）が表示される
- [ ] Given カテゴリが選択されている, When 該当カテゴリに商品が0件, Then 「商品がありません」が表示される
- [ ] Given 商品にキャンペーンが設定されている, When 商品カードが表示される, Then キャンペーン情報（割引率、元値、割引後価格）が表示される

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

### 3.3. テスト対象メソッド（Repository）

PBIタスクファイルで定義されたメソッド:

1. **GetAllProductsAsync()** - 全商品取得
2. **GetProductsByCategoryIdAsync(int categoryId)** - カテゴリ別商品取得
3. **GetActiveProductsAsync()** - アクティブ商品のみ取得（is_active=true）

**テスト内容:**
- 全商品取得（GetAllProductsAsync）
- カテゴリ別商品取得（GetProductsByCategoryIdAsync）
- アクティブ商品のみ取得（is_active=true）
- 商品とカテゴリのリレーション取得（Include使用）
- 例外処理（NpgsqlException、TimeoutException）
- 存在しないカテゴリIDでの検索（空リスト返却）

**必須テストケース:**
- 正常系: 全商品が正しく取得される
- 正常系: カテゴリID指定で該当商品のみ取得される
- 正常系: is_active=trueの商品のみ取得される
- 正常系: 商品に紐づくカテゴリ情報が取得される
- 異常系: データベース接続エラー→NpgsqlException
- 境界値: 商品が0件の場合、空リストが返される

---

## 4. 品質基準

### 4.1. テストコードの品質基準

**必須要件:**
- AAA パターン（Arrange-Act-Assert）の厳守
- Given-When-Then 形式のコメント
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`

**Repository テストの必須要件:**
- InMemoryDatabase を使用した統合テスト
- CRUD操作の完全なカバレッジ
- 例外処理の検証（EntityNotFoundException等）
- データ整合性・制約違反の検証

**エラーハンドリングパターン:**
- `EntityNotFoundException` → リポジトリで未検出時にスロー
- `NpgsqlException` → データベース接続エラー
- `TimeoutException` → タイムアウトエラー
- `InvalidOperationException` → データベース制約違反

### 4.2. チェックリスト

- [ ] AAA（Arrange-Act-Assert）パターンを使用
- [ ] テストメソッド命名規則（MethodName_Scenario_ExpectedBehavior）に従う
- [ ] InMemoryDatabaseの初期化・クリーンアップを実装
- [ ] GetAllProductsAsync のテストケースを作成
- [ ] GetProductsByCategoryIdAsync のテストケースを作成
- [ ] GetActiveProductsAsync のテストケースを作成
- [ ] リレーションシップのテスト（Include検証）
- [ ] 例外処理のテストケースを作成

---

## 5. 最終コード生成プロンプト

以下のプロンプトをコピーし、コード生成AIに投入してください。

```
あなたは、C#と.NET Core 8.0に精通したシニアソフトウェアエンジニアです。

**ゴール:**
`Test/Unit/Product/ProductRepositoryTests.cs`のテストコードを生成してください。

**品質基準:**
- AAA パターン（Arrange-Act-Assert）の厳守
- Given-When-Then 形式のコメント
- テストメソッド命名規則: `MethodName_Scenario_ExpectedBehavior`
- InMemoryDatabaseを使用した統合テスト

**要件:**
- **テストコードのみを作成し、テスト対象の実装コード（ProductRepository）は作成しないでください。**
- テスト対象のクラスは次のタスクで実装されるため、テストは「Red」状態（失敗する状態）で完了します。
- テストコードで、実装すべきインターフェース（メソッドシグネチャ、戻り値、例外処理）を明確に定義してください。
- 必要最小限の例外クラス（EntityNotFoundException等）のみ作成可能です。

**テスト対象メソッド:**
1. `Task<IEnumerable<Product>> GetAllProductsAsync()` - 全商品取得
2. `Task<IEnumerable<Product>> GetProductsByCategoryIdAsync(int categoryId)` - カテゴリ別商品取得
3. `Task<IEnumerable<Product>> GetActiveProductsAsync()` - アクティブ商品のみ取得

**必須テストケース:**
1. `GetAllProductsAsync_MultipleProducts_ReturnsAllProducts` - 複数商品が正しく取得される
2. `GetAllProductsAsync_NoProducts_ReturnsEmptyList` - 商品が0件の場合、空リストが返される
3. `GetProductsByCategoryIdAsync_ExistingCategory_ReturnsFilteredProducts` - カテゴリID指定で該当商品のみ取得
4. `GetProductsByCategoryIdAsync_NonExistingCategory_ReturnsEmptyList` - 存在しないカテゴリIDで空リスト
5. `GetActiveProductsAsync_MixedProducts_ReturnsOnlyActiveProducts` - is_active=trueの商品のみ取得
6. `GetAllProductsAsync_WithCategory_IncludesCategoryInfo` - 商品にカテゴリ情報が含まれる（Include検証）

**参照ファイル（タスクディレクトリ内）:**
- コンテキスト情報: `context/database-schema.sql`
- 汎用パターン: `patterns/backend/repository-test-pattern.cs`（必須参照）
- ルール定義: `rules/backend-test-patterns.md`, `rules/backend-architecture-layers.md`

**プレースホルダー置換:**
- `{ProjectName}` → `ProductBE`
- `{EntityName}` → `Product`
- `{EntityPluralName}` → `Products`
- `{DomainName}` → `Product`
- `{DbContextName}` → `ProductDbContext`

**データベーススキーマ:**
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
    recipe_id INTEGER NOT NULL,
    category_id INTEGER NOT NULL REFERENCES category(category_id),
    price INTEGER NOT NULL CHECK (price >= 0),
    is_campaign BOOLEAN DEFAULT FALSE NOT NULL,
    campaign_discount_percent INTEGER DEFAULT 0,
    is_active BOOLEAN DEFAULT TRUE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);
```

**生成するコード:**

```csharp
// ここに生成されたテストコードを記述
```
```

---

## 6. 参照ファイル一覧

| 種別 | ファイル | 説明 |
|------|----------|------|
| コンテキスト | `context/database-schema.sql` | データベーススキーマ定義 |
| パターン | `patterns/backend/entity-pattern.cs` | Entity実装パターン |
| パターン | `patterns/backend/repository-pattern.cs` | Repository実装パターン |
| パターン | `patterns/backend/repository-test-pattern.cs` | Repositoryテストパターン（必須） |
| ルール | `rules/backend-architecture-layers.md` | Backendアーキテクチャ層の定義 |
| ルール | `rules/backend-test-patterns.md` | Backendテストパターンの定義 |
