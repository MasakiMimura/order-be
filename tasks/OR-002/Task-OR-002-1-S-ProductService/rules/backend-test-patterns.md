# Backend テストパターン

このドキュメントは、.NET Core バックエンドのテストコード作成時の標準パターンを定義します。

## AAAパターン（テスト構造の標準）

原則として、テストメソッドは以下の3段階構造で記述してください：

### Arrange（準備）
テストデータ・Mock・前提条件の設定

### Act（実行）
テスト対象メソッドの呼び出し

### Assert（検証）
期待値と実際の結果の比較

---

## 例外ケース

以下の場合は、厳密なAAAパターンにならないことがあります：

### 1. 例外テスト
`Assert.ThrowsAsync` 等でAct & Assertが一体化する場合

```csharp
[Fact]
public async Task GetOrderById_NonExistentId_ThrowsEntityNotFoundException()
{
    // Arrange
    var repository = new OrderRepository(_context);

    // Act & Assert（一体化）
    await Assert.ThrowsAsync<EntityNotFoundException>(
        () => repository.GetOrderByIdAsync(999)
    );
}
```

### 2. シンプルな検証
Arrangeが不要な場合

```csharp
[Fact]
public void Order_Constructor_InitializesWithInOrderStatus()
{
    // Act
    var order = new Order();

    // Assert
    Assert.Equal("IN_ORDER", order.Status);
}
```

### 3. パラメータ化テスト
`[Theory]` でArrangeが簡略化される場合

```csharp
[Theory]
[InlineData(100, 2, 0, 200)]
[InlineData(100, 2, 10, 180)]
public void CalculateTotal_VariousInputs_ReturnsCorrectTotal(
    decimal price, int qty, decimal discount, decimal expected)
{
    // Act
    var result = OrderService.CalculateTotal(price, qty, discount);

    // Assert
    Assert.Equal(expected, result);
}
```

---

## 重要な原則

**パターンよりも、テストの意図が明確で可読性が高いことを優先してください。**

AAAパターンは推奨される標準ですが、絶対のルールではありません。

---

## テストメソッド命名規則

**フォーマット**: `MethodName_Scenario_ExpectedBehavior`

### 良い例

```csharp
CreateOrder_ValidRequest_Returns201Created
GetOrderById_NonExistentId_ThrowsEntityNotFoundException
UpdateOrder_InvalidData_Returns400BadRequest
DeleteOrder_ExistingOrder_DeletesSuccessfully
```

### 悪い例

```
Test1()                    // 何をテストしているか不明
CreateOrderTest()          // シナリオと期待結果が不明
TestCreateOrder()          // 同上
CreateOrder()              // テストメソッドだと分かりにくい
```

---

## xUnit アトリビュート

### Fact
パラメータなしの単一テストケース

```csharp
[Fact]
public async Task CreateOrder_ValidRequest_Returns201Created()
{
    // ...
}
```

### Theory
パラメータ化された複数テストケース

```csharp
[Theory]
[InlineData(1, "IN_ORDER")]
[InlineData(2, "CONFIRMED")]
[InlineData(3, "PAID")]
public async Task GetOrderByStatus_ValidStatus_ReturnsOrders(
    int expectedCount, string status)
{
    // ...
}
```

---

## テストの独立性

各テストは独立して実行可能でなければなりません：
- テストの実行順序に依存しない
- テストごとに独立したデータベース状態
- テスト間で状態を共有しない
- 並列実行可能（xUnit のデフォルト動作）
