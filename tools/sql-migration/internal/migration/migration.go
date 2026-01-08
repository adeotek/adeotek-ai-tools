package migration

import (
	"crypto/md5"
	"fmt"
	"io/ioutil"
	"os"
	"path/filepath"
	"sort"
	"strings"

	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/database"
	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/models"
)

// Manager handles database migrations
type Manager struct {
	db        *database.Database
	config    models.MigrationConfig
	tableName string
}

// New creates a new migration manager
func New(db *database.Database, config models.MigrationConfig) *Manager {
	tableName := config.TableName
	if tableName == "" {
		tableName = "schema_migrations"
	}

	return &Manager{
		db:        db,
		config:    config,
		tableName: tableName,
	}
}

// Initialize creates the migrations tracking table if it doesn't exist
func (m *Manager) Initialize() error {
	query := fmt.Sprintf(`
		CREATE TABLE IF NOT EXISTS %s (
			id SERIAL PRIMARY KEY,
			version VARCHAR(255) NOT NULL UNIQUE,
			description TEXT,
			applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
			checksum VARCHAR(32) NOT NULL
		)
	`, m.tableName)

	_, err := m.db.Exec(query)
	if err != nil {
		return fmt.Errorf("failed to create migrations table: %w", err)
	}

	return nil
}

// GetAppliedMigrations returns a list of all applied migrations
func (m *Manager) GetAppliedMigrations() ([]models.MigrationRecord, error) {
	query := fmt.Sprintf(`
		SELECT id, version, description, applied_at, checksum
		FROM %s
		ORDER BY version ASC
	`, m.tableName)

	rows, err := m.db.Query(query)
	if err != nil {
		return nil, fmt.Errorf("failed to query applied migrations: %w", err)
	}
	defer rows.Close()

	var migrations []models.MigrationRecord
	for rows.Next() {
		var migration models.MigrationRecord
		err := rows.Scan(
			&migration.ID,
			&migration.Version,
			&migration.Description,
			&migration.AppliedAt,
			&migration.Checksum,
		)
		if err != nil {
			return nil, fmt.Errorf("failed to scan migration record: %w", err)
		}
		migrations = append(migrations, migration)
	}

	return migrations, nil
}

// GetPendingMigrations returns migration scripts that haven't been applied yet
func (m *Manager) GetPendingMigrations() ([]models.MigrationScript, error) {
	// Read all migration scripts from the scripts path
	allScripts, err := m.readMigrationScripts()
	if err != nil {
		return nil, err
	}

	// Get already applied migrations
	applied, err := m.GetAppliedMigrations()
	if err != nil {
		return nil, err
	}

	// Build a map of applied versions
	appliedMap := make(map[string]bool)
	for _, migration := range applied {
		appliedMap[migration.Version] = true
	}

	// Filter out applied migrations
	var pending []models.MigrationScript
	for _, script := range allScripts {
		if !appliedMap[script.Version] {
			pending = append(pending, script)
		}
	}

	return pending, nil
}

// ApplyMigration applies a single migration script
func (m *Manager) ApplyMigration(script models.MigrationScript) error {
	// Start a transaction
	tx, err := m.db.GetDB().Begin()
	if err != nil {
		return fmt.Errorf("failed to begin transaction: %w", err)
	}
	defer tx.Rollback()

	// Execute the migration script
	_, err = tx.Exec(script.Content)
	if err != nil {
		return fmt.Errorf("failed to execute migration %s: %w", script.Version, err)
	}

	// Record the migration
	query := fmt.Sprintf(`
		INSERT INTO %s (version, description, checksum)
		VALUES ($1, $2, $3)
	`, m.tableName)

	_, err = tx.Exec(query, script.Version, script.Description, script.Checksum)
	if err != nil {
		return fmt.Errorf("failed to record migration %s: %w", script.Version, err)
	}

	// Commit the transaction
	if err := tx.Commit(); err != nil {
		return fmt.Errorf("failed to commit migration %s: %w", script.Version, err)
	}

	return nil
}

// readMigrationScripts reads all migration scripts from the configured directory
func (m *Manager) readMigrationScripts() ([]models.MigrationScript, error) {
	if m.config.ScriptsPath == "" {
		return nil, fmt.Errorf("migration scripts path not configured")
	}

	// Check if directory exists
	if _, err := os.Stat(m.config.ScriptsPath); os.IsNotExist(err) {
		return nil, fmt.Errorf("migration scripts directory does not exist: %s", m.config.ScriptsPath)
	}

	// Read all .sql files
	files, err := filepath.Glob(filepath.Join(m.config.ScriptsPath, "*.sql"))
	if err != nil {
		return nil, fmt.Errorf("failed to read migration scripts: %w", err)
	}

	var scripts []models.MigrationScript
	for _, file := range files {
		script, err := m.readScript(file)
		if err != nil {
			return nil, fmt.Errorf("failed to read script %s: %w", file, err)
		}
		scripts = append(scripts, script)
	}

	// Sort scripts by version
	sort.Slice(scripts, func(i, j int) bool {
		return scripts[i].Version < scripts[j].Version
	})

	return scripts, nil
}

// readScript reads a single migration script file
func (m *Manager) readScript(filename string) (models.MigrationScript, error) {
	content, err := ioutil.ReadFile(filename)
	if err != nil {
		return models.MigrationScript{}, err
	}

	// Parse version and description from filename
	// Expected format: V001__initial_schema.sql or 001_initial_schema.sql
	basename := filepath.Base(filename)
	version, description := parseFilename(basename)

	// Calculate checksum
	checksum := fmt.Sprintf("%x", md5.Sum(content))

	return models.MigrationScript{
		Version:     version,
		Description: description,
		Filename:    basename,
		Content:     string(content),
		Checksum:    checksum,
	}, nil
}

// parseFilename extracts version and description from migration filename
func parseFilename(filename string) (version, description string) {
	// Remove .sql extension
	name := strings.TrimSuffix(filename, ".sql")

	// Split on __ or _
	var parts []string
	if strings.Contains(name, "__") {
		parts = strings.SplitN(name, "__", 2)
	} else {
		parts = strings.SplitN(name, "_", 2)
	}

	if len(parts) >= 1 {
		version = strings.TrimPrefix(parts[0], "V")
		version = strings.TrimPrefix(version, "v")
	}

	if len(parts) >= 2 {
		description = strings.ReplaceAll(parts[1], "_", " ")
	}

	return version, description
}

// HasPendingMigrations checks if there are any unapplied migrations
func (m *Manager) HasPendingMigrations() (bool, error) {
	pending, err := m.GetPendingMigrations()
	if err != nil {
		return false, err
	}
	return len(pending) > 0, nil
}
