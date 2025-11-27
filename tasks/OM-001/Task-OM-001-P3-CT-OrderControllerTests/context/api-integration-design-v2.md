# Coffee Shop API連携・認証統合設計書 v2

## 概要・認証方式

### システム構成
- **Product Service**: 材料・単位、レシピ、在庫履歴、商品・カテゴリ管理
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

### 1.1 材料管理

#### 1.1.1 材料一覧表示

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE
    
    Note over FE: フロントステージ: 材料管理画面表示
    FE->>FE: 材料管理画面表示・検索条件入力
    
    Note over FE,BE: バックステージ: 材料データ取得
    FE->>BE: GET /api/v1/materials（材料一覧取得用）
    BE->>FE: Response: 材料一覧・単位情報
    
    Note over FE: フロントステージ: 検索結果表示
    FE->>FE: 材料リスト表示・ページング処理
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request
GET /api/v1/materials?search=&page=1&limit=50
Headers: X-API-Key: shop-system-key

# Response Success
{
  "materials": [
    {
      "materialId": 1,
      "materialName": "コーヒー豆",
      "unitId": 1,
      "unitName": "グラム",
      "unitCode": "G"
    }
  ],
  "totalCount": 1,
  "page": 1,
  "limit": 50
}

# Response Error
{
  "error": "Unauthorized",
  "message": "APIキーが無効です"
}
```

**React実装例**:
```typescript
const MaterialManagement: React.FC = () => {
  const [materials, setMaterials] = useState<Material[]>([]);
  const [searchTerm, setSearchTerm] = useState('');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchMaterials();
  }, [searchTerm]);

  const fetchMaterials = async () => {
    setLoading(true);
    try {
      const response = await fetch(`/api/v1/materials?search=${searchTerm}&page=1&limit=50`, {
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });
      const data = await response.json();
      setMaterials(data.materials);
    } catch (error) {
      console.error('材料取得エラー:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <input 
        type="text" 
        value={searchTerm} 
        onChange={(e) => setSearchTerm(e.target.value)}
        placeholder="材料名で検索"
      />
      {loading ? <div>Loading...</div> : (
        <table>
          <tbody>
            {materials.map(material => (
              <tr key={material.materialId}>
                <td>{material.materialName}</td>
                <td>{material.unitName}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};
```

#### 1.1.2 材料登録

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
Content-Type: application/json
Body: {
  "materialName": "コーヒー豆",
  "unitId": 1
}

# Response Success
{
  "materialId": 1,
  "materialName": "コーヒー豆",
  "unitId": 1,
  "created": true
}

# Response Error
{
  "error": "ValidationError",
  "message": "入力値が無効です",
  "details": [
    {
      "field": "materialName",
      "error": "材料名は必須です"
    }
  ]
}
```

**React実装例**:
```typescript
const MaterialForm: React.FC = ({ onSuccess }) => {
  const [formData, setFormData] = useState({
    materialName: '',
    unitId: 0
  });
  const [units, setUnits] = useState<Unit[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    fetchUnits();
  }, []);

  const fetchUnits = async () => {
    try {
      const response = await fetch('/api/v1/units', {
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });
      const data = await response.json();
      setUnits(data.units);
    } catch (error) {
      console.error('単位取得エラー:', error);
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    try {
      const response = await fetch('/api/v1/materials', {
        method: 'POST',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(formData)
      });

      if (response.ok) {
        onSuccess();
      } else {
        const errorData = await response.json();
        const newErrors: Record<string, string> = {};
        errorData.details?.forEach((detail: any) => {
          newErrors[detail.field] = detail.error;
        });
        setErrors(newErrors);
      }
    } catch (error) {
      console.error('登録エラー:', error);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <div>
        <label>材料名:</label>
        <input
          type="text"
          value={formData.materialName}
          onChange={(e) => setFormData({...formData, materialName: e.target.value})}
        />
        {errors.materialName && <span className="error">{errors.materialName}</span>}
      </div>
      <div>
        <label>単位:</label>
        <select
          value={formData.unitId}
          onChange={(e) => setFormData({...formData, unitId: parseInt(e.target.value)})}
        >
          <option value={0}>選択してください</option>
          {units.map(unit => (
            <option key={unit.unitId} value={unit.unitId}>
              {unit.unitName}
            </option>
          ))}
        </select>
      </div>
      <button type="submit">登録</button>
    </form>
  );
};
```

#### 1.1.3 材料削除（在庫履歴連携）

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant BE as Product BE
    
    Note over FE: フロントステージ: 材料削除確認
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

# Response - 削除可能（関連データなし）
{
  "canDelete": true,
  "materialId": 1,
  "materialName": "コーヒー豆",
  "dependencies": {
    "recipeIngredients": 0,
    "stockTransactions": 0
  }
}

# Response - 削除不可（関連データあり）
{
  "canDelete": false,
  "materialId": 1,
  "materialName": "コーヒー豆", 
  "dependencies": {
    "recipeIngredients": 2,
    "stockTransactions": 15
  },
  "message": "この材料はレシピ2件、在庫履歴15件で使用されているため削除できません"
}

# Request - 材料削除
DELETE /api/v1/materials/1
Headers: X-API-Key: shop-system-key

# Response - 削除成功
{
  "deleted": true,
  "materialId": 1,
  "materialName": "コーヒー豆"
}

# Response - 削除失敗（依存関係あり）
{
  "error": "DependencyError",
  "message": "関連データが存在するため削除できません",
  "dependencies": {
    "recipeIngredients": 2,
    "stockTransactions": 15
  }
}
```

**React実装例**:
```typescript
const MaterialDeleteConfirm: React.FC<{ materialId: number; onDeleted: () => void }> = ({ 
  materialId, onDeleted 
}) => {
  const [canDelete, setCanDelete] = useState<boolean | null>(null);
  const [dependencies, setDependencies] = useState<any>(null);
  const [loading, setLoading] = useState(false);

  const checkDependencies = async () => {
    setLoading(true);
    try {
      const response = await fetch(`/api/v1/materials/${materialId}/dependencies`, {
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });
      
      const data = await response.json();
      setCanDelete(data.canDelete);
      setDependencies(data.dependencies);
    } catch (error) {
      console.error('依存関係チェックエラー:', error);
    } finally {
      setLoading(false);
    }
  };

  const deleteMaterial = async () => {
    if (!canDelete) return;
    
    setLoading(true);
    try {
      const response = await fetch(`/api/v1/materials/${materialId}`, {
        method: 'DELETE',
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });

      if (response.ok) {
        onDeleted();
      } else {
        const errorData = await response.json();
        alert(`削除失敗: ${errorData.message}`);
      }
    } catch (error) {
      console.error('削除エラー:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    checkDependencies();
  }, [materialId]);

  if (loading) return <div>確認中...</div>;

  return (
    <div className="delete-confirm">
      <h3>材料削除確認</h3>
      
      {canDelete ? (
        <div>
          <p>この材料を削除してもよろしいですか？</p>
          <button onClick={deleteMaterial} disabled={loading}>
            {loading ? '削除中...' : '削除実行'}
          </button>
        </div>
      ) : (
        <div className="cannot-delete">
          <p>この材料は以下の理由で削除できません：</p>
          <ul>
            {dependencies?.recipeIngredients > 0 && (
              <li>レシピで{dependencies.recipeIngredients}件使用中</li>
            )}
            {dependencies?.stockTransactions > 0 && (
              <li>在庫履歴に{dependencies.stockTransactions}件記録あり</li>
            )}
          </ul>
          <p>関連データを削除してから再試行してください。</p>
        </div>
      )}
    </div>
  );
};
```

### 1.2 在庫管理

#### 1.2.1 在庫一覧表示

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
{
  "error": "InternalServerError",
  "message": "在庫データの取得に失敗しました"
}
```

**React実装例**:
```typescript
const StockManagement: React.FC = () => {
  const [stocks, setStocks] = useState<Stock[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    fetchStocks();
  }, []);

  const fetchStocks = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/v1/stocks', {
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });
      const data = await response.json();
      setStocks(data.stocks);
    } catch (error) {
      console.error('在庫取得エラー:', error);
    } finally {
      setLoading(false);
    }
  };

  const isLowStock = (quantity: number) => quantity < 100;

  return (
    <div>
      <h2>在庫管理</h2>
      {loading ? <div>Loading...</div> : (
        <table>
          <thead>
            <tr>
              <th>材料名</th>
              <th>現在量</th>
              <th>単位</th>
              <th>最終更新</th>
              <th>状態</th>
            </tr>
          </thead>
          <tbody>
            {stocks.map(stock => (
              <tr key={stock.materialId} className={isLowStock(stock.currentQuantity) ? 'low-stock' : ''}>
                <td>{stock.materialName}</td>
                <td>{stock.currentQuantity}</td>
                <td>{stock.unitName}</td>
                <td>{new Date(stock.lastUpdated).toLocaleDateString()}</td>
                <td>
                  {isLowStock(stock.currentQuantity) ? (
                    <span className="warning">在庫不足</span>
                  ) : (
                    <span className="normal">正常</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
};
```

#### 1.2.2 在庫入出庫処理

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
Content-Type: application/json
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
  "message": "入力値が無効です",
  "details": [
    {
      "field": "quantity",
      "error": "数量は0より大きい値を入力してください"
    }
  ]
}
```

**React実装例**:
```typescript
const StockTransactionModal: React.FC<{ material: Material; onSuccess: () => void; onClose: () => void }> = ({
  material, onSuccess, onClose
}) => {
  const [formData, setFormData] = useState({
    txType: 'IN' as 'IN' | 'OUT' | 'ADJ',
    quantity: 0,
    reason: ''
  });
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      const response = await fetch('/api/v1/stocks/transactions', {
        method: 'POST',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          materialId: material.materialId,
          ...formData
        })
      });

      if (response.ok) {
        onSuccess();
        onClose();
      } else {
        const errorData = await response.json();
        console.error('在庫更新エラー:', errorData.message);
      }
    } catch (error) {
      console.error('在庫更新エラー:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <h3>{material.materialName} - 在庫更新</h3>
        <form onSubmit={handleSubmit}>
          <div>
            <label>処理種別:</label>
            <select
              value={formData.txType}
              onChange={(e) => setFormData({...formData, txType: e.target.value as 'IN' | 'OUT' | 'ADJ'})}
            >
              <option value="IN">入庫</option>
              <option value="OUT">出庫</option>
              <option value="ADJ">調整</option>
            </select>
          </div>
          <div>
            <label>数量:</label>
            <input
              type="number"
              step="0.001"
              value={formData.quantity}
              onChange={(e) => setFormData({...formData, quantity: parseFloat(e.target.value)})}
              required
            />
            <span>{material.unitName}</span>
          </div>
          <div>
            <label>理由:</label>
            <textarea
              value={formData.reason}
              onChange={(e) => setFormData({...formData, reason: e.target.value})}
              required
            />
          </div>
          <div className="modal-buttons">
            <button type="submit" disabled={loading}>
              {loading ? '処理中...' : '更新'}
            </button>
            <button type="button" onClick={onClose}>キャンセル</button>
          </div>
        </form>
      </div>
    </div>
  );
};
```

### 1.3 商品管理

#### 1.3.1 商品一覧表示

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
      "price": 300.00,
      "isCampaign": false,
      "campaignDiscountPercent": 0,
      "categoryId": 1,
      "categoryName": "エスプレッソ系",
      "recipeId": 1,
      "recipeName": "エスプレッソ"
    }
  ],
  "categories": [
    {
      "categoryId": 1,
      "categoryName": "エスプレッソ系"
    }
  ]
}

# Response Error
{
  "error": "InternalServerError",
  "message": "商品データの取得に失敗しました"
}
```

**React実装例**:
```typescript
const ProductManagement: React.FC = () => {
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<number>(0);
  const [searchTerm, setSearchTerm] = useState('');

  useEffect(() => {
    fetchProducts();
  }, [selectedCategoryId, searchTerm]);

  const fetchProducts = async () => {
    try {
      const response = await fetch(
        `/api/v1/products?categoryId=${selectedCategoryId || ''}&search=${searchTerm}`,
        {
          headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
        }
      );
      const data = await response.json();
      setProducts(data.products);
      setCategories(data.categories);
    } catch (error) {
      console.error('商品取得エラー:', error);
    }
  };

  const calculateDiscountPrice = (price: number, discountPercent: number) => {
    return Math.round(price * (100 - discountPercent) / 100);
  };

  return (
    <div>
      <div className="filters">
        <select
          value={selectedCategoryId}
          onChange={(e) => setSelectedCategoryId(parseInt(e.target.value))}
        >
          <option value={0}>全カテゴリ</option>
          {categories.map(category => (
            <option key={category.categoryId} value={category.categoryId}>
              {category.categoryName}
            </option>
          ))}
        </select>
        <input
          type="text"
          placeholder="商品名で検索"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
        />
      </div>
      
      <div className="product-grid">
        {products.map(product => (
          <div key={product.productId} className="product-card">
            <h3>{product.productName}</h3>
            <p>カテゴリ: {product.categoryName}</p>
            <p>レシピ: {product.recipeName}</p>
            <div className="price">
              {product.isCampaign ? (
                <>
                  <span className="original-price">¥{product.price}</span>
                  <span className="campaign-price">
                    ¥{calculateDiscountPrice(product.price, product.campaignDiscountPercent)}
                  </span>
                  <span className="discount-rate">
                    {product.campaignDiscountPercent}% OFF
                  </span>
                </>
              ) : (
                <span>¥{product.price}</span>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
```

### 2.1 注文処理

#### 2.1.1 レジ画面・商品選択

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant Order_BE as Order BE
    participant Product_BE as Product BE
    participant User_BE as User BE
    
    Note over FE: フロントステージ: レジ画面表示
    FE->>FE: レジ画面表示・カテゴリタブ準備
    
    Note over FE,Product_BE: バックステージ: 商品・カテゴリ情報取得
    FE->>Product_BE: GET /api/v1/products/with-categories（商品・カテゴリ一覧取得）
    Product_BE->>FE: Response: カテゴリ一覧・商品一覧・価格・キャンペーン情報
    
    Note over FE,Order_BE: バックステージ: 注文作成
    FE->>Order_BE: POST /api/v1/orders（注文作成用）
    Order_BE->>FE: Response: 新規注文ID・ステータス=IN_ORDER
    
    Note over FE: フロントステージ: 商品選択UI表示
    FE->>FE: カテゴリ別商品表示・カート表示
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - 商品・カテゴリ情報取得
GET /api/v1/products/with-categories
Headers: X-API-Key: shop-system-key

# Response - 商品・カテゴリ情報取得成功
{
  "categories": [
    {
      "categoryId": 1,
      "categoryName": "エスプレッソ系"
    },
    {
      "categoryId": 2,
      "categoryName": "フラペチーノ"
    },
    {
      "categoryId": 3,
      "categoryName": "ティー"
    }
  ],
  "products": [
    {
      "productId": 1,
      "productName": "エスプレッソ",
      "price": 300.00,
      "isCampaign": false,
      "campaignDiscountPercent": 0,
      "categoryId": 1,
      "categoryName": "エスプレッソ系"
    },
    {
      "productId": 2,
      "productName": "カフェラテ",
      "price": 450.00,
      "isCampaign": true,
      "campaignDiscountPercent": 10.00,
      "categoryId": 1,
      "categoryName": "エスプレッソ系"
    }
  ]
}

# Response - 商品・カテゴリ情報取得エラー
{
  "error": "ProductCategoryRetrievalFailed",
  "message": "商品・カテゴリ情報の取得に失敗しました"
}

# Request - 注文作成
POST /api/v1/orders
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "memberCardNo": null
}

# Response - 注文作成
{
  "orderId": 123,
  "status": "IN_ORDER",
  "total": 0,
  "items": []
}
```

**React実装例**:
```typescript
const PosSystem: React.FC = () => {
  const [currentOrder, setCurrentOrder] = useState<Order | null>(null);
  const [products, setProducts] = useState<Product[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState<number>(1);
  const [member, setMember] = useState<Member | null>(null);

  useEffect(() => {
    initializePosSystem();
  }, []);

  const initializePosSystem = async () => {
    try {
      // 1. POS用マスタデータ一括取得
      await fetchMasterData();
      // 2. 注文作成
      await initializeOrder();
    } catch (error) {
      console.error('POS初期化エラー:', error);
    }
  };

  const fetchMasterData = async () => {
    try {
      const response = await fetch('/api/pos/masterdata', {
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });
      const data = await response.json();
      setCategories(data.categories);
      setProducts(data.products);
    } catch (error) {
      console.error('マスタデータ取得エラー:', error);
    }
  };

  const initializeOrder = async () => {
    try {
      const response = await fetch('/api/v1/orders', {
        method: 'POST',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ memberCardNo: null })
      });
      const orderData = await response.json();
      setCurrentOrder(orderData);
    } catch (error) {
      console.error('注文作成エラー:', error);
    }
  };

  const addItemToOrder = async (product: Product, quantity: number) => {
    if (!currentOrder) return;

    try {
      const response = await fetch(`/api/v1/orders/${currentOrder.orderId}/items`, {
        method: 'POST',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          productId: product.productId,
          quantity: quantity
        })
      });
      
      if (response.ok) {
        const updatedOrder = await response.json();
        setCurrentOrder(updatedOrder);
      }
    } catch (error) {
      console.error('商品追加エラー:', error);
    }
  };

  const filteredProducts = products.filter(p => p.categoryId === selectedCategoryId);

  return (
    <div className="pos-system">
      <div className="pos-header">
        <h2>レジシステム</h2>
        <div className="order-status">
          ステータス: {currentOrder?.status || 'LOADING'}
        </div>
      </div>

      <div className="pos-main">
        <div className="product-section">
          <div className="category-tabs">
            {categories.map(category => (
              <button
                key={category.categoryId}
                className={selectedCategoryId === category.categoryId ? 'active' : ''}
                onClick={() => setSelectedCategoryId(category.categoryId)}
              >
                {category.categoryName}
              </button>
            ))}
          </div>

          <div className="product-grid">
            {filteredProducts.map(product => (
              <div key={product.productId} className="product-card" 
                   onClick={() => addItemToOrder(product, 1)}>
                <h4>{product.productName}</h4>
                <div className="price">¥{product.price}</div>
                {product.isCampaign && (
                  <div className="campaign-badge">
                    {product.campaignDiscountPercent}% OFF
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        <div className="order-section">
          <div className="member-info">
            {member ? (
              <div>
                <p>会員: {member.firstName} {member.lastName}</p>
                <p>ポイント残高: {member.pointBalance}pt</p>
              </div>
            ) : (
              <div>ゲスト注文</div>
            )}
          </div>

          <div className="order-items">
            <table>
              <tbody>
                {currentOrder?.items.map((item, index) => (
                  <tr key={index}>
                    <td>{item.productName}</td>
                    <td>¥{item.productPrice}</td>
                    <td>x{item.quantity}</td>
                    <td>¥{item.productPrice * item.quantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="order-total">
            <h3>合計: ¥{currentOrder?.total || 0}</h3>
          </div>
        </div>
      </div>
    </div>
  );
};
```

#### 2.1.2 注文確定処理

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
Content-Type: application/json
Body: {
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}

# Response - 在庫可用性確認成功（在庫充足）
{
  "available": true,
  "details": [
    {
      "productId": 1,
      "productName": "エスプレッソ",
      "quantity": 2,
      "materials": [
        {
          "materialId": 1,
          "materialName": "コーヒー豆",
          "required": 40.000,
          "available": 100.000,
          "sufficient": true
        }
      ]
    },
    {
      "productId": 2,
      "productName": "カフェラテ",
      "quantity": 1,
      "materials": [
        {
          "materialId": 1,
          "materialName": "コーヒー豆",
          "required": 20.000,
          "available": 60.000,
          "sufficient": true
        },
        {
          "materialId": 2,
          "materialName": "ミルク",
          "required": 150.000,
          "available": 500.000,
          "sufficient": true
        }
      ]
    }
  ]
}

# Response - 在庫可用性確認失敗（在庫不足）
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

# Request - 注文確定（在庫確認後）
PUT /api/v1/orders/123/confirm
Headers: X-API-Key: shop-system-key

# Response - 注文確定成功
{
  "orderId": 123,
  "status": "CONFIRMED",
  "total": 750.00,
  "confirmed": true,
  "confirmedAt": "2025-09-01T10:30:00Z"
}

# Response - 注文確定エラー
{
  "error": "OrderConfirmationFailed",
  "message": "注文確定に失敗しました",
  "details": "注文が既に確定済みまたは無効な状態です"
}
```

**React実装例**:
```typescript
const OrderConfirmation: React.FC<{ order: Order; onConfirmed: (order: Order) => void }> = ({ 
  order, onConfirmed 
}) => {
  const [loading, setLoading] = useState(false);
  const [stockError, setStockError] = useState<StockError | null>(null);

  const confirmOrder = async () => {
    setLoading(true);
    setStockError(null);

    try {
      const response = await fetch(`/api/v1/orders/${order.orderId}/confirm`, {
        method: 'PUT',
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });

      if (response.ok) {
        const confirmedOrder = await response.json();
        onConfirmed(confirmedOrder);
      } else if (response.status === 409) {
        const errorData = await response.json();
        if (errorData.error === 'InsufficientStock') {
          setStockError(errorData);
        }
      }
    } catch (error) {
      console.error('注文確定エラー:', error);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="order-confirmation">
      <h3>注文確定</h3>
      
      <div className="order-summary">
        <h4>注文内容</h4>
        <table>
          <tbody>
            {order.items.map((item, index) => (
              <tr key={index}>
                <td>{item.productName}</td>
                <td>¥{item.productPrice}</td>
                <td>x{item.quantity}</td>
                <td>¥{item.productPrice * item.quantity}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="total">
          合計: ¥{order.total}
        </div>
      </div>

      {stockError && (
        <div className="stock-error">
          <h4>在庫不足</h4>
          <p>{stockError.message}</p>
          <ul>
            {stockError.details.map((detail, index) => (
              <li key={index}>
                {detail.materialName}: 必要量{detail.required} / 在庫{detail.available} 
                (不足{detail.shortage})
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="confirmation-buttons">
        <button 
          onClick={confirmOrder} 
          disabled={loading || order.status !== 'IN_ORDER'}
          className="confirm-button"
        >
          {loading ? '確定中...' : '注文確定'}
        </button>
      </div>
    </div>
  );
};
```

#### 2.1.3 会員カード照会・残高確認

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant User_BE as User BE
    
    Note over FE: フロントステージ: 会員カード入力
    FE->>FE: 会員カード番号入力・照会ボタンクリック
    
    Note over FE,User_BE: バックステージ: 会員情報取得
    FE->>User_BE: GET /api/v1/users/{cardNo}（会員照会・残高確認用）
    User_BE->>FE: Response: 会員情報・ポイント残高
    
    Note over FE: フロントステージ: 会員情報表示
    FE->>FE: 会員名・ポイント残高表示・支払方法選択UI有効化
```

**HTTP リクエスト・レスポンス詳細**:
```http
# Request - 会員照会
GET /api/v1/users/ABC123DEF456
Headers: X-API-Key: shop-system-key

# Response - 会員照会成功
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

# Response - 会員照会失敗（会員なし）
{
  "error": "UserNotFound",
  "message": "指定された会員カード番号のユーザーが見つかりません",
  "cardNo": "ABC123DEF456"
}
```

**React実装例**:
```typescript
const MemberLookup: React.FC<{ onMemberFound: (member: Member) => void }> = ({ onMemberFound }) => {
  const [cardNo, setCardNo] = useState('');
  const [member, setMember] = useState<Member | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');

  const lookupMember = async () => {
    if (!cardNo) return;
    
    setLoading(true);
    setError('');

    try {
      const response = await fetch(`/api/v1/users/${cardNo}`, {
        headers: { 'X-API-Key': process.env.REACT_APP_API_KEY }
      });
      
      if (!response.ok) {
        throw new Error('会員が見つかりません');
      }
      
      const data = await response.json();
      setMember(data.user);
      onMemberFound(data.user);
    } catch (error) {
      setError(error.message);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div>
      <input
        type="text"
        placeholder="会員カード番号"
        value={cardNo}
        onChange={(e) => setCardNo(e.target.value)}
      />
      <button onClick={lookupMember} disabled={loading}>
        {loading ? '照会中...' : '会員照会'}
      </button>
      
      {error && <div className="error">{error}</div>}
      
      {member && (
        <div className="member-info">
          <p>会員名: {member.lastName} {member.firstName}</p>
          <p>ポイント残高: {member.pointBalance} pt</p>
        </div>
      )}
    </div>
  );
};
```

#### 2.1.4 決済処理（ポイント使用）

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant Order_BE as Order BE
    participant User_BE as User BE
    participant Product_BE as Product BE
    
    Note over FE: フロントステージ: 支払方法選択
    FE->>FE: 支払方法選択（POINT/OTHER）・決済ボタンクリック
    
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
Content-Type: application/json
Body: {
  "memberCardNo": "ABC123DEF456",
  "points": 750,
  "orderId": 123,
  "reason": "ORDER_PAYMENT"
}

# Response - ポイント減算成功
{
  "success": true,
  "memberCardNo": "ABC123DEF456",
  "pointsRedeemed": 750,
  "previousBalance": 1000,
  "currentBalance": 250,
  "transactionId": "pt_789",
  "processedAt": "2025-09-01T10:45:00Z"
}

# Response - ポイント減算失敗（残高不足）
{
  "success": false,
  "error": "InsufficientPoints",
  "message": "ポイント残高が不足しています",
  "details": {
    "required": 750,
    "available": 500,
    "shortage": 250
  }
}

# Request - 在庫消費
POST /api/v1/stocks/consumption
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "orderId": 123,
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}

# Response - 在庫消費成功
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
      "orderId": 123,
      "createdAt": "2025-09-01T10:45:30Z"
    },
    {
      "txId": 790,
      "materialId": 2,
      "txType": "OUT", 
      "quantity": -150.000,
      "reason": "ORDER_PAYMENT",
      "orderId": 123,
      "createdAt": "2025-09-01T10:45:30Z"
    }
  ],
  "consumedMaterials": [
    {
      "materialId": 1,
      "materialName": "コーヒー豆",
      "consumed": 60.000,
      "previousStock": 1000.000,
      "remainingStock": 940.000
    },
    {
      "materialId": 2,
      "materialName": "ミルク",
      "consumed": 150.000,
      "previousStock": 500.000,
      "remainingStock": 350.000
    }
  ],
  "processedAt": "2025-09-01T10:45:30Z"
}

# Response - 在庫消費失敗（在庫不足）
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
Content-Type: application/json
Body: {
  "paymentMethod": "POINT",
  "memberCardNo": "ABC123DEF456",
  "pointTransactionId": "pt_789"
}

# Response - 決済完了成功
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
{
  "error": "PaymentProcessingFailed",
  "message": "決済処理に失敗しました",
  "details": "ポイント減算または在庫消費が完了していません"
}
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

**React実装例**:
```typescript
const PaymentProcess: React.FC<{ order: Order; member: Member; onPaymentComplete: () => void }> = ({ 
  order, member, onPaymentComplete 
}) => {
  const [paymentMethod, setPaymentMethod] = useState<'POINT' | 'OTHER'>('OTHER');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');

  const processPayment = async () => {
    setLoading(true);
    setError('');

    try {
      // 1. ポイント減算
      if (paymentMethod === 'POINT') {
        const pointResponse = await fetch('/api/v1/points/redemption', {
          method: 'POST',
          headers: {
            'X-API-Key': process.env.REACT_APP_API_KEY,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            memberCardNo: member.cardNo,
            points: order.total,
            orderId: order.orderId,
            reason: 'ORDER_PAYMENT'
          })
        });

        if (!pointResponse.ok) {
          throw new Error('ポイント減算に失敗しました');
        }
      }

      // 2. 在庫消費
      const stockResponse = await fetch('/api/v1/stocks/consumption', {
        method: 'POST',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          orderId: order.orderId,
          items: order.items
        })
      });

      if (!stockResponse.ok) {
        throw new Error('在庫消費に失敗しました');
      }

      // 3. 決済完了
      const response = await fetch(`/api/v1/orders/${order.orderId}/pay`, {
        method: 'PUT',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          paymentMethod: paymentMethod,
          memberCardNo: member?.cardNo || null
        })
      });

      if (response.ok) {
        const result = await response.json();
        console.log('決済完了:', result);
        onPaymentComplete();
      } else {
        const errorData = await response.json();
        setError(errorData.message);
      }
    } catch (error) {
      console.error('決済エラー:', error);
      setError('決済処理でエラーが発生しました');
    } finally {
      setLoading(false);
    }
  };

  const canUsePoints = member && member.pointBalance >= order.total;

  return (
    <div className="payment-process">
      <h3>決済処理</h3>
      
      <div className="order-summary">
        <p>注文合計: ¥{order.total}</p>
        {member && (
          <p>会員: {member.firstName} {member.lastName}</p>
        )}
      </div>

      <div className="payment-method">
        <h4>支払方法</h4>
        <div>
          <label>
            <input
              type="radio"
              value="OTHER"
              checked={paymentMethod === 'OTHER'}
              onChange={(e) => setPaymentMethod(e.target.value as 'OTHER')}
            />
            現金・カード
          </label>
        </div>
        
        {member && (
          <div>
            <label>
              <input
                type="radio"
                value="POINT"
                checked={paymentMethod === 'POINT'}
                onChange={(e) => setPaymentMethod(e.target.value as 'POINT')}
                disabled={!canUsePoints}
              />
              ポイント使用 
              {member && (
                <span>
                  (残高: {member.pointBalance}pt)
                  {!canUsePoints && <span className="error"> - 残高不足</span>}
                </span>
              )}
            </label>
          </div>
        )}
      </div>

      {paymentMethod === 'POINT' && member && (
        <div className="point-usage">
          <p>使用ポイント: {order.total}pt</p>
          <p>決済後残高: {member.pointBalance - order.total}pt</p>
        </div>
      )}

      {paymentMethod === 'OTHER' && member && (
        <div className="point-earn">
          <p>獲得予定ポイント: {Math.floor(order.total * 0.1)}pt</p>
        </div>
      )}

      {error && (
        <div className="error-message">
          {error}
        </div>
      )}

      <div className="payment-buttons">
        <button 
          onClick={processPayment} 
          disabled={loading || order.status !== 'CONFIRMED'}
          className="payment-button"
        >
          {loading ? '決済中...' : '決済実行'}
        </button>
      </div>
    </div>
  );
};
```

#### 2.1.5 決済処理（OTHER支払い・ポイント付与）

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
# Request - 在庫消費
POST /api/v1/stocks/consumption
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "orderId": 123,
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 2,
      "quantity": 1
    }
  ]
}

# Response - 在庫消費成功
{
  "success": true,
  "orderId": 123,
  "stockTransactions": [
    {
      "txId": 791,
      "materialId": 1,
      "txType": "OUT",
      "quantity": -60.000,
      "reason": "ORDER_PAYMENT",
      "orderId": 123,
      "createdAt": "2025-09-01T11:00:00Z"
    },
    {
      "txId": 792,
      "materialId": 2,
      "txType": "OUT",
      "quantity": -150.000,
      "reason": "ORDER_PAYMENT", 
      "orderId": 123,
      "createdAt": "2025-09-01T11:00:00Z"
    }
  ],
  "consumedMaterials": [
    {
      "materialId": 1,
      "materialName": "コーヒー豆",
      "consumed": 60.000,
      "previousStock": 1000.000,
      "remainingStock": 940.000
    },
    {
      "materialId": 2,
      "materialName": "ミルク",
      "consumed": 150.000,
      "previousStock": 500.000,
      "remainingStock": 350.000
    }
  ],
  "processedAt": "2025-09-01T11:00:00Z"
}

# Response - 在庫消費失敗（在庫不足）
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

# Request - ポイント付与（OTHER支払い時のみ）
POST /api/v1/points/accrual
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "memberCardNo": "ABC123DEF456",
  "points": 75,
  "orderId": 123,
  "reason": "ORDER_PAYMENT",
  "baseAmount": 750.00
}

# Response - ポイント付与成功
{
  "success": true,
  "memberCardNo": "ABC123DEF456",
  "pointsEarned": 75,
  "previousBalance": 1000,
  "currentBalance": 1075,
  "transactionId": "pt_earn_890",
  "processedAt": "2025-09-01T11:00:15Z"
}

# Response - ポイント付与失敗（会員なし）
{
  "success": false,
  "error": "UserNotFound",
  "message": "指定された会員が見つかりません",
  "cardNo": "ABC123DEF456"
}

# Request - 決済完了処理
PUT /api/v1/orders/123/pay
Headers: X-API-Key: shop-system-key
Content-Type: application/json
Body: {
  "paymentMethod": "OTHER",
  "memberCardNo": "ABC123DEF456",
  "pointEarnTransactionId": "pt_earn_890"
}

# Response - 決済完了成功
{
  "orderId": 123,
  "status": "PAID",
  "total": 750.00,
  "paymentMethod": "OTHER",
  "pointsEarned": 75,
  "memberNewBalance": 1075,
  "paidAt": "2025-09-01T11:00:30Z",
  "paid": true
}

# Response - 決済完了エラー
{
  "error": "PaymentProcessingFailed",
  "message": "決済処理に失敗しました",
  "details": "在庫消費またはポイント付与が完了していません"
}
```

**React実装例**:
```typescript
const OtherPaymentProcess: React.FC<{ order: Order; member?: Member; onPaymentComplete: () => void }> = ({ 
  order, member, onPaymentComplete 
}) => {
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');

  const processOtherPayment = async () => {
    setLoading(true);
    setError('');

    try {
      // 1. 在庫消費
      const stockResponse = await fetch('/api/v1/stocks/consumption', {
        method: 'POST',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          orderId: order.orderId,
          items: order.items
        })
      });

      if (!stockResponse.ok) {
        throw new Error('在庫消費に失敗しました');
      }

      let pointTransactionId = null;

      // 2. ポイント付与（会員のみ）
      if (member) {
        const earnedPoints = Math.floor(order.total * 0.1);
        
        const pointResponse = await fetch('/api/v1/points/accrual', {
          method: 'POST',
          headers: {
            'X-API-Key': process.env.REACT_APP_API_KEY,
            'Content-Type': 'application/json'
          },
          body: JSON.stringify({
            memberCardNo: member.cardNo,
            points: earnedPoints,
            orderId: order.orderId,
            reason: 'ORDER_PAYMENT',
            baseAmount: order.total
          })
        });

        if (pointResponse.ok) {
          const pointResult = await pointResponse.json();
          pointTransactionId = pointResult.transactionId;
        }
      }

      // 3. 決済完了
      const response = await fetch(`/api/v1/orders/${order.orderId}/pay`, {
        method: 'PUT',
        headers: {
          'X-API-Key': process.env.REACT_APP_API_KEY,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          paymentMethod: 'OTHER',
          memberCardNo: member?.cardNo || null,
          pointEarnTransactionId: pointTransactionId
        })
      });

      if (response.ok) {
        onPaymentComplete();
      } else {
        const errorData = await response.json();
        setError(errorData.message);
      }
    } catch (error) {
      setError(error.message);
    } finally {
      setLoading(false);
    }
  };

  const earnedPoints = member ? Math.floor(order.total * 0.1) : 0;

  return (
    <div className="other-payment">
      <h3>現金・カード支払い</h3>
      
      <div className="payment-summary">
        <p>支払金額: ¥{order.total}</p>
        {member && (
          <p className="point-earn">獲得ポイント: {earnedPoints}pt</p>
        )}
      </div>
      
      {error && (
        <div className="error-message">
          {error}
        </div>
      )}

      <button 
        onClick={processOtherPayment} 
        disabled={loading || order.status !== 'CONFIRMED'}
        className="payment-button"
      >
        {loading ? '決済中...' : '現金・カード決済'}
      </button>
    </div>
  );
};
```

### 3.1 会員管理

#### 3.1.1 会員登録

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
    {
      "field": "email",
      "error": "このメールアドレスは既に登録されています"
    }
  ]
}
```

**React実装例**:
```typescript
const UserRegistration: React.FC = () => {
  const [formData, setFormData] = useState({
    lastName: '',
    firstName: '',
    gender: '',
    email: '',
    password: '',
    confirmPassword: ''
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);
  const [registrationResult, setRegistrationResult] = useState<any>(null);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.lastName.trim()) newErrors.lastName = '姓は必須です';
    if (!formData.firstName.trim()) newErrors.firstName = '名は必須です';
    if (!formData.email.trim()) newErrors.email = 'メールアドレスは必須です';
    else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'メールアドレスの形式が正しくありません';
    }
    
    if (!formData.password) newErrors.password = 'パスワードは必須です';
    else if (formData.password.length < 8) {
      newErrors.password = 'パスワードは8文字以上で入力してください';
    }
    
    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'パスワードが一致しません';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) return;

    setLoading(true);
    try {
      const response = await fetch('/api/v1/users/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          lastName: formData.lastName,
          firstName: formData.firstName,
          gender: formData.gender || null,
          email: formData.email,
          password: formData.password
        })
      });

      if (response.ok) {
        const result = await response.json();
        setRegistrationResult(result);
      } else {
        const errorData = await response.json();
        const newErrors: Record<string, string> = {};
        errorData.details?.forEach((detail: any) => {
          newErrors[detail.field] = detail.error;
        });
        setErrors(newErrors);
      }
    } catch (error) {
      console.error('登録エラー:', error);
      setErrors({ general: '登録処理でエラーが発生しました' });
    } finally {
      setLoading(false);
    }
  };

  if (registrationResult) {
    return (
      <div className="registration-success">
        <h2>会員登録完了</h2>
        <div className="success-content">
          <p>会員登録が完了しました！</p>
          <div className="card-info">
            <h3>ポイントカード番号</h3>
            <div className="card-number">{registrationResult.cardNo}</div>
            <p>この番号を店舗でお伝えください</p>
          </div>
          <div className="user-info">
            <p>お名前: {registrationResult.lastName} {registrationResult.firstName}</p>
            <p>メールアドレス: {registrationResult.email}</p>
          </div>
          <button onClick={() => window.location.href = '/login'}>
            ログインページへ
          </button>
        </div>
      </div>
    );
  }

  return (
    <form onSubmit={handleSubmit} className="registration-form">
      <h2>会員登録</h2>
      
      {errors.general && (
        <div className="error-message">{errors.general}</div>
      )}

      <div className="form-group">
        <label>姓 *</label>
        <input
          type="text"
          value={formData.lastName}
          onChange={(e) => setFormData({...formData, lastName: e.target.value})}
          className={errors.lastName ? 'error' : ''}
        />
        {errors.lastName && <span className="field-error">{errors.lastName}</span>}
      </div>

      <div className="form-group">
        <label>名 *</label>
        <input
          type="text"
          value={formData.firstName}
          onChange={(e) => setFormData({...formData, firstName: e.target.value})}
          className={errors.firstName ? 'error' : ''}
        />
        {errors.firstName && <span className="field-error">{errors.firstName}</span>}
      </div>

      <div className="form-group">
        <label>性別</label>
        <select
          value={formData.gender}
          onChange={(e) => setFormData({...formData, gender: e.target.value})}
        >
          <option value="">選択しない</option>
          <option value="M">男性</option>
          <option value="F">女性</option>
        </select>
      </div>

      <div className="form-group">
        <label>メールアドレス *</label>
        <input
          type="email"
          value={formData.email}
          onChange={(e) => setFormData({...formData, email: e.target.value})}
          className={errors.email ? 'error' : ''}
        />
        {errors.email && <span className="field-error">{errors.email}</span>}
      </div>

      <div className="form-group">
        <label>パスワード *</label>
        <input
          type="password"
          value={formData.password}
          onChange={(e) => setFormData({...formData, password: e.target.value})}
          className={errors.password ? 'error' : ''}
        />
        {errors.password && <span className="field-error">{errors.password}</span>}
      </div>

      <div className="form-group">
        <label>パスワード確認 *</label>
        <input
          type="password"
          value={formData.confirmPassword}
          onChange={(e) => setFormData({...formData, confirmPassword: e.target.value})}
          className={errors.confirmPassword ? 'error' : ''}
        />
        {errors.confirmPassword && <span className="field-error">{errors.confirmPassword}</span>}
      </div>

      <button type="submit" disabled={loading} className="submit-button">
        {loading ? '登録中...' : '会員登録'}
      </button>
    </form>
  );
};
```

#### 3.1.2 ログイン・認証

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

**React実装例**:
```typescript
const Login: React.FC = () => {
  const [credentials, setCredentials] = useState({
    email: '',
    password: ''
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const response = await fetch('/api/v1/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(credentials)
      });

      if (response.ok) {
        const result = await response.json();
        
        // JWTトークンをlocalStorageに保存
        localStorage.setItem('authToken', result.token);
        localStorage.setItem('user', JSON.stringify(result.user));
        
        // マイページにリダイレクト
        window.location.href = '/mypage';
      } else {
        const errorData = await response.json();
        setError(errorData.message);
      }
    } catch (error) {
      console.error('ログインエラー:', error);
      setError('ログイン処理でエラーが発生しました');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-container">
      <form onSubmit={handleLogin} className="login-form">
        <h2>ログイン</h2>
        
        {error && (
          <div className="error-message">{error}</div>
        )}

        <div className="form-group">
          <label>メールアドレス</label>
          <input
            type="email"
            value={credentials.email}
            onChange={(e) => setCredentials({...credentials, email: e.target.value})}
            required
          />
        </div>

        <div className="form-group">
          <label>パスワード</label>
          <input
            type="password"
            value={credentials.password}
            onChange={(e) => setCredentials({...credentials, password: e.target.value})}
            required
          />
        </div>

        <button type="submit" disabled={loading} className="login-button">
          {loading ? 'ログイン中...' : 'ログイン'}
        </button>
        
        <div className="links">
          <a href="/register">会員登録はこちら</a>
        </div>
      </form>
    </div>
  );
};
```

#### 3.1.3 マイページ・ポイント確認

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

# Response - 会員情報
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

# Response - ポイント履歴
{
  "history": [
    {
      "ledgerId": 123,
      "type": "EARN",
      "points": 75,
      "orderId": 456,
      "occurredAt": "2024-01-15T14:30:00Z",
      "description": "お買い物でポイント獲得"
    },
    {
      "ledgerId": 122,
      "type": "USE",
      "points": 500,
      "orderId": 455,
      "occurredAt": "2024-01-14T16:00:00Z",
      "description": "ポイント使用"
    }
  ]
}
```

**React実装例**:
```typescript
const MyPage: React.FC = () => {
  const [user, setUser] = useState<User | null>(null);
  const [pointHistory, setPointHistory] = useState<PointTransaction[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchUserData();
    fetchPointHistory();
  }, []);

  const fetchUserData = async () => {
    try {
      const token = localStorage.getItem('authToken');
      const response = await fetch('/api/v1/users/me', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (response.ok) {
        const userData = await response.json();
        setUser(userData);
      } else if (response.status === 401) {
        // 認証エラー - ログインページにリダイレクト
        localStorage.removeItem('authToken');
        window.location.href = '/login';
      }
    } catch (error) {
      console.error('ユーザー情報取得エラー:', error);
    }
  };

  const fetchPointHistory = async () => {
    try {
      const token = localStorage.getItem('authToken');
      const response = await fetch('/api/v1/points/history?limit=10', {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      
      if (response.ok) {
        const historyData = await response.json();
        setPointHistory(historyData.history);
      }
    } catch (error) {
      console.error('ポイント履歴取得エラー:', error);
    } finally {
      setLoading(false);
    }
  };

  const formatTransactionType = (type: string) => {
    switch (type) {
      case 'EARN': return '獲得';
      case 'USE': return '使用';
      case 'EXPIRE': return '失効';
      case 'ADJUST': return '調整';
      default: return type;
    }
  };

  if (loading) {
    return <div>Loading...</div>;
  }

  if (!user) {
    return <div>ユーザー情報を取得できませんでした</div>;
  }

  return (
    <div className="mypage">
      <header className="mypage-header">
        <h1>マイページ</h1>
        <button onClick={() => {
          localStorage.removeItem('authToken');
          window.location.href = '/login';
        }}>
          ログアウト
        </button>
      </header>

      <div className="user-profile">
        <h2>プロフィール</h2>
        <div className="profile-info">
          <p><strong>お名前:</strong> {user.lastName} {user.firstName}</p>
          <p><strong>メールアドレス:</strong> {user.email}</p>
          <p><strong>ポイントカード番号:</strong> {user.cardNo}</p>
          <p><strong>会員登録日:</strong> {new Date(user.createdAt).toLocaleDateString()}</p>
        </div>
      </div>

      <div className="point-balance">
        <h2>ポイント残高</h2>
        <div className="balance-display">
          <span className="balance-amount">{user.pointBalance}</span>
          <span className="balance-unit">pt</span>
        </div>
      </div>

      <div className="point-history">
        <h2>ポイント履歴（最新10件）</h2>
        <table className="history-table">
          <thead>
            <tr>
              <th>日時</th>
              <th>種別</th>
              <th>ポイント</th>
              <th>詳細</th>
            </tr>
          </thead>
          <tbody>
            {pointHistory.map((transaction) => (
              <tr key={transaction.ledgerId}>
                <td>{new Date(transaction.occurredAt).toLocaleDateString()}</td>
                <td>
                  <span className={`transaction-type ${transaction.type.toLowerCase()}`}>
                    {formatTransactionType(transaction.type)}
                  </span>
                </td>
                <td className={transaction.type === 'EARN' ? 'positive' : 'negative'}>
                  {transaction.type === 'EARN' ? '+' : '-'}{Math.abs(transaction.points)}pt
                </td>
                <td>
                  {transaction.description}
                  {transaction.orderId && (
                    <span className="order-id">（注文#{transaction.orderId}）</span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="mypage-actions">
        <button onClick={() => window.location.href = '/profile/edit'}>
          プロフィール編集
        </button>
      </div>
    </div>
  );
};
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
- [ ] GET /api/v1/stocks - 在庫一覧取得
- [ ] POST /api/v1/stocks/transactions - 在庫取引記録
- [ ] POST /api/v1/stocks/availability-check - 在庫確認
- [ ] POST /api/v1/stocks/consumption - 在庫消費
- [ ] GET /api/v1/products - 商品一覧取得
- [ ] POST /api/v1/products - 商品登録

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
- [ ] 材料管理画面（一覧・登録・編集・削除）
- [ ] 在庫管理画面（一覧・入出庫・調整）
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
- [ ] Frontend Tests（React Component）
- [ ] End-to-End Tests（業務フロー）
- [ ] Performance Tests（負荷テスト）

### デプロイ・運用
- [ ] Docker設定
- [ ] 環境変数設定
- [ ] ログ設定
- [ ] モニタリング設定
- [ ] バックアップ設定