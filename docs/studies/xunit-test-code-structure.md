# xUnit テストコードの構造解説

## 概要

このドキュメントでは、`Test/Unit/OrderManagement/OrderRepositoryTests.cs`を例に、xUnitテストコードの構造と各部分の役割を解説します。

## テストコードとは？

テストコードは、実装したコード（プロダクションコード）が正しく動作するかを自動で検証するためのコードです。

### テストコードのメリット
- バグを早期に発見できる
- コードの変更時に既存機能が壊れていないか確認できる
- コードの使い方を示すドキュメントとしても機能する

---

## ファイル全体の構造

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderBE.Data;
using OrderBE.Exceptions;
using OrderBE.Models;
using OrderBE.Repository;

namespace OrderBE.Tests.Unit.OrderManagement
{
    public class OrderRepositoryTests  // ← テストクラス
    {
        // ヘルパーメソッド
        private OrderDbContext CreateInMemoryContext() { ... }

        // テストメソッド1
        [Fact]
        public async Task CreateOrderAsync_ValidOrder_ReturnsCreatedOrder() { ... }

        // テストメソッド2
        [Fact]
        public async Task CreateOrderAsync_GuestOrder_ReturnsCreatedOrderWithNullMemberCardNo() { ... }

        // ... 他のテストメソッド
    }
}
```

---

## 1. テストクラスの宣言

```csharp
public class OrderRepositoryTests
```

### 役割
- xUnitでは、`public`クラスがテストクラスとして認識されます
- クラス名は慣習的に`{テスト対象クラス名}Tests`とします
- 例: `OrderRepository`のテスト → `OrderRepositoryTests`

---

## 2. テストヘルパーメソッド

```csharp
private OrderDbContext CreateInMemoryContext()
{
    var options = new DbContextOptionsBuilder<OrderDbContext>()
        .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
        .Options;

    return new OrderDbContext(options);
}
```

### 役割
- テストで共通的に使う処理をメソッドとして切り出したもの
- この例では、テスト用のインメモリデータベースを作成

### インメモリデータベースとは？
- 本物のPostgreSQLではなく、メモリ上に一時的に作られるデータベース
- テストが高速に実行できる
- 各テストで独立したDBを使うため、テスト間の干渉を防ぐ
- `Guid.NewGuid()`で毎回異なるDB名を生成することで完全に独立

---

## 3. テストメソッド

### テストメソッドの識別方法

```csharp
[Fact]  // ← この属性がテストメソッドの印
public async Task CreateOrderAsync_ValidOrder_ReturnsCreatedOrder()
```

- **`[Fact]`属性**がついているメソッドがテストメソッド
- xUnitのテストランナーは、この属性を見つけてテストを実行します

### テストメソッドの命名規則

**パターン**: `{メソッド名}_{シナリオ}_{期待される動作}`

**例**:
- `CreateOrderAsync_ValidOrder_ReturnsCreatedOrder`
  - メソッド名: `CreateOrderAsync`
  - シナリオ: `ValidOrder`（正常な注文データ）
  - 期待される動作: `ReturnsCreatedOrder`（作成された注文を返す）

- `GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException`
  - メソッド名: `GetOrderByIdAsync`
  - シナリオ: `NonExistingOrder`（存在しない注文）
  - 期待される動作: `ThrowsEntityNotFoundException`（例外をスロー）

---

## 4. AAA パターン（Arrange-Act-Assert）

テストメソッドの基本構造です。すべてのテストはこの3つのセクションで構成されます。

### 完全な例

```csharp
[Fact]
public async Task CreateOrderAsync_ValidOrder_ReturnsCreatedOrder()
{
    // ============================================
    // Arrange（準備）
    // ============================================
    using var context = CreateInMemoryContext();
    var logger = new Mock<ILogger<OrderRepository>>();
    var repository = new OrderRepository(context, logger.Object);

    var order = new Order
    {
        CreatedAt = DateTime.UtcNow,
        MemberCardNo = "MEMBER00001",
        Total = 1170.00m,
        Status = "IN_ORDER",
        Items = new List<OrderItem>
        {
            new OrderItem
            {
                ProductId = 1,
                ProductName = "カフェラテ",
                ProductPrice = 450.00m,
                ProductDiscountPercent = 0.00m,
                Quantity = 2
            },
            new OrderItem
            {
                ProductId = 2,
                ProductName = "クロワッサン",
                ProductPrice = 300.00m,
                ProductDiscountPercent = 10.00m,
                Quantity = 1
            }
        }
    };

    // ============================================
    // Act（実行）
    // ============================================
    var result = await repository.CreateOrderAsync(order);

    // ============================================
    // Assert（検証）
    // ============================================
    Assert.NotNull(result);
    Assert.True(result.OrderId > 0);
    Assert.Equal("IN_ORDER", result.Status);
    Assert.Equal(2, result.Items.Count);
    Assert.Equal("MEMBER00001", result.MemberCardNo);
    Assert.Equal(1170.00m, result.Total);
}
```

---

### 1️⃣ Arrange（準備）

```csharp
// Arrange
using var context = CreateInMemoryContext();
var logger = new Mock<ILogger<OrderRepository>>();
var repository = new OrderRepository(context, logger.Object);

var order = new Order { ... };
```

#### 役割
テストに必要なデータやオブジェクトを準備します。

#### この例での準備内容
1. **データベースコンテキストの作成**
   ```csharp
   using var context = CreateInMemoryContext();
   ```
   - インメモリDBを作成

2. **モック（偽物）のLoggerを作成**
   ```csharp
   var logger = new Mock<ILogger<OrderRepository>>();
   ```
   - `Mock<T>`は、Moqライブラリを使った偽物オブジェクトの作成
   - テストでは実際のログ出力は不要なので、偽物で代用

3. **テスト対象のインスタンス化**
   ```csharp
   var repository = new OrderRepository(context, logger.Object);
   ```
   - テストしたいクラス（OrderRepository）のインスタンスを作成

4. **テストデータの作成**
   ```csharp
   var order = new Order { ... };
   ```
   - テストに使う入力データを準備

---

### 2️⃣ Act（実行）

```csharp
// Act
var result = await repository.CreateOrderAsync(order);
```

#### 役割
テスト対象のメソッドを実行します。

#### 特徴
- **通常1行〜数行の短いコード**
- テストしたい機能を呼び出すだけ
- この例では、`CreateOrderAsync`メソッドを呼び出して結果を取得

---

### 3️⃣ Assert（検証）

```csharp
// Assert
Assert.NotNull(result);                    // 結果がnullでないことを確認
Assert.True(result.OrderId > 0);           // OrderIdが採番されたことを確認
Assert.Equal("IN_ORDER", result.Status);   // ステータスが正しいことを確認
Assert.Equal(2, result.Items.Count);       // 明細が2件あることを確認
Assert.Equal("MEMBER00001", result.MemberCardNo);  // 会員番号が正しいことを確認
Assert.Equal(1170.00m, result.Total);      // 合計金額が正しいことを確認
```

#### 役割
実行結果が期待通りかを検証します。

#### これがテストコードの本質部分！
- `Assert.XXX()`メソッドで期待値と実際の値を比較
- **1つでも失敗するとテスト全体が失敗**
- テストが成功＝すべてのAssertが成功

#### よく使うAssertメソッド

| メソッド | 意味 | 例 |
|---------|------|-----|
| `Assert.NotNull(value)` | valueがnullでないことを確認 | `Assert.NotNull(result);` |
| `Assert.Null(value)` | valueがnullであることを確認 | `Assert.Null(order.MemberCardNo);` |
| `Assert.True(condition)` | conditionがtrueであることを確認 | `Assert.True(result.OrderId > 0);` |
| `Assert.False(condition)` | conditionがfalseであることを確認 | `Assert.False(string.IsNullOrEmpty(name));` |
| `Assert.Equal(expected, actual)` | 期待値と実際の値が等しいことを確認 | `Assert.Equal("IN_ORDER", result.Status);` |
| `Assert.NotEqual(expected, actual)` | 期待値と実際の値が異なることを確認 | `Assert.NotEqual(0, result.OrderId);` |
| `Assert.Throws<TException>(action)` | 指定した例外がスローされることを確認 | `Assert.Throws<ArgumentException>(() => new Order());` |
| `Assert.ThrowsAsync<TException>(func)` | 非同期で指定した例外がスローされることを確認 | `await Assert.ThrowsAsync<EntityNotFoundException>(...);` |

---

## 5. OrderRepositoryTests.cs内の7つのテストメソッド

このファイルには、OrderRepositoryの機能を検証する7つのテストが含まれています。

### テスト一覧

| # | テストメソッド名 | テスト内容 |
|---|---|---|
| 1 | `CreateOrderAsync_ValidOrder_ReturnsCreatedOrder` | 通常の注文作成が成功するか |
| 2 | `CreateOrderAsync_GuestOrder_ReturnsCreatedOrderWithNullMemberCardNo` | ゲスト注文（会員番号なし）が作成できるか |
| 3 | `GetOrderByIdAsync_ExistingOrder_ReturnsOrderWithItems` | IDで注文を取得できるか（OrderItemをInclude） |
| 4 | `GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException` | 存在しないIDで例外が発生するか |
| 5 | `GetOrdersByStatusAsync_InOrderStatus_ReturnsInOrderOrdersOnly` | ステータス別に注文を取得できるか |
| 6 | `UpdateOrderAsync_ValidUpdate_ReturnsUpdatedOrder` | 注文を更新できるか（Total、Status変更） |
| 7 | `DeleteOrderAsync_ExistingOrder_DeletesOrderAndItems` | 注文と注文明細を削除できるか（CASCADE DELETE） |

### 正常系と異常系

テストは大きく2種類に分類できます：

#### 正常系テスト（Happy Path）
期待通りの入力で、期待通りの結果が返ることを確認
- 例: `CreateOrderAsync_ValidOrder_ReturnsCreatedOrder`

#### 異常系テスト（Error Cases）
エラー状況で、適切に例外がスローされることを確認
- 例: `GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException`

**重要**: 両方のテストが必要です！

---

## 6. 例外のテスト

異常系のテスト例：

```csharp
[Fact]
public async Task GetOrderByIdAsync_NonExistingOrder_ThrowsEntityNotFoundException()
{
    // Arrange
    using var context = CreateInMemoryContext();
    var logger = new Mock<ILogger<OrderRepository>>();
    var repository = new OrderRepository(context, logger.Object);

    // Act & Assert
    await Assert.ThrowsAsync<EntityNotFoundException>(
        async () => await repository.GetOrderByIdAsync(999)
    );
}
```

### ポイント
- `Assert.ThrowsAsync<T>`を使って例外発生を検証
- Act（実行）とAssert（検証）が同時に行われる
- ラムダ式`async () => ...`内でメソッドを実行
- 指定した例外（EntityNotFoundException）がスローされればテスト成功

---

## 7. まとめ：どこがテストコードか？

| コード部分 | 役割 | テストコードか？ |
|---|---|---|
| `using` ステートメント | 名前空間のインポート | ✅ テスト実行に必要 |
| `OrderRepositoryTests` クラス | テストクラス | ✅ テストのコンテナ |
| `CreateInMemoryContext()` メソッド | ヘルパーメソッド | ✅ テスト準備用 |
| `[Fact]` メソッド（8個） | テストメソッド | ✅ **これが本体** |
| `// Arrange` セクション | テスト準備 | ✅ テストの一部 |
| `// Act` セクション | メソッド実行 | ✅ テストの一部 |
| `// Assert` セクション | 結果検証 | ✅ **最重要部分** |

### 結論
**このファイル全体がテストコードで、特に`Assert.XXX()`の部分が「何を確認しているか」を表す最も重要な部分です！**

---

## 8. テストの実行方法

### コマンドライン
```bash
cd Test
dotnet test
```

### 実行結果の見方
```
成功!   -失敗:     0、合格:     7、スキップ:     0、合計:     7、期間: 964 ms
```

- **失敗: 0** - 失敗したテストの数（0が理想）
- **合格: 7** - 成功したテストの数
- **スキップ: 0** - スキップされたテストの数
- **合計: 7** - 全テストの数
- **期間** - テスト実行にかかった時間

---

## 9. TDD（テスト駆動開発）とは？

### TDDの流れ

1. **Red（失敗）**: テストを先に書く → 実装がないので失敗
2. **Green（成功）**: 最小限の実装でテストを通す
3. **Refactor（改善）**: コードをリファクタリング

### このプロジェクトでのTDD
- **Red**: `OrderRepositoryTests.cs`を先に作成（テストのみ） ✅ 完了
- **Green**: `OrderRepository.cs`を実装してテストを通す ✅ 完了（全7テスト合格）
- **Refactor**: コードをリファクタリング（必要に応じて）

**現在の状態**: TDDサイクル完了 - 全テストが合格し、OrderRepositoryの実装が完成しました

### TDDのメリット
- 実装前に「何を作るべきか」が明確になる
- テストがインターフェース設計書の役割を果たす
- 実装後すぐに動作確認ができる

---

## 10. 参考：Moqライブラリ

```csharp
var logger = new Mock<ILogger<OrderRepository>>();
var repository = new OrderRepository(context, logger.Object);
```

### Moqとは？
- テストで使う「偽物のオブジェクト」を作るライブラリ
- 実際のLogger実装は不要（テストでログを確認しないため）

### モックが必要な理由
- テスト対象以外の部分（Logger、外部API等）に依存したくない
- テストを高速化・シンプル化できる
- テスト対象のみに集中できる

---

## まとめ

### テストコードを書く時のチェックリスト

- [ ] テストクラスは`public`で、名前は`{テスト対象}Tests`
- [ ] テストメソッドに`[Fact]`属性をつける
- [ ] メソッド名は`{メソッド名}_{シナリオ}_{期待される動作}`
- [ ] AAAパターン（Arrange-Act-Assert）に従う
- [ ] Arrange: 必要なデータとオブジェクトを準備
- [ ] Act: テスト対象のメソッドを実行（1行）
- [ ] Assert: 結果を検証（複数のAssertでOK）
- [ ] 正常系と異常系の両方をテストする
- [ ] 1つのテストメソッドで1つの観点をテストする

### 次のステップ

1. このファイルの各テストメソッドを読んで、何をテストしているか理解する ✅
2. 実装（OrderRepository.cs）を確認して、テストとの対応を理解する ✅
3. 新しいテストケースを追加してみる（例: 注文明細の更新テスト、複数ステータス取得など）
4. 他のリポジトリクラスでも同様のテストを作成してみる

---

**作成日**: 2025-10-11
**最終更新日**: 2025-10-12
**対象ファイル**: `Test/Unit/OrderManagement/OrderRepositoryTests.cs`
**テストフレームワーク**: xUnit
**プロジェクト**: OrderBE
**テスト状態**: ✅ 全7テスト合格（期間: 964ms）
