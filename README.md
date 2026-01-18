# order-be

## 初回セットアップ

### 1. 開発環境設定ファイルの作成

```bash
cp appsettings.Development.json.template appsettings.Development.json
```

### 2. データベース接続情報の設定

`appsettings.Development.json` を編集して、PostgreSQLのパスワードを設定:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=order;Username=postgres;Password=あなたのパスワード"
  }
}
```

### 3. データベースのマイグレーション

```bash
dotnet ef database update
```

### 4. ビルドとテスト

```bash
dotnet build
dotnet test
```

export ASPNETCORE_ENVIRONMENT=Development
dotnet run


---

**注意**: `appsettings.Development.json` は `.gitignore` に含まれており、Gitにコミットされません。各開発者が自分の環境に合わせて作成してください。