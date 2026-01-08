package models

import "time"

// Config holds the application configuration
type Config struct {
	Database DatabaseConfig
	Migration MigrationConfig
	Backup   BackupConfig
}

// DatabaseConfig holds database connection settings
type DatabaseConfig struct {
	Type     string // "postgres" or "mssql"
	Host     string
	Port     int
	Database string
	User     string
	Password string
	SSLMode  string // for postgres
}

// MigrationConfig holds migration-specific settings
type MigrationConfig struct {
	ScriptsPath string // path to migration scripts directory
	TableName   string // name of the migrations tracking table
}

// BackupConfig holds backup-specific settings
type BackupConfig struct {
	BackupPath string // path where backups are stored
}

// MigrationRecord represents a migration that has been applied
type MigrationRecord struct {
	ID          int
	Version     string
	Description string
	AppliedAt   time.Time
	Checksum    string
}

// MigrationScript represents a migration script file
type MigrationScript struct {
	Version     string
	Description string
	Filename    string
	Content     string
	Checksum    string
}

// BackupMetadata represents information about a database backup
type BackupMetadata struct {
	Filename   string
	CreatedAt  time.Time
	DatabaseName string
	Size       int64
}
