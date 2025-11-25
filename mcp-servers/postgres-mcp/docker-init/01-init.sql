-- PostgreSQL MCP Server Test Database Initialization
-- This script creates sample tables for testing the MCP server

-- Create customers table
CREATE TABLE IF NOT EXISTS customers (
    customer_id SERIAL PRIMARY KEY,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    phone VARCHAR(20),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE customers IS 'Customer information table';
COMMENT ON COLUMN customers.customer_id IS 'Unique customer identifier';
COMMENT ON COLUMN customers.email IS 'Customer email address (unique)';

-- Create products table
CREATE TABLE IF NOT EXISTS products (
    product_id SERIAL PRIMARY KEY,
    product_name VARCHAR(200) NOT NULL,
    description TEXT,
    category VARCHAR(100),
    price DECIMAL(10, 2) NOT NULL,
    stock_quantity INTEGER DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE products IS 'Product catalog table';
COMMENT ON COLUMN products.price IS 'Product price in USD';

-- Create orders table
CREATE TABLE IF NOT EXISTS orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INTEGER NOT NULL,
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(50) DEFAULT 'pending',
    total_amount DECIMAL(10, 2),
    shipping_address TEXT,
    CONSTRAINT fk_customer FOREIGN KEY (customer_id) REFERENCES customers(customer_id)
);

COMMENT ON TABLE orders IS 'Customer orders table';
COMMENT ON COLUMN orders.status IS 'Order status: pending, processing, shipped, delivered';

-- Create order_items table
CREATE TABLE IF NOT EXISTS order_items (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    quantity INTEGER NOT NULL,
    unit_price DECIMAL(10, 2) NOT NULL,
    CONSTRAINT fk_order FOREIGN KEY (order_id) REFERENCES orders(order_id),
    CONSTRAINT fk_product FOREIGN KEY (product_id) REFERENCES products(product_id)
);

COMMENT ON TABLE order_items IS 'Individual items in customer orders';

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_customers_email ON customers(email);
CREATE INDEX IF NOT EXISTS idx_orders_customer ON orders(customer_id);
CREATE INDEX IF NOT EXISTS idx_orders_date ON orders(order_date);
CREATE INDEX IF NOT EXISTS idx_order_items_order ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_product ON order_items(product_id);
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category);

-- Insert sample data
INSERT INTO customers (first_name, last_name, email, phone) VALUES
    ('John', 'Doe', 'john.doe@example.com', '555-0001'),
    ('Jane', 'Smith', 'jane.smith@example.com', '555-0002'),
    ('Bob', 'Johnson', 'bob.johnson@example.com', '555-0003'),
    ('Alice', 'Williams', 'alice.williams@example.com', '555-0004'),
    ('Charlie', 'Brown', 'charlie.brown@example.com', '555-0005')
ON CONFLICT (email) DO NOTHING;

INSERT INTO products (product_name, description, category, price, stock_quantity) VALUES
    ('Laptop Pro 15', 'High-performance laptop', 'Electronics', 1299.99, 50),
    ('Wireless Mouse', 'Ergonomic wireless mouse', 'Electronics', 29.99, 200),
    ('USB-C Cable', 'USB-C to USB-C cable, 2m', 'Accessories', 12.99, 500),
    ('Monitor 27"', '4K UHD monitor', 'Electronics', 449.99, 75),
    ('Keyboard Mechanical', 'RGB mechanical keyboard', 'Electronics', 89.99, 150),
    ('Desk Lamp LED', 'Adjustable LED desk lamp', 'Furniture', 39.99, 100),
    ('Office Chair', 'Ergonomic office chair', 'Furniture', 299.99, 40),
    ('Notebook Set', 'Set of 3 notebooks', 'Stationery', 15.99, 300)
ON CONFLICT DO NOTHING;

INSERT INTO orders (customer_id, order_date, status, total_amount, shipping_address)
SELECT
    c.customer_id,
    CURRENT_TIMESTAMP - (random() * interval '30 days'),
    CASE (random() * 3)::int
        WHEN 0 THEN 'pending'
        WHEN 1 THEN 'processing'
        WHEN 2 THEN 'shipped'
        ELSE 'delivered'
    END,
    (random() * 1000 + 50)::numeric(10,2),
    '123 Main St, City, State ' || (10000 + (random() * 90000)::int)
FROM customers c
WHERE NOT EXISTS (SELECT 1 FROM orders WHERE customer_id = c.customer_id)
LIMIT 5;

-- Grant SELECT-only privileges to ensure read-only access
-- (This is for demonstration - the MCP server validates queries programmatically)
-- ALTER DEFAULT PRIVILEGES REVOKE ALL ON TABLES FROM PUBLIC;
-- GRANT SELECT ON ALL TABLES IN SCHEMA public TO postgres;

VACUUM ANALYZE;
