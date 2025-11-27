-- ==========================================
-- Coffee Shop システム - データベース作成スクリプト
-- PostgreSQL + Entity Framework Core 8.0
-- マイクロサービスアーキテクチャ対応
-- ==========================================

-- ===================
-- Product Service DB
-- ===================

-- 単位テーブル
CREATE TABLE unit (
    unit_id SERIAL PRIMARY KEY,
    unit_code VARCHAR(100) UNIQUE NOT NULL,
    unit_name VARCHAR(255) NOT NULL
);

-- 材料テーブル
CREATE TABLE material (
    material_id SERIAL PRIMARY KEY,
    material_name VARCHAR(255) UNIQUE NOT NULL,
    unit_id INTEGER NOT NULL REFERENCES unit(unit_id)
);

-- カテゴリテーブル
CREATE TABLE category (
    category_id SERIAL PRIMARY KEY,
    category_name VARCHAR(255) UNIQUE NOT NULL
);

-- レシピテーブル
CREATE TABLE recipe (
    recipe_id SERIAL PRIMARY KEY,
    recipe_name VARCHAR(255) NOT NULL,
    how_to TEXT
);

-- レシピ材料テーブル（中間テーブル）
CREATE TABLE recipe_ingredients (
    recipe_id INTEGER NOT NULL REFERENCES recipe(recipe_id),
    material_id INTEGER NOT NULL REFERENCES material(material_id),
    quantity NUMERIC(12,3) NOT NULL,
    PRIMARY KEY (recipe_id, material_id)
);

-- 商品テーブル
CREATE TABLE product (
    product_id SERIAL PRIMARY KEY,
    product_name VARCHAR(255) NOT NULL,
    recipe_id INTEGER REFERENCES recipe(recipe_id),
    price NUMERIC(10,2) NOT NULL,
    is_campaign BOOLEAN DEFAULT FALSE,
    campaign_discount_percent NUMERIC(5,2) DEFAULT 0,
    category_id INTEGER REFERENCES category(category_id)
);

-- 在庫履歴テーブル
CREATE TABLE stock_transactions (
    tx_id SERIAL PRIMARY KEY,
    material_id INTEGER NOT NULL REFERENCES material(material_id),
    tx_type VARCHAR(10) NOT NULL CHECK (tx_type IN ('IN', 'OUT', 'ADJ')),
    quantity NUMERIC(12,3) NOT NULL,
    reason TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    order_id INTEGER -- Order Serviceへの参照（外部キー制約なし）
);

-- ===================
-- Order Service DB
-- ===================

-- 注文テーブル
CREATE TABLE "order" (
    order_id SERIAL PRIMARY KEY,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    member_card_no VARCHAR(20), -- nullable
    total NUMERIC(10,2) NOT NULL,
    status VARCHAR(16) NOT NULL CHECK (status IN ('IN_ORDER', 'CONFIRMED', 'PAID'))
);

-- 注文明細テーブル
CREATE TABLE order_item (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL REFERENCES "order"(order_id),
    product_id INTEGER NOT NULL, -- Product Serviceへの参照（外部キー制約なし）
    product_name VARCHAR(255) NOT NULL, -- スナップショット保存
    product_price NUMERIC(10,2) NOT NULL, -- スナップショット保存
    product_discount_percent NUMERIC(5,2) DEFAULT 0, -- スナップショット保存
    quantity INTEGER NOT NULL
);

-- ===================
-- User Service DB
-- ===================

-- 会員テーブル
CREATE TABLE "user" (
    user_id SERIAL PRIMARY KEY,
    last_name VARCHAR(255) NOT NULL,
    first_name VARCHAR(255) NOT NULL,
    gender CHAR(1) CHECK (gender IN ('M', 'F')), -- nullable
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    card_no VARCHAR(20) UNIQUE NOT NULL, -- ランダム英数12桁
    is_deleted BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- ポイント台帳テーブル
CREATE TABLE point_ledger (
    ledger_id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES "user"(user_id),
    order_id INTEGER, -- Order Serviceへの参照（外部キー制約なし）
    type VARCHAR(20) NOT NULL CHECK (type IN ('EARN', 'USE', 'EXPIRE', 'ADJUST')),
    points INTEGER NOT NULL,
    occurred_at TIMESTAMPTZ DEFAULT NOW()
);

-- ==========================================
-- インデックス作成
-- ==========================================

-- Product Service
CREATE INDEX idx_material_unit_id ON material(unit_id);
CREATE INDEX idx_recipe_ingredients_recipe_id ON recipe_ingredients(recipe_id);
CREATE INDEX idx_recipe_ingredients_material_id ON recipe_ingredients(material_id);
CREATE INDEX idx_product_recipe_id ON product(recipe_id);
CREATE INDEX idx_product_category_id ON product(category_id);
CREATE INDEX idx_stock_transactions_material_id ON stock_transactions(material_id);
CREATE INDEX idx_stock_transactions_created_at ON stock_transactions(created_at);
CREATE INDEX idx_stock_transactions_order_id ON stock_transactions(order_id);

-- Order Service
CREATE INDEX idx_order_created_at ON "order"(created_at);
CREATE INDEX idx_order_member_card_no ON "order"(member_card_no);
CREATE INDEX idx_order_status ON "order"(status);
CREATE INDEX idx_order_item_order_id ON order_item(order_id);
CREATE INDEX idx_order_item_product_id ON order_item(product_id);

-- User Service
CREATE INDEX idx_point_ledger_user_id ON point_ledger(user_id);
CREATE INDEX idx_point_ledger_order_id ON point_ledger(order_id);
CREATE INDEX idx_point_ledger_occurred_at ON point_ledger(occurred_at);
CREATE INDEX idx_point_ledger_type ON point_ledger(type);

-- ==========================================
-- 初期データ投入
-- ==========================================

-- 単位マスタ
INSERT INTO unit (unit_code, unit_name) VALUES 
('G', 'グラム'),
('ML', 'ミリリットル'),
('PCS', '個');

-- カテゴリマスタ
INSERT INTO category (category_name) VALUES 
('エスプレッソ系'),
('フラペチーノ'),
('ティー');

-- 材料マスタ
INSERT INTO material (material_name, unit_id) VALUES 
('コーヒー豆', 1),
('ミルク', 2),
('砂糖', 1);

-- レシピマスタ
INSERT INTO recipe (recipe_name, how_to) VALUES 
('エスプレッソ', 'コーヒー豆20gを抽出'),
('ラテ', 'エスプレッソにスチームミルクを加える');

-- レシピ材料
INSERT INTO recipe_ingredients (recipe_id, material_id, quantity) VALUES 
(1, 1, 20.000),
(2, 1, 20.000),
(2, 2, 150.000);

-- 商品マスタ
INSERT INTO product (product_name, recipe_id, price, category_id) VALUES 
('エスプレッソ', 1, 300.00, 1),
('カフェラテ', 2, 450.00, 1);