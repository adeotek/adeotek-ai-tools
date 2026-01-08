package main

import (
	"flag"
	"fmt"
	"log"
	"os"

	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/backup"
	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/database"
	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/migration"
	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/models"
)

const (
	Version = "1.0.0"
)

func main() {
	// Define command-line flags
	var (
		dbType      = flag.String("db-type", getEnvOrDefault("DB_TYPE", "postgres"), "Database type (postgres or mssql)")
		dbHost      = flag.String("db-host", getEnvOrDefault("DB_HOST", "localhost"), "Database host")
		dbPort      = flag.Int("db-port", getEnvOrDefaultInt("DB_PORT", 5432), "Database port")
		dbName      = flag.String("db-name", getEnvOrDefault("DB_NAME", ""), "Database name")
		dbUser      = flag.String("db-user", getEnvOrDefault("DB_USER", ""), "Database user")
		dbPassword  = flag.String("db-password", getEnvOrDefault("DB_PASSWORD", ""), "Database password")
		dbSSLMode   = flag.String("db-sslmode", getEnvOrDefault("DB_SSLMODE", "disable"), "PostgreSQL SSL mode")
		scriptsPath = flag.String("scripts-path", getEnvOrDefault("MIGRATION_SCRIPTS_PATH", "./migrations"), "Path to migration scripts")
		backupPath  = flag.String("backup-path", getEnvOrDefault("BACKUP_PATH", "./backups"), "Path to store backups")
		tableName   = flag.String("table-name", getEnvOrDefault("MIGRATION_TABLE", "schema_migrations"), "Name of migrations tracking table")
		doBackup    = flag.Bool("backup", false, "Create backup before applying migrations (only if there are unapplied scripts)")
		doRestore   = flag.Bool("restore", false, "Restore from last backup (will not run migrations)")
		showVersion = flag.Bool("version", false, "Show version information")
		listBackups = flag.Bool("list-backups", false, "List available backups")
	)

	flag.Parse()

	// Show version and exit
	if *showVersion {
		fmt.Printf("sql-migration version %s\n", Version)
		os.Exit(0)
	}

	// Validate required flags
	if *dbName == "" {
		log.Fatal("Error: database name is required (use -db-name or DB_NAME environment variable)")
	}
	if *dbUser == "" {
		log.Fatal("Error: database user is required (use -db-user or DB_USER environment variable)")
	}

	// Build configuration
	config := models.Config{
		Database: models.DatabaseConfig{
			Type:     *dbType,
			Host:     *dbHost,
			Port:     *dbPort,
			Database: *dbName,
			User:     *dbUser,
			Password: *dbPassword,
			SSLMode:  *dbSSLMode,
		},
		Migration: models.MigrationConfig{
			ScriptsPath: *scriptsPath,
			TableName:   *tableName,
		},
		Backup: models.BackupConfig{
			BackupPath: *backupPath,
		},
	}

	// Connect to database
	db, err := database.New(config.Database)
	if err != nil {
		log.Fatalf("Failed to connect to database: %v", err)
	}
	defer db.Close()

	fmt.Printf("Connected to %s database: %s\n", config.Database.Type, config.Database.Database)

	// Initialize backup manager
	backupMgr, err := backup.New(db, config.Backup.BackupPath)
	if err != nil {
		log.Fatalf("Failed to initialize backup manager: %v", err)
	}

	// Handle list-backups flag
	if *listBackups {
		if err := listAvailableBackups(backupMgr); err != nil {
			log.Fatalf("Failed to list backups: %v", err)
		}
		os.Exit(0)
	}

	// Handle restore flag
	if *doRestore {
		fmt.Println("\n=== Restoring from backup ===")
		if err := backupMgr.RestoreBackup(); err != nil {
			log.Fatalf("Failed to restore backup: %v", err)
		}
		fmt.Println("✓ Database restored successfully from latest backup")
		os.Exit(0)
	}

	// Initialize migration manager
	migrationMgr := migration.New(db, config.Migration)

	// Initialize migrations table
	if err := migrationMgr.Initialize(); err != nil {
		log.Fatalf("Failed to initialize migrations table: %v", err)
	}

	// Get pending migrations
	pending, err := migrationMgr.GetPendingMigrations()
	if err != nil {
		log.Fatalf("Failed to get pending migrations: %v", err)
	}

	if len(pending) == 0 {
		fmt.Println("\n✓ No pending migrations found. Database is up to date.")
		os.Exit(0)
	}

	fmt.Printf("\nFound %d pending migration(s):\n", len(pending))
	for i, script := range pending {
		fmt.Printf("  %d. %s - %s\n", i+1, script.Version, script.Description)
	}

	// Create backup if requested and there are pending migrations
	if *doBackup {
		fmt.Println("\n=== Creating backup ===")
		metadata, err := backupMgr.CreateBackup()
		if err != nil {
			log.Fatalf("Failed to create backup: %v", err)
		}
		fmt.Printf("✓ Backup created: %s (%.2f MB)\n",
			metadata.Filename,
			float64(metadata.Size)/(1024*1024))
	}

	// Apply pending migrations
	fmt.Println("\n=== Applying migrations ===")
	for i, script := range pending {
		fmt.Printf("[%d/%d] Applying %s - %s...\n",
			i+1, len(pending), script.Version, script.Description)

		if err := migrationMgr.ApplyMigration(script); err != nil {
			log.Fatalf("Failed to apply migration %s: %v", script.Version, err)
		}

		fmt.Printf("  ✓ Migration %s applied successfully\n", script.Version)
	}

	fmt.Println("\n✓ All migrations applied successfully!")
}

// listAvailableBackups lists all available backups
func listAvailableBackups(backupMgr *backup.Manager) error {
	backups, err := backupMgr.ListBackups()
	if err != nil {
		return err
	}

	if len(backups) == 0 {
		fmt.Println("No backups found.")
		return nil
	}

	fmt.Printf("Found %d backup(s):\n\n", len(backups))
	for i, b := range backups {
		fmt.Printf("%d. %s\n", i+1, b.Filename)
		fmt.Printf("   Created: %s\n", b.CreatedAt.Format("2006-01-02 15:04:05"))
		fmt.Printf("   Size:    %.2f MB\n\n", float64(b.Size)/(1024*1024))
	}

	return nil
}

// getEnvOrDefault gets an environment variable or returns a default value
func getEnvOrDefault(key, defaultValue string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return defaultValue
}

// getEnvOrDefaultInt gets an environment variable as int or returns a default value
func getEnvOrDefaultInt(key string, defaultValue int) int {
	if value := os.Getenv(key); value != "" {
		var intValue int
		if _, err := fmt.Sscanf(value, "%d", &intValue); err == nil {
			return intValue
		}
	}
	return defaultValue
}
