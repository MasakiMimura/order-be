-- ==========================================
-- Coffee Shop システム - データベース作成スクリプト
-- DBMS: PostgreSQL
-- ORM/Framework: Entity Framework (.NET)
-- アーキテクチャ: マイクロサービス
-- 生成元: docs/requirements-specification.md
-- ==========================================

-- ===================
-- Product Service DB
-- ===================

-- 単位マスタ
CREATE TABLE unit (
    unit_id SERIAL PRIMARY KEY,
    unit_code VARCHAR(50) UNIQUE NOT NULL,
    unit_name VARCHAR(255) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- 材料マスタ
CREATE TABLE material (
    material_id SERIAL PRIMARY KEY,
    material_name VARCHAR(255) UNIQUE NOT NULL,
    unit_id INTEGER NOT NULL REFERENCES unit(unit_id),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- カテゴリマスタ
CREATE TABLE category (
    category_id SERIAL PRIMARY KEY,
    category_name VARCHAR(255) UNIQUE NOT NULL,
    display_order INTEGER DEFAULT 0 NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- レシピ
CREATE TABLE recipe (
    recipe_id SERIAL PRIMARY KEY,
    recipe_name VARCHAR(255) NOT NULL,
    how_to TEXT, -- nullable
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- レシピ配合（多対多中間テーブル）
CREATE TABLE recipe_ingredient (
    recipe_id INTEGER NOT NULL REFERENCES recipe(recipe_id),
    material_id INTEGER NOT NULL REFERENCES material(material_id),
    quantity DECIMAL(10, 3) NOT NULL CHECK (quantity > 0),
    unit_id INTEGER NOT NULL REFERENCES unit(unit_id),
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    PRIMARY KEY (recipe_id, material_id)
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

-- 在庫トランザクション（履歴管理）
CREATE TABLE stock_transaction (
    stock_transaction_id BIGSERIAL PRIMARY KEY,
    material_id INTEGER NOT NULL REFERENCES material(material_id),
    tx_type VARCHAR(20) NOT NULL CHECK (tx_type IN ('IN', 'OUT', 'ADJUST')),
    quantity DECIMAL(10, 3) NOT NULL,
    reason VARCHAR(255), -- nullable
    order_id INTEGER, -- nullable, [Order Service]への参照（外部キー制約なし）
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    created_by VARCHAR(255) -- nullable
);

-- ===================
-- Order Service DB
-- ===================

-- 注文
CREATE TABLE "order" (
    order_id SERIAL PRIMARY KEY,
    order_status VARCHAR(20) NOT NULL CHECK (order_status IN ('IN_ORDER', 'CONFIRMED', 'PAID')),
    card_no VARCHAR(20), -- nullable, [User Service]への参照（外部キー制約なし）
    payment_method VARCHAR(20), -- nullable, CHECK (payment_method IN ('POINT', 'OTHER'))
    total_amount INTEGER DEFAULT 0 NOT NULL,
    discount_amount INTEGER DEFAULT 0 NOT NULL,
    final_amount INTEGER DEFAULT 0 NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    paid_at TIMESTAMPTZ -- nullable
);

-- 注文明細（商品スナップショット保存）
CREATE TABLE order_item (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL REFERENCES "order"(order_id),
    product_id INTEGER NOT NULL, -- [Product Service]への参照（外部キー制約なし）
    product_name VARCHAR(255) NOT NULL, -- スナップショット保存
    unit_price INTEGER NOT NULL, -- スナップショット保存
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    discount_percent INTEGER DEFAULT 0 CHECK (discount_percent >= 0 AND discount_percent <= 100), -- スナップショット保存
    subtotal INTEGER NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- ===================
-- User Service DB
-- ===================

-- 会員
CREATE TABLE "user" (
    user_id SERIAL PRIMARY KEY,
    card_no VARCHAR(20) UNIQUE NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    gender VARCHAR(10), -- nullable
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL,
    deleted_at TIMESTAMPTZ -- nullable
);

-- ポイント台帳
CREATE TABLE point_ledger (
    point_ledger_id BIGSERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES "user"(user_id),
    tx_type VARCHAR(20) NOT NULL CHECK (tx_type IN ('ACCRUAL', 'REDEEM')),
    points INTEGER NOT NULL,
    order_id INTEGER, -- nullable, [Order Service]への参照（外部キー制約なし）
    description VARCHAR(255), -- nullable
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP NOT NULL
);

-- ==========================================
-- インデックス作成
-- ==========================================

-- Product Service
CREATE INDEX idx_material_unit_id ON material(unit_id);
CREATE INDEX idx_recipe_ingredient_recipe_id ON recipe_ingredient(recipe_id);
CREATE INDEX idx_recipe_ingredient_material_id ON recipe_ingredient(material_id);
CREATE INDEX idx_product_recipe_id ON product(recipe_id);
CREATE INDEX idx_product_category_id ON product(category_id);
CREATE INDEX idx_product_is_active ON product(is_active);
CREATE INDEX idx_stock_transaction_material_id ON stock_transaction(material_id);
CREATE INDEX idx_stock_transaction_order_id ON stock_transaction(order_id);
CREATE INDEX idx_stock_transaction_created_at ON stock_transaction(created_at);

-- Order Service
CREATE INDEX idx_order_order_status ON "order"(order_status);
CREATE INDEX idx_order_card_no ON "order"(card_no);
CREATE INDEX idx_order_created_at ON "order"(created_at);
CREATE INDEX idx_order_item_order_id ON order_item(order_id);
CREATE INDEX idx_order_item_product_id ON order_item(product_id);

-- User Service
CREATE INDEX idx_user_email ON "user"(email);
CREATE INDEX idx_user_is_deleted ON "user"(is_deleted);
CREATE INDEX idx_point_ledger_user_id ON point_ledger(user_id);
CREATE INDEX idx_point_ledger_order_id ON point_ledger(order_id);
CREATE INDEX idx_point_ledger_created_at ON point_ledger(created_at);

-- ==========================================
-- 初期データ投入
-- ==========================================

-- 単位マスタ
INSERT INTO unit (unit_code, unit_name) VALUES
('g', 'グラム'),
('kg', 'キログラム'),
('ml', 'ミリリットル'),
('l', 'リットル'),
('pcs', '個'),
('sheet', '枚');

-- カテゴリマスタ
INSERT INTO category (category_name, display_order) VALUES
('ドリンク', 1),
('フード', 2),
('デザート', 3);
