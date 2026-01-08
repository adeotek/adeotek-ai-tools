package database

import (
	"database/sql"
	"fmt"

	_ "github.com/lib/pq"                   // PostgreSQL driver
	_ "github.com/microsoft/go-mssqldb"     // SQL Server driver
	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/models"
)

// Database represents a database connection
type Database struct {
	db     *sql.DB
	config models.DatabaseConfig
}

// New creates a new database connection
func New(config models.DatabaseConfig) (*Database, error) {
	connStr, err := buildConnectionString(config)
	if err != nil {
		return nil, fmt.Errorf("failed to build connection string: %w", err)
	}

	db, err := sql.Open(config.Type, connStr)
	if err != nil {
		return nil, fmt.Errorf("failed to open database: %w", err)
	}

	if err := db.Ping(); err != nil {
		return nil, fmt.Errorf("failed to ping database: %w", err)
	}

	return &Database{
		db:     db,
		config: config,
	}, nil
}

// Close closes the database connection
func (d *Database) Close() error {
	if d.db != nil {
		return d.db.Close()
	}
	return nil
}

// GetDB returns the underlying sql.DB instance
func (d *Database) GetDB() *sql.DB {
	return d.db
}

// GetConfig returns the database configuration
func (d *Database) GetConfig() models.DatabaseConfig {
	return d.config
}

// Exec executes a SQL statement
func (d *Database) Exec(query string, args ...interface{}) (sql.Result, error) {
	return d.db.Exec(query, args...)
}

// Query executes a SQL query
func (d *Database) Query(query string, args ...interface{}) (*sql.Rows, error) {
	return d.db.Query(query, args...)
}

// QueryRow executes a SQL query that returns at most one row
func (d *Database) QueryRow(query string, args ...interface{}) *sql.Row {
	return d.db.QueryRow(query, args...)
}

// buildConnectionString builds a connection string based on database type
func buildConnectionString(config models.DatabaseConfig) (string, error) {
	switch config.Type {
	case "postgres":
		sslMode := config.SSLMode
		if sslMode == "" {
			sslMode = "disable"
		}
		return fmt.Sprintf(
			"host=%s port=%d user=%s password=%s dbname=%s sslmode=%s",
			config.Host, config.Port, config.User, config.Password, config.Database, sslMode,
		), nil
	case "mssql", "sqlserver":
		return fmt.Sprintf(
			"server=%s;port=%d;database=%s;user id=%s;password=%s;",
			config.Host, config.Port, config.Database, config.User, config.Password,
		), nil
	default:
		return "", fmt.Errorf("unsupported database type: %s", config.Type)
	}
}
