# インターフェースを使ったテスト可能な設計

## 概要

このドキュメントでは、Service層のテストを可能にするために行った「インターフェース抽出」のリファクタリングについて解説します。

## なぜこの作業が必要だったのか？

### 問題の発生

`OrderServiceTests.cs`を作成してテストを実行したところ、以下のエラーが発生しました：

```
System.NotSupportedException : Unsupported expression: r => r.CreateOrderAsync(It.IsAny<Order>())
Non-overridable members (here: OrderRepository.CreateOrderAsync) may not be used in setup / verification expressions.
```

### エラーの原因

Service層のテストコードは以下のようにMoqを使ってRepositoryをモック化しようとしていました：

```csharp
// ❌ これが失敗していた
var mockRepository = new Mock<OrderRepository>(MockBehavior.Strict, null, null);
var service = new OrderService(mockRepository.Object);

mockRepository
    .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
    .ReturnsAsync(createdOrder);
```

**問題点**:
- `OrderRepository`は具象クラス（実際の実装を持つクラス）
- そのメソッドは`virtual`ではない
- Moqは`virtual`メソッドまたはインターフェースのメソッドしかモック化できない

---

## 解決策：インターフェース抽出リファクタリング

### リファクタリングの全体像

```
修正前:
OrderService → OrderRepository（具象クラス）
                ↑ モック化不可能

修正後:
OrderService → IOrderRepository（インターフェース）
                ↑ モック化可能
                ↑ 実装
              OrderRepository（具象クラス）
```

### 修正後のコード構造

```csharp
// テストコード
var mockRepository = new Mock<IOrderRepository>();  // ✅ インターフェースをモック化
var service = new OrderService(mockRepository.Object);

mockRepository
    .Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
    .ReturnsAsync(createdOrder);
```

---

## 実装手順

### ステップ1: IOrderRepositoryインターフェースの作成

**ファイル**: `Repository/IOrderRepository.cs`

```csharp
using OrderBE.Models;

namespace OrderBE.Repository
{
    /// <summary>
    /// IOrderRepository（注文リポジトリインターフェース）
    /// データアクセス層のCRUD操作の契約を定義
    /// Service層の単体テストでMock化を可能にする
    /// </summary>
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order> GetOrderByIdAsync(int orderId);
        Task<List<Order>> GetOrdersByStatusAsync(string status);
        Task<Order> UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(int orderId);
    }
}
```

#### ポイント

- **インターフェースは契約**：「何ができるか」を定義するだけ、「どうやるか」は書かない
- メソッドシグネチャ（名前、引数、戻り値）のみを定義
- 実装（`{ ... }`の中身）は書かない

---

### ステップ2: OrderRepositoryにインターフェース実装を追加

**ファイル**: `Repository/OrderRepository.cs`

```csharp
// 修正前
public class OrderRepository
{
    // ...
}

// 修正後
public class OrderRepository : IOrderRepository  // ← : IOrderRepository を追加
{
    // メソッドの実装は変更なし
    public async Task<Order> CreateOrderAsync(Order order)
    {
        // ...既存の実装...
    }
    // ... 他のメソッドも同様
}
```

#### ポイント

- `: IOrderRepository`を追加するだけ
- メソッドの実装自体は一切変更なし
- これで`OrderRepository`は`IOrderRepository`の実装クラスになる

---

### ステップ3: OrderServiceの依存関係を変更

**ファイル**: `Service/OrderService.cs`

```csharp
// 修正前
public class OrderService
{
    private readonly OrderRepository _repository;  // ❌ 具象クラスに依存

    public OrderService(OrderRepository repository)
    {
        _repository = repository;
    }
}

// 修正後
public class OrderService
{
    private readonly IOrderRepository _repository;  // ✅ インターフェースに依存

    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
}
```

#### ポイント

- `OrderRepository` → `IOrderRepository`に変更
- これを**依存性の逆転（Dependency Inversion Principle）**という
- Serviceは「具体的な実装」ではなく「契約（インターフェース）」に依存する

---

### ステップ4: OrderServiceTestsのモック化コードを修正

**ファイル**: `Test/Unit/OrderManagement/OrderServiceTests.cs`

```csharp
// 修正前（全8テスト）
var mockRepository = new Mock<OrderRepository>(MockBehavior.Strict, null, null);  // ❌

// 修正後（全8テスト）
var mockRepository = new Mock<IOrderRepository>();  // ✅ シンプルかつ動作する
```

#### ポイント

- インターフェースのモック化は非常にシンプル
- `MockBehavior.Strict`や`null, null`の引数も不要
- Moqはインターフェースを簡単にモック化できる

---

### ステップ5: Program.csでDI（依存性注入）設定を追加

**ファイル**: `Program.cs`

```csharp
// 修正前
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 修正後
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repository and Service layers
builder.Services.AddScoped<IOrderRepository, OrderRepository>();  // ← 追加
builder.Services.AddScoped<OrderService>();                        // ← 追加
```

#### ポイント

- `AddScoped<IOrderRepository, OrderRepository>()`：
  - 「`IOrderRepository`が必要な時は`OrderRepository`を使う」という設定
  - これで本番環境では実際のOrderRepositoryが使われる
- `AddScoped<OrderService>()`：
  - OrderServiceを依存性注入コンテナに登録
  - Serviceのコンストラクタに`IOrderRepository`が自動で注入される

---

## なぜインターフェースでモック化できるのか？

### 具象クラスのモック化（失敗）

```csharp
public class OrderRepository  // 具象クラス
{
    public async Task<Order> CreateOrderAsync(Order order)  // virtualでない
    {
        // 実際のDB処理
    }
}

// テストでモック化しようとする
var mock = new Mock<OrderRepository>();  // ❌ Moqは継承してメソッドをオーバーライドしようとするが、
                                          //    virtualでないのでオーバーライドできない
```

### インターフェースのモック化（成功）

```csharp
public interface IOrderRepository  // インターフェース
{
    Task<Order> CreateOrderAsync(Order order);  // 実装がない = 必ずオーバーライドされる
}

// テストでモック化する
var mock = new Mock<IOrderRepository>();  // ✅ Moqは簡単に偽の実装を作れる
mock.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
    .ReturnsAsync(new Order { OrderId = 1 });  // 偽の実装を設定
```

---

## テスト実行結果

### 修正後のテスト実行

```bash
dotnet test --filter "FullyQualifiedName~OrderServiceTests"
```

### 結果

```
テストの合計数: 8
     失敗: 8
```

### 失敗内容

```
System.NotImplementedException : CreateOrderAsync is not yet implemented
System.NotImplementedException : GetOrderByIdAsync is not yet implemented
System.NotImplementedException : GetOrdersByStatusAsync is not yet implemented
System.NotImplementedException : UpdateOrderStatusAsync is not yet implemented
System.NotImplementedException : CalculateTotalAsync is not yet implemented
```

### これは正しい状態！（TDD Red状態）

✅ **良い失敗**:
- Moqのエラーが消えた
- テストが実行できるようになった
- `NotImplementedException`で失敗 = 実装がまだないから失敗（当然）

❌ **悪い失敗**（修正前）:
- Moqのエラーでテストが実行すらできない
- テストコードに問題がある

---

## TDDサイクルとの関係

### TDDの3ステップ

1. **Red（レッド）**: テストを書く → 実装がないので失敗 ✅ **現在ここ**
2. **Green（グリーン）**: 最小限の実装でテストを通す
3. **Refactor（リファクタリング）**: コードを改善

### 現在の状態

- ✅ OrderServiceTests.cs: 8つのテストを作成済み
- ✅ インターフェース抽出: テストが実行可能になった
- ✅ TDD Red状態: 全8テスト失敗（NotImplementedException）
- ⏳ 次のタスク: OrderService.csの実装（TDD Green状態へ）

---

## OrderServiceTestsの8つのテスト

| # | テストメソッド名 | テスト内容 | 失敗理由 |
|---|---|---|---|
| 1 | `CreateOrderAsync_ValidOrder_ReturnsCreatedOrder` | 有効な注文データで注文作成が成功するか | CreateOrderAsync未実装 |
| 2 | `CreateOrderAsync_CalculatesTotalCorrectly` | 注文合計が正しく計算されるか（2 × 100 × 0.9 = 180.00） | CreateOrderAsync未実装 |
| 3 | `GetOrderByIdAsync_ExistingOrder_ReturnsOrder` | 既存の注文をIDで取得できるか | GetOrderByIdAsync未実装 |
| 4 | `GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException` | 存在しない注文IDで例外がスローされるか | GetOrderByIdAsync未実装 |
| 5 | `GetOrdersByStatusAsync_InOrderStatus_ReturnsInOrderOrders` | ステータスでフィルタリングして注文を取得できるか | GetOrdersByStatusAsync未実装 |
| 6 | `UpdateOrderStatusAsync_ValidTransition_ReturnsUpdatedOrder` | ステータスを正しく更新できるか（IN_ORDER → CONFIRMED） | UpdateOrderStatusAsync未実装 |
| 7 | `CalculateTotalAsync_MultipleItems_ReturnsCorrectTotal` | 複数明細の合計を正しく計算できるか（1170.00） | CalculateTotalAsync未実装 |
| 8 | `CreateOrderAsync_RepositoryThrowsInvalidOperationException_RethrowsException` | Repository層の例外が正しく再スローされるか | CreateOrderAsync未実装 |

---

## インターフェースを使う利点

### 1. テスト容易性（Testability）

```csharp
// テストでは偽物（モック）を使う
var mockRepo = new Mock<IOrderRepository>();
var service = new OrderService(mockRepo.Object);  // 偽物を注入

// 本番環境では本物を使う
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
```

### 2. 依存性の逆転（Dependency Inversion）

```
悪い設計:
[Service] → [具象クラスRepository]
↑ Service が Repository の実装詳細に依存

良い設計:
[Service] → [IRepository インターフェース] ← [具象クラスRepository]
↑ どちらもインターフェースに依存、実装の詳細は隠蔽
```

### 3. 将来の拡張性

```csharp
// 現在
public class OrderRepository : IOrderRepository  // PostgreSQL版

// 将来、別の実装に差し替え可能
public class CachedOrderRepository : IOrderRepository  // キャッシュ付き版
public class MockOrderRepository : IOrderRepository    // テスト用の完全なモック
```

---

## モック化のメリット（なぜMockが必要か）

### Service層のテストで考えるべきこと

OrderServiceをテストする時、以下のどちらをテストしたいですか？

- ❌ OrderRepositoryが正しくDBにアクセスできるか（これはRepositoryのテスト）
- ✅ OrderServiceのビジネスロジックが正しいか（これがServiceのテスト）

### モックを使わない場合

```csharp
// ❌ モックなし = Repository層も一緒にテストしてしまう
var context = new OrderDbContext(...);  // 実際のDB接続
var repository = new OrderRepository(context, logger);  // 本物のRepository
var service = new OrderService(repository);  // Serviceのテスト？

await service.CreateOrderAsync(order);  // これはServiceとRepositoryの統合テスト

// 問題点:
// - DB接続が必要（遅い）
// - Repositoryのバグの影響を受ける
// - Serviceのロジックだけをテストできない
```

### モックを使う場合

```csharp
// ✅ モック使用 = Service層のみをテスト
var mockRepo = new Mock<IOrderRepository>();
mockRepo.Setup(r => r.CreateOrderAsync(It.IsAny<Order>()))
    .ReturnsAsync(new Order { OrderId = 1 });  // 偽の戻り値を設定

var service = new OrderService(mockRepo.Object);
await service.CreateOrderAsync(order);

// 利点:
// - DB不要（高速）
// - Repositoryの影響を受けない
// - Serviceのビジネスロジックのみをテスト
```

---

## テストの種類と役割分担

### 1. 単体テスト（Unit Test）- 今回のテスト

**OrderRepositoryTests.cs**（前回のテスト）
- 対象: OrderRepository
- モック: ILogger（ログ出力は本質ではない）
- 本物: InMemory Database（軽量なので許容）
- 目的: Repository層のCRUD操作が正しいか

**OrderServiceTests.cs**（今回のテスト）
- 対象: OrderService
- モック: IOrderRepository（Repository層は本質ではない）
- 本物: なし（Service層のみ）
- 目的: Service層のビジネスロジックが正しいか

### 2. 統合テスト（Integration Test）

複数の層を組み合わせてテスト：
```csharp
var context = new OrderDbContext(...);  // 本物のDB
var repository = new OrderRepository(context, logger);  // 本物のRepository
var service = new OrderService(repository);  // 本物のService

// Service → Repository → DB の連携をテスト
```

### 3. E2Eテスト（End-to-End Test）

システム全体をテスト：
```
HTTPリクエスト → API → Service → Repository → DB
```

---

## まとめ

### 今回のリファクタリングで学んだこと

| 項目 | 内容 |
|---|---|
| **問題** | 具象クラスはMoqでモック化できない |
| **解決策** | インターフェース抽出リファクタリング |
| **作成したもの** | `IOrderRepository`インターフェース |
| **変更したもの** | OrderRepository（実装追加）、OrderService（依存先変更）、OrderServiceTests（モック対象変更）、Program.cs（DI設定） |
| **効果** | Service層のテストが実行可能になった |
| **現在の状態** | TDD Red状態（全8テスト失敗、NotImplementedException） |
| **次のステップ** | OrderService.csの実装（TDD Green状態へ） |

### インターフェースを使うべき場面

- ✅ テストでモック化したいクラス
- ✅ 将来的に別の実装に差し替える可能性があるクラス
- ✅ 依存性注入で使うクラス

### インターフェースが不要な場合

- ❌ プライベートなヘルパークラス
- ❌ データクラス（Models、DTOなど）
- ❌ 実装が1つしかなく、今後も増えない単純なクラス

---

## チェックリスト：Service層のテストを作る時

- [ ] Repositoryのインターフェースは存在するか？（`IOrderRepository`など）
- [ ] Repositoryクラスはインターフェースを実装しているか？
- [ ] Serviceクラスは具象クラスではなくインターフェースに依存しているか？
- [ ] テストコードでRepositoryをモック化しているか？（`new Mock<IOrderRepository>()`）
- [ ] Program.csでDI設定を追加したか？（`AddScoped<I〇〇Repository, 〇〇Repository>()`）
- [ ] テストが実行できるか？（Moqエラーが出ないか）
- [ ] テストがRed状態か？（NotImplementedExceptionで失敗しているか）

---

**作成日**: 2025-10-13
**対象タスク**: Task-OM-001-P3-ST-OrderServiceTests
**対象ファイル**:
- `Repository/IOrderRepository.cs`（新規作成）
- `Repository/OrderRepository.cs`（インターフェース実装追加）
- `Service/OrderService.cs`（依存先変更）
- `Test/Unit/OrderManagement/OrderServiceTests.cs`（モック対象変更）
- `Program.cs`（DI設定追加）

**テストフレームワーク**: xUnit
**モックライブラリ**: Moq
**プロジェクト**: OrderBE
**テスト状態**: 🔴 TDD Red状態（全8テスト失敗、NotImplementedException）
