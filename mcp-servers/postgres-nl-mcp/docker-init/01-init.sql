-- Sample database schema for testing PostgreSQL MCP Server
-- This script creates a simple e-commerce database

-- Create customers table
CREATE TABLE customers (
    customer_id SERIAL PRIMARY KEY,
    first_name VARCHAR(50) NOT NULL,
    last_name VARCHAR(50) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    phone VARCHAR(20),
    address TEXT,
    city VARCHAR(50),
    country VARCHAR(50),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE customers IS 'Stores customer information';
COMMENT ON COLUMN customers.customer_id IS 'Unique customer identifier';
COMMENT ON COLUMN customers.email IS 'Customer email address (unique)';

-- Create products table
CREATE TABLE products (
    product_id SERIAL PRIMARY KEY,
    product_name VARCHAR(100) NOT NULL,
    description TEXT,
    category VARCHAR(50),
    price DECIMAL(10, 2) NOT NULL CHECK (price >= 0),
    stock_quantity INTEGER NOT NULL DEFAULT 0 CHECK (stock_quantity >= 0),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE products IS 'Product catalog';
COMMENT ON COLUMN products.price IS 'Product price in USD';
COMMENT ON COLUMN products.stock_quantity IS 'Available quantity in stock';

-- Create orders table
CREATE TABLE orders (
    order_id SERIAL PRIMARY KEY,
    customer_id INTEGER NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE,
    order_date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'shipped', 'delivered', 'cancelled')),
    total_amount DECIMAL(10, 2) NOT NULL CHECK (total_amount >= 0),
    shipping_address TEXT,
    notes TEXT
);

COMMENT ON TABLE orders IS 'Customer orders';
COMMENT ON COLUMN orders.status IS 'Order status: pending, processing, shipped, delivered, cancelled';

-- Create order_items table
CREATE TABLE order_items (
    order_item_id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    product_id INTEGER NOT NULL REFERENCES products(product_id) ON DELETE RESTRICT,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    unit_price DECIMAL(10, 2) NOT NULL CHECK (unit_price >= 0),
    subtotal DECIMAL(10, 2) NOT NULL CHECK (subtotal >= 0)
);

COMMENT ON TABLE order_items IS 'Line items for each order';

-- Create indexes for better query performance
CREATE INDEX idx_customers_email ON customers(email);
CREATE INDEX idx_customers_created_at ON customers(created_at);
CREATE INDEX idx_products_category ON products(category);
CREATE INDEX idx_products_price ON products(price);
CREATE INDEX idx_orders_customer_id ON orders(customer_id);
CREATE INDEX idx_orders_order_date ON orders(order_date);
CREATE INDEX idx_orders_status ON orders(status);
CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);

-- Insert sample data

-- Sample customers
INSERT INTO customers (first_name, last_name, email, phone, address, city, country) VALUES
('John', 'Doe', 'john.doe@example.com', '+1-555-0101', '123 Main St', 'New York', 'USA'),
('Jane', 'Smith', 'jane.smith@example.com', '+1-555-0102', '456 Oak Ave', 'Los Angeles', 'USA'),
('Bob', 'Johnson', 'bob.johnson@example.com', '+1-555-0103', '789 Pine Rd', 'Chicago', 'USA'),
('Alice', 'Williams', 'alice.williams@example.com', '+1-555-0104', '321 Elm St', 'Houston', 'USA'),
('Charlie', 'Brown', 'charlie.brown@example.com', '+1-555-0105', '654 Maple Dr', 'Phoenix', 'USA');

-- Sample products
INSERT INTO products (product_name, description, category, price, stock_quantity) VALUES
('Laptop Pro 15', 'High-performance laptop with 15-inch display', 'Electronics', 1299.99, 50),
('Wireless Mouse', 'Ergonomic wireless mouse with USB receiver', 'Electronics', 29.99, 200),
('Office Chair', 'Comfortable ergonomic office chair', 'Furniture', 249.99, 30),
('Desk Lamp', 'LED desk lamp with adjustable brightness', 'Furniture', 39.99, 100),
('USB-C Hub', '7-in-1 USB-C hub with HDMI and card reader', 'Electronics', 49.99, 150),
('Notebook Set', 'Set of 3 premium notebooks', 'Stationery', 19.99, 500),
('Mechanical Keyboard', 'RGB mechanical keyboard with blue switches', 'Electronics', 129.99, 75),
('Monitor Stand', 'Adjustable monitor stand with storage', 'Furniture', 59.99, 60),
('Webcam HD', '1080p HD webcam with built-in microphone', 'Electronics', 79.99, 120),
('Backpack', 'Professional laptop backpack with USB charging port', 'Accessories', 69.99, 80);

-- Sample orders
INSERT INTO orders (customer_id, order_date, status, total_amount, shipping_address) VALUES
(1, CURRENT_TIMESTAMP - INTERVAL '10 days', 'delivered', 1329.98, '123 Main St, New York, USA'),
(2, CURRENT_TIMESTAMP - INTERVAL '8 days', 'delivered', 329.97, '456 Oak Ave, Los Angeles, USA'),
(3, CURRENT_TIMESTAMP - INTERVAL '5 days', 'shipped', 79.98, '789 Pine Rd, Chicago, USA'),
(1, CURRENT_TIMESTAMP - INTERVAL '3 days', 'processing', 249.99, '123 Main St, New York, USA'),
(4, CURRENT_TIMESTAMP - INTERVAL '2 days', 'pending', 159.98, '321 Elm St, Houston, USA'),
(5, CURRENT_TIMESTAMP - INTERVAL '1 day', 'pending', 199.98, '654 Maple Dr, Phoenix, USA');

-- Sample order items
INSERT INTO order_items (order_id, product_id, quantity, unit_price, subtotal) VALUES
-- Order 1
(1, 1, 1, 1299.99, 1299.99),
(1, 2, 1, 29.99, 29.99),
-- Order 2
(2, 7, 1, 129.99, 129.99),
(2, 3, 1, 249.99, 249.99),
-- Order 3
(3, 6, 2, 19.99, 39.98),
(3, 4, 1, 39.99, 39.99),
-- Order 4
(4, 3, 1, 249.99, 249.99),
-- Order 5
(5, 5, 1, 49.99, 49.99),
(5, 9, 1, 79.99, 79.99),
(5, 2, 1, 29.99, 29.99),
-- Order 6
(6, 10, 1, 69.99, 69.99),
(6, 8, 1, 59.99, 59.99),
(6, 4, 1, 39.99, 39.99);

-- Create a view for order summary
CREATE VIEW order_summary AS
SELECT
    o.order_id,
    o.order_date,
    o.status,
    c.first_name || ' ' || c.last_name AS customer_name,
    c.email AS customer_email,
    COUNT(oi.order_item_id) AS item_count,
    o.total_amount
FROM orders o
JOIN customers c ON o.customer_id = c.customer_id
LEFT JOIN order_items oi ON o.order_id = oi.order_id
GROUP BY o.order_id, o.order_date, o.status, c.first_name, c.last_name, c.email, o.total_amount;

COMMENT ON VIEW order_summary IS 'Aggregated view of orders with customer information';

-- Grant permissions (if needed)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO your_user;
-- GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO your_user;

-- Display summary
DO $$
BEGIN
    RAISE NOTICE 'Database initialization complete!';
    RAISE NOTICE 'Created tables: customers, products, orders, order_items';
    RAISE NOTICE 'Created view: order_summary';
    RAISE NOTICE 'Inserted sample data for testing';
END $$;
