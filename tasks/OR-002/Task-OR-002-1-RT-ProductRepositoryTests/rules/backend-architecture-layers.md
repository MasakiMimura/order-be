# Backend アーキテクチャ層の役割定義

このドキュメントは、.NET Core バックエンドにおける各層（Repository, Service, Controller）の役割と責務を定義します。テストコード作成時は、各層の役割を正確に理解した上で実装してください。

## Repository（リポジトリ層）

**役割**: データベースとのやり取り（CRUD操作）

**責務**: データの保存・取得・更新・削除

**特徴**:
- SQLやORMを通じて実データベースにアクセス
- メソッドの引数はエンティティやDTOなどのデータオブジェクト
- 返り値は配列やリスト形式になることが多い
- 「新規作成・更新・削除・検索」などをメソッド化して提供

**テスト方法**: InMemoryDatabaseを使用した統合テスト

**参考実装**: `patterns/backend/repository-pattern.cs`

---

## Service（サービス層）

**役割**: 業務ロジック（ビジネスルール）の実装

**責務**: データの前処理・計算・検証・条件分岐

**特徴**:
- Repositoryを呼び出してデータ操作を行う
- データの前処理・計算・検証・条件分岐などをまとめる
- 受け入れ条件（入力チェック、ドメインルールの適用など）を定義
- 「何をどう扱うか」の判断・手順を担う

**テスト方法**: Moqを使用したRepositoryのMock化

**参考実装**: `patterns/backend/service-pattern.cs`

---

## Controller（コントローラー層）

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

**参考実装**: `patterns/backend/controller-pattern.cs`

---

## 層間の依存関係

```
Controller → Service → Repository → Database
    ↓          ↓           ↓
  HTTP     Business    Data Access
 Request    Logic       (CRUD)
```

各層は上位から下位への一方向の依存関係を持ちます。下位層が上位層を参照することは禁止です。
