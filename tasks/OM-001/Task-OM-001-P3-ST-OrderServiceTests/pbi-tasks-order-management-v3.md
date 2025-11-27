# PBI タスク詳細化：注文管理 v3

このファイルは、`rules/03-user-story-refinement-template.md`のテンプレートを使用して、プロダクトバックログ（`docs/product-backlog-order-management-v3.md`）の各PBIを実装可能レベルに詳細化したものです。

---

## PBI OM-001: レジモード起動機能

# User Story
As a 店員（レジ担当者）
I want to 左メニュー「レジ」をクリックしてレジ画面を表示する
So that 注文受付業務を開始し、効率的に顧客対応を行うことができる

# Business Rules
- 注文途中での初期化確認：未確定の注文（IN_ORDER状態）がある場合、「注文をクリアして新規注文を開始しますか？」の確認ダイアログを表示する
- 確認OK時：既存注文を破棄し新規注文を作成する
- 確認キャンセル時：既存注文を継続する
- 他モードからの移行時：左メニューから「レジ」以外のモードに移る場合、IN_ORDERレコードは放置（削除しない）
- レジモード再開時：既にレジモード中に「レジ」ボタンを再クリックした場合、上記初期化確認ダイアログを表示する
- 注文途中確認ダイアログのUI：モーダルダイアログで実装する
- 他システムモード（在庫管理、売上分析等）との状態連携：LocalStorageを使用する
- ネットワーク障害時のオフライン動作：対応しない

# UI要素
- 利用する画面：レジ画面（POS System）
- 必要な項目：
  - 商品エリア（カテゴリタブ、商品カード）
  - 注文エリア（選択商品リスト、合計金額表示）
  - 操作ボタン（確定、クリア等）
- 操作フロー：
  1. 左メニュー「レジ」クリック
  2. 初期化確認ダイアログ表示（必要時）
  3. マスタデータ取得（商品・カテゴリ情報）
  4. 注文作成・初期化
  5. レジ画面表示

# Acceptance Criteria
- Given 左メニューの「レジ」ボタンが表示されている, When ボタンをクリックする, Then レジ画面が表示され商品・注文エリアが初期化される
- Given 未確定の注文（IN_ORDER）が存在する, When レジモード起動時, Then 「注文をクリアして新規注文を開始しますか？」確認ダイアログが表示される
- Given 確認ダイアログでOKを選択, When 処理完了時, Then 既存注文が破棄され新規注文が作成される
- Given 確認ダイアログでキャンセルを選択, When 処理完了時, Then 既存注文が継続される
- Given レジモード中に「レジ」ボタン再クリック, When クリック処理時, Then 初期化確認ダイアログを表示する
- Given マスタデータ取得要求, When API呼び出し実行, Then GET /api/v1/products/with-categories でカテゴリ・商品・価格・キャンペーン情報を一括取得する
- Given 新規注文作成要求, When API呼び出し実行, Then POST /api/v1/orders でIN_ORDER状態の注文を作成する
- Given 初期化処理開始, When 処理完了まで, Then 3秒以内にUI表示が完了する

# Architecture / Technical Notes
- 利用技術：React（Frontend）、.NET Core 8.0 + Entity Framework Core（Backend）、PostgreSQL（Database）
- マイクロサービス構成：Product Service、Order Service、User Service
- Entity クラス：
  - Product Service：Unit, Material, Category, Recipe, RecipeIngredient, Product, StockTransaction
  - Order Service：Order, OrderItem
  - User Service：User, PointLedger
- API設計：
  - GET /api/v1/products/with-categories：マスタデータ一括取得
  - POST /api/v1/orders：注文作成（IN_ORDER状態）
- 認証方式：APIキー認証（店内システム）
- 状態管理：React useStateでレジシステム状態管理
- レスポンス要件：初期化からUI表示まで3秒以内
- 依存関係：Product Service（商品マスタ）、Order Service（注文管理）


---

## PBI OM-002: カテゴリ切替・商品表示機能

# User Story
As a 店員（レジ担当者）
I want to カテゴリタブをクリックして該当商品を表示する
So that 効率的に商品を探し、顧客の要望に迅速に対応できる

# Business Rules
- カテゴリは商品マスタから取得され、動的に表示される
- 商品カードには名前、価格、キャンペーン情報、画像が表示される
- 初期表示時はカテゴリID=1（最初のカテゴリ）を選択状態とする
- カテゴリに商品が0件の場合は「商品がありません」を表示する
- 商品画像の遅延読み込み（Lazy Loading）：対応しない
- カテゴリ数が多い場合の対応：横スクロールで対応
- 商品カードの詳細情報表示（モーダル・ツールチップ等）：対応しない

# UI要素
- 利用する画面：レジ画面の商品エリア
- 必要な項目：
  - カテゴリタブ（横並び、アクティブ状態表示）
  - 商品カード（グリッド表示、タップ対応）
  - 商品情報（名前、価格、キャンペーン、画像）
- 操作フロー：
  1. カテゴリタブクリック
  2. 選択カテゴリの商品にフィルタリング
  3. 商品カード表示更新
  4. アクティブタブ状態更新

# Acceptance Criteria
- Given カテゴリタブが表示されている, When 任意のタブをクリック, Then 該当カテゴリの商品のみが表示される
- Given カテゴリ切替処理, When 切替完了時, Then 1秒以内にレスポンスが完了する
- Given 商品カード表示, When カード描画時, Then 名前、価格、キャンペーン情報、画像が表示される
- Given タッチデバイス利用時, When 商品カードタップ, Then タッチ操作に適切に反応する
- Given カテゴリに商品が0件, When カテゴリ選択時, Then 「商品がありません」メッセージが表示される
- Given 初期表示時, When レジ画面表示完了時, Then カテゴリID=1が選択状態で表示される
- Given useState状態管理, When カテゴリ切替時, Then selectedCategoryId状態が適切に更新される

# Architecture / Technical Notes
- 利用技術：React（useState、useEffect）、CSS Grid/Flexbox（レイアウト）
- 状態管理：
  - selectedCategoryId: 選択中カテゴリID
  - products: 全商品データ（マスタから取得済み）
  - categories: カテゴリマスタデータ
- フィルタリング：商品配列のfilter処理（クライアントサイド）
- レスポンス要件：カテゴリ切替から表示更新まで1秒以内
- タッチ対応：CSS touch-action、適切なボタンサイズ（最小44px）


---

## PBI OM-003: 商品選択・注文追加機能

# User Story
As a 店員（レジ担当者）
I want to 商品カードをタップして注文エリアに追加する
So that 顧客の注文を正確に記録し、注文内容を即座に確認できる

# Business Rules
- 商品カードタップで注文エリアに商品が追加される（初期数量1）
- 同じ商品を再タップした場合は数量+1される
- 注文エリアには商品名、単価、数量、小計が表示される
- 合計金額はリアルタイムで計算・表示される
- 注文追加時に在庫チェックは行わない（確定時に実施）
- 税率設定の管理：データベースで管理する
- 商品カードのタップフィードバック：最小限の視覚的フィードバックのみ
- 大量商品選択時のパフォーマンス対策：100商品以上で仮想化実装

# UI要素
- 利用する画面：レジ画面（商品エリア・注文エリア）
- 必要な項目：
  - 商品カード（タップ可能状態）
  - 注文エリア（注文リスト、合計金額表示）
  - 注文アイテム（商品名、単価、数量、小計）
- 操作フロー：
  1. 商品カードタップ
  2. カート状態更新（useState）
  3. 注文エリア表示更新
  4. 合計金額再計算・表示

# Acceptance Criteria
- Given 商品カードが表示されている, When カードをタップ, Then 注文エリアに商品名・数量1が追加される
- Given 注文追加処理, When 処理完了時, Then 0.5秒以内にレスポンスが完了する
- Given 同じ商品を再タップ, When タップ処理時, Then 既存商品の数量が+1される
- Given 商品追加時, When カート状態更新時, Then 合計金額がリアルタイムで再計算される
- Given 注文エリア表示, When 商品追加後, Then 商品名、単価、数量、小計が表示される
- Given 合計金額計算, When 計算処理時, Then 税込み価格で正確に計算される
- Given フロントエンド状態管理, When 商品選択時, Then useStateでcartItems状態が適切に更新される


# Architecture / Technical Notes
- 利用技術：React（useState）、TypeScript（型安全性）、.NET Core 8.0 + Entity Framework Core（Backend）
- 状態管理構造：
  ```typescript
  interface CartItem {
    product: Product;
    quantity: number;
  }
  const [cartItems, setCartItems] = useState<CartItem[]>([]);
  ```
- データフロー：商品タップ → カート状態更新 → UI再描画（APIコール不要）
- 計算処理：税込み価格計算、小計・合計の自動更新
- レスポンス要件：商品タップから表示更新まで0.5秒以内


---

## PBI OM-004: 注文数量管理機能

# User Story
As a 店員（レジ担当者）
I want to 注文した商品の数量を調整・削除する
So that 顧客の要望に応じて注文内容を柔軟に修正できる

# Business Rules
- +/-ボタンで数量を1ずつ増減できる
- 数量を0に設定すると商品が注文から削除される
- 最大数量は99個、最小数量は1個（0で削除）
- 数量変更時は合計金額がリアルタイム更新される
- 同じ商品の再タップでも数量+1される
- 数量0時の削除確認ダイアログ：不要（レジ操作の高速性を優先）
- 数量手入力（キーボード入力）：対応する（大量注文時の効率化）
- 大量数量変更時のパフォーマンス最適化：Debounce（300ms）による更新頻度制御

# UI要素
- 利用する画面：レジ画面の注文エリア
- 必要な項目：
  - +/-ボタン（各注文アイテムに配置）
  - 数量表示（現在の数量）
  - 削除確認UI（数量0時）
- 操作フロー：
  1. +/-ボタンクリック
  2. 数量更新処理
  3. 数量0時の削除処理
  4. 合計金額再計算・表示更新

# Acceptance Criteria
- Given 注文エリアに商品が表示されている, When +ボタンをクリック, Then 数量が1増加し合計金額が更新される
- Given 注文エリアに商品が表示されている, When -ボタンをクリック, Then 数量が1減少し合計金額が更新される
- Given 数量が1の商品, When -ボタンをクリック, Then 商品が注文リストから削除される
- Given 数量変更処理, When 処理完了時, Then 0.3秒以内にレスポンスが完了する
- Given 数量上限制御, When 数量が99に達している, Then +ボタンが無効化される
- Given 同じ商品の商品カード, When 再タップ, Then 注文リストの数量が+1される
- Given 数量変更時, When 状態更新時, Then 小計・合計金額がリアルタイムで再計算される

# Architecture / Technical Notes
- 利用技術：React（useState）、イベントハンドリング、.NET Core 8.0 + Entity Framework Core（Backend）
- 状態更新処理：
  ```typescript
  const updateQuantity = (productId: number, quantity: number) => {
    if (quantity === 0) {
      setCartItems(prev => prev.filter(item => item.product.id !== productId));
    } else {
      setCartItems(prev => prev.map(item => 
        item.product.id === productId ? { ...item, quantity } : item
      ));
    }
    calculateTotal();
  };
  ```
- 境界値制御：最大99個、最小1個（0で削除）
- レスポンス要件：数量変更から表示更新まで0.3秒以内


---

## PBI OM-005: 注文確定・在庫チェック機能

# User Story
As a 店員（レジ担当者）
I want to 「確定」ボタンをクリックして注文を確定し在庫を確保する
So that 注文内容を固定し、顧客に対して商品提供を保証できる

# Business Rules
- 確定前に在庫可用性をチェックする（レシピベース材料在庫確認）
- 在庫不足の場合は確定を停止し、詳細エラー表示する
- 在庫OK時は注文状態をIN_ORDER→CONFIRMEDに変更する
- 確定後は会員カード入力画面を表示する
- 確定処理中はローディング表示し、ボタンを無効化する
- 在庫不足時の注文修正UI：商品別に数量減・削除ボタン表示
- 確定処理失敗時のリトライ：3回まで自動リトライ
- 他店舗の在庫確認・取り寄せ機能：対応しない

# UI要素
- 利用する画面：レジ画面（注文エリア・確認画面）
- 必要な項目：
  - 確定ボタン（注文エリア下部）
  - ローディング表示（処理中）
  - エラー表示（在庫不足時）
  - 会員カード入力画面（確定後）
- 操作フロー：
  1. 確定ボタンクリック
  2. ローディング表示開始
  3. 在庫チェックAPI実行
  4. 結果に応じた画面表示
  5. 確定成功時の次画面遷移

# Acceptance Criteria
- Given 注文エリアに商品が追加されている, When 確定ボタンをクリック, Then 在庫チェックが実行される
- Given 在庫チェック要求, When API呼び出し時, Then POST /api/v1/stocks/availability-check でレシピベース材料在庫を確認する
- Given 在庫チェック結果OK, When 処理完了時, Then PUT /api/v1/orders/{id}/confirm でIN_ORDER→CONFIRMED状態に更新する
- Given 在庫不足検出, When チェック完了時, Then 材料名・不足量を含む詳細エラーを表示し注文修正を促す
- Given 確定成功, When 処理完了時, Then 会員カード入力画面が表示される
- Given 確定処理実行中, When 処理中, Then ローディング表示し確定ボタンを無効化する
- Given 確定処理全体, When 開始から完了まで, Then 5秒以内に処理が完了する

# Architecture / Technical Notes
- 利用技術：React（useState、useEffect）、Axios（API通信）、.NET Core 8.0 + Entity Framework Core（Backend）
- API設計：
  - POST /api/v1/stocks/availability-check：在庫可用性確認
  - PUT /api/v1/orders/{id}/confirm：注文確定
- エラーハンドリング：
  - 在庫不足：材料別詳細表示、注文修正誘導
  - ネットワークエラー：リトライ機能、オフライン対応
- 状態管理：loading状態、error状態、orderStatus状態
- レスポンス要件：確定処理完了まで5秒以内


---

## PBI OM-006: 会員照会機能

# User Story
As a 店員（レジ担当者）
I want to 会員カード番号や名前で会員を検索し情報を表示する
So that 会員向けサービス（ポイント利用）を提供し、顧客満足度を向上させる

# Business Rules
- 会員カード番号入力で直接照会が可能
- 名前による部分一致検索が可能（複数候補表示）
- 会員情報にはポイント残高も含める
- 会員が見つからない場合はゲスト対応とする
- スキップ機能で会員照会をパスできる
- カード番号の桁数・形式：12桁英数（確定）
- 名前検索の最小入力文字数：2文字
- 会員情報の表示項目：基本情報（姓名 + カード番号 + ポイント残高）

# UI要素
- 利用する画面：会員照会画面
- 必要な項目：
  - カード番号入力フィールド
  - 名前検索フィールド
  - 検索ボタン・スキップボタン
  - 会員情報表示（名前、ポイント残高）
  - 候補選択リスト（名前検索時）
- 操作フロー：
  1. 会員カード番号入力 or 名前検索
  2. 検索実行
  3. 結果表示（単一 or 複数候補）
  4. 会員選択確定
  5. 支払方法選択画面遷移

# Acceptance Criteria
- Given 会員カード番号入力フィールド, When カード番号を入力し検索, Then GET /api/v1/users/{cardNo} で会員情報・ポイント残高を取得する
- Given 名前検索フィールド, When 名前を入力し検索, Then GET /api/v1/users/search で部分一致する会員リストを取得する
- Given 名前検索結果が複数, When 検索完了時, Then 候補選択UIを表示し任意の会員を選択できる
- Given 会員情報取得成功, When 照会完了時, Then 会員名・ポイント残高を表示し支払方法選択を有効化する
- Given 会員が見つからない, When 検索結果が0件時, Then ゲスト表示に切り替わりスキップ機能を提供する
- Given スキップボタン, When ボタンクリック時, Then 会員照会をパスし支払方法選択画面に遷移する
- Given 会員照会処理, When 検索から結果表示まで, Then 2秒以内にレスポンスが完了する

# Architecture / Technical Notes
- 利用技術：React（useState、useEffect）、フォームバリデーション、.NET Core 8.0 + Entity Framework Core（Backend）
- API設計：
  - GET /api/v1/users/{cardNo}：カード番号による直接照会
  - GET /api/v1/users/search?name=検索名：名前による部分一致検索
- 入力バリデーション：
  - カード番号：数字のみ、桁数チェック
  - 名前：文字種制限、最小文字数
- 状態管理：searchResult、selectedMember、loading状態
- レスポンス要件：会員照会レスポンス2秒以内


---

## PBI OM-007: ポイント使用判定機能

# User Story
As a 店員（レジ担当者）
I want to 会員のポイント残高を確認し使用可否を判定する
So that 適切な支払方法を提示し、ポイント活用を促進できる

# Business Rules
- ポイント残高と合計金額を比較し使用可否を判定する
- ポイント充分時：「ポイント使用」「現金・カード」選択肢を表示
- ポイント不足時：「現金・カード」のみ表示、不足量を表示
- ポイント使用選択時は使用ポイント・決済後残高を表示
- 1ポイント=1円として計算する
- ポイント使用単位：全額使用のみ
- ポイント有効期限チェック：対応しない
- ポイント不足時の他決済方法併用（一部ポイント使用）：併用非対応

# UI要素
- 利用する画面：支払方法選択画面
- 必要な項目：
  - ポイント残高表示
  - 必要ポイント表示
  - 支払方法選択ボタン（条件に応じて表示）
  - 使用ポイント・決済後残高表示
- 操作フロー：
  1. 会員ポイント残高確認
  2. 合計金額と比較判定
  3. 条件に応じた支払方法表示
  4. 支払方法選択確認

# Acceptance Criteria
- Given 会員ポイント残高と注文合計金額, When 判定処理時, Then ポイント残高≧合計金額で使用可否を判定する
- Given ポイント充分（残高≧合計）, When 判定完了時, Then 「ポイント使用」「現金・カード」選択肢を両方表示する
- Given ポイント不足（残高＜合計）, When 判定完了時, Then 「現金・カード」のみ表示し不足ポイントを表示する
- Given ポイント使用選択時, When 選択確認時, Then 使用ポイント・決済後残高を視覚的に表示する
- Given ポイント残高表示, When 画面描画時, Then 現在残高・必要ポイントを視覚的に比較表示する
- Given 判定処理, When 判定から選択肢表示まで, Then 0.5秒以内にレスポンスが完了する
- Given 支払方法選択後, When 確認UI表示時, Then 使用ポイント・決済後残高が正確に計算・表示される

# Architecture / Technical Notes
- 利用技術：React（useState）、条件分岐レンダリング、.NET Core 8.0 + Entity Framework Core（Backend）
- 判定ロジック：
  ```typescript
  const canUsePoints = memberPoints >= orderTotal;
  const shortagePoints = Math.max(0, orderTotal - memberPoints);
  ```
- UI制御：
  - ポイント充分時：両方の支払方法ボタン表示
  - ポイント不足時：現金・カードボタンのみ、不足表示
- 計算処理：1ポイント=1円、リアルタイム残高計算
- レスポンス要件：判定から選択肢表示まで0.5秒以内


---

## PBI OM-008: 決済処理機能

# User Story
As a 店員（レジ担当者）
I want to 選択した支払方法で決済を実行する
So that 確実な売上計上と商品提供を完了し、業務を円滑に進める

# Business Rules
- 決済実行前に在庫を再確認する（二重チェック）
- 在庫不足時は決済を中止し、注文修正を促す
- ポイント支払時：ポイント減算→在庫消費→決済完了の順序
- 現金・カード支払時：在庫消費→ポイント付与→決済完了の順序
- Frontend主導でAPI順次呼び出し制御を行う
- API呼び出し失敗時のロールバック：自動ロールバック
- 決済途中でのネットワーク断絶時の回復処理：保存済み状態から続行可能なUI表示
- 外部決済API（カード決済等）の統合：対応しない（現金のみ）

# UI要素
- 利用する画面：決済処理画面
- 必要な項目：
  - 支払方法選択ボタン
  - 決済実行ボタン
  - 進捗表示（ローディング・ステップ表示）
  - エラー表示・回復UI
- 操作フロー：
  1. 支払方法選択
  2. 決済実行ボタンクリック
  3. 在庫再確認
  4. 決済処理（API順次実行）
  5. 完了・エラー結果表示

# Acceptance Criteria
- Given 支払方法選択完了, When 決済実行前, Then POST /api/v1/stocks/availability-check で在庫を再確認する
- Given 在庫再確認でNG, When 確認完了時, Then 決済を中止し注文修正を促すエラーを表示する
- Given ポイント支払選択, When 決済実行時, Then POST /api/v1/points/redemption → POST /api/v1/stocks/consumption → PUT /api/v1/orders/{id}/pay の順で実行する
- Given 現金・カード支払選択, When 決済実行時, Then POST /api/v1/stocks/consumption → POST /api/v1/points/accrual → PUT /api/v1/orders/{id}/pay の順で実行する
- Given Frontend主導制御, When API呼び出し時, Then Backend間呼び出しを禁止しFrontend順次制御で実行する
- Given 決済処理中, When 処理実行中, Then ローディング・進捗表示しボタンを無効化する
- Given 決済処理全体, When 開始から完了まで, Then 10秒以内（外部API含む）に処理が完了する

# Architecture / Technical Notes
- 利用技術：React（useState、async/await）、API順次呼び出し、.NET Core 8.0 + Entity Framework Core（Backend）
- API設計・呼び出し順序：
  - 在庫再確認：POST /api/v1/stocks/availability-check
  - ポイント処理：POST /api/v1/points/redemption（減算）、POST /api/v1/points/accrual（付与）
  - 在庫消費：POST /api/v1/stocks/consumption
  - 決済完了：PUT /api/v1/orders/{id}/pay
- エラーハンドリング：各API呼び出し段階でのエラー捕捉・回復処理
- Frontend主導制御：Backend間連携禁止、Frontend責任でのAPI統合
- レスポンス要件：決済完了まで10秒以内


---

## PBI OM-009: 決済完了・レシート表示機能

# User Story
As a 店員（レジ担当者）
I want to 決済完了後にレシート画面を表示する
So that 取引内容を確認し、顧客に正確な情報を提示できる

# Business Rules
- 決済完了後に自動的にレシート画面を表示する
- レシートには注文内容・支払詳細・ポイント変動を表示する
- 取引完了時刻・注文番号を必ず表示する
- データ保存機能を提供する（印刷機能は非対応）
- レシート表示は取引の最終確認として機能する
- レシート印刷機能：実装しない（レシート表示のみ対応）
- データ保存形式の優先度：JSON優先

# UI要素
- 利用する画面：レシート表示画面
- 必要な項目：
  - 注文明細（商品名・数量・単価・小計）
  - 支払詳細（支払方法・支払金額）
  - ポイント変動（使用・獲得ポイント・残高）
  - 取引情報（注文番号・時刻・合計金額）
  - 保存ボタン（印刷機能は非対応）
- 操作フロー：
  1. 決済完了自動遷移
  2. レシートデータ生成
  3. レシート画面表示
  4. 保存操作（任意）

# Acceptance Criteria
- Given 決済処理完了, When 決済完了時, Then レシート画面が自動的に表示される
- Given レシート表示内容, When 画面描画時, Then 注文内容・支払詳細・獲得/使用ポイント・合計金額が表示される
- Given 取引情報表示, When レシート生成時, Then 取引完了時刻・注文番号が必ず含まれる
- Given データ保存機能, When 保存ボタンクリック時, Then レシートデータをファイル出力する
- Given レシート生成処理, When 決済完了から表示まで, Then 2秒以内にレスポンスが完了する
- Given ポイント変動表示, When 会員取引時, Then 使用ポイント・獲得ポイント・決済後残高が正確に表示される

# Architecture / Technical Notes
- 利用技術：React（JSX）、CSS Print Media Queries（印刷対応）、.NET Core 8.0 + Entity Framework Core（Backend）
- データ構造：
  ```typescript
  interface Receipt {
    orderItems: OrderItem[];
    paymentMethod: string;
    pointsUsed: number;
    pointsEarned: number;
    pointsBalance: number;
    orderNumber: string;
    completedAt: Date;
    totalAmount: number;
  }
  ```
- データ保存：JSON形式、CSV形式での出力機能
- レスポンス要件：レシート生成・表示まで2秒以内


---

## PBI OM-010: 新規注文準備機能

# User Story
As a 店員（レジ担当者）
I want to 「新しい注文」ボタンで画面をリセットする
So that 次の顧客の注文を効率的に受け付け、継続的な業務フローを維持できる

# Business Rules
- レシート表示完了後に「新しい注文」ボタンを表示する
- ボタンクリックで全ての注文関連状態をクリアする
- 会員情報・支払方法選択もクリアする
- カテゴリは初期選択（ID=1）に戻す
- 前の注文データは完全にクリアし、データ整合性を確保する
- リセット時の確認ダイアログ：注文内容がある場合のみ確認表示
- 商品マスタデータの再取得：最新取得
- リセット失敗時のエラーハンドリング・回復方法：ページ再読み込み誘導

# UI要素
- 利用する画面：レシート画面→レジ画面（リセット）
- 必要な項目：
  - 「新しい注文」ボタン（レシート画面下部）
  - リセット処理中のローディング表示
- 操作フロー：
  1. レシート表示確認
  2. 「新しい注文」ボタンクリック
  3. 状態リセット処理
  4. レジ画面初期状態表示

# Acceptance Criteria
- Given レシート表示画面, When 「新しい注文」ボタンをクリック, Then 全ての注文関連状態がクリアされる
- Given 状態リセット処理, When リセット実行時, Then 注文状態・カート内容・会員情報・支払方法選択が全てクリアされる
- Given カテゴリ状態リセット, When リセット完了時, Then selectedCategoryがID=1（初期選択）に戻る
- Given 画面遷移処理, When リセット処理完了時, Then レジ画面の初期状態が表示される
- Given データ整合性確保, When リセット処理時, Then 前注文のデータが新規注文に影響しないことを保証する
- Given リセット処理時間, When ボタンクリックから初期画面表示まで, Then 1秒以内にレスポンスが完了する
- Given 状態管理整合性, When リセット後, Then useState状態が適切に初期化され矛盾がない状態になる

# Architecture / Technical Notes
- 利用技術：React（useState）、状態管理リセット、.NET Core 8.0 + Entity Framework Core（Backend）
- リセット処理：
  ```typescript
  const resetOrderState = () => {
    setCartItems([]);
    setMember(null);
    setSelectedPaymentMethod(null);
    setSelectedCategory(1);
    setCurrentOrder(null);
  };
  ```
- 状態整合性：全関連状態の同期リセット、初期値との一致確認
- UI制御：リセット中のローディング表示、ボタン無効化
- レスポンス要件：リセットから初期画面表示まで1秒以内


---

## まとめ

このファイルでは、Coffee Shopの注文管理機能に関する10個のPBIを、実装可能レベルまで詳細化しました。各PBIは以下の要素を含んでいます：

### 主な特徴
- **実装可能な詳細度**: 開発チームがそのまま設計・実装・テストに着手できるレベル
- **Frontend主導設計**: React useStateを活用した状態管理、API呼び出し最小化
- **明確な受け入れ条件**: Given-When-Then形式での具体的な動作定義
- **技術制約の明記**: レスポンス時間、API設計、エラーハンドリング要件
- **未確定事項の明示**: 追加検討が必要な点を明確化

### 技術設計の統一性
- React + TypeScript + useState による状態管理
- Frontend主導のAPI統合制御
- レスポンス時間要件の明確化
- エラーハンドリング・品質要件の標準化

このリファインメント結果は、スプリントプランニングの入力資料として活用し、自動コード生成ツール（Devin等）への入力データとしても利用可能です。