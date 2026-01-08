package backup

import (
	"fmt"
	"os"
	"os/exec"
	"path/filepath"
	"sort"
	"time"

	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/database"
	"github.com/adeotek/adeotek-ai-tools/tools/sql-migration/internal/models"
)

// Manager handles database backups and restores
type Manager struct {
	db         *database.Database
	backupPath string
}

// New creates a new backup manager
func New(db *database.Database, backupPath string) (*Manager, error) {
	// Create backup directory if it doesn't exist
	if err := os.MkdirAll(backupPath, 0755); err != nil {
		return nil, fmt.Errorf("failed to create backup directory: %w", err)
	}

	return &Manager{
		db:         db,
		backupPath: backupPath,
	}, nil
}

// CreateBackup creates a backup of the database
func (m *Manager) CreateBackup() (*models.BackupMetadata, error) {
	config := m.db.GetConfig()
	timestamp := time.Now().Format("20060102_150405")
	filename := fmt.Sprintf("%s_%s.backup", config.Database, timestamp)
	backupFile := filepath.Join(m.backupPath, filename)

	var cmd *exec.Cmd

	switch config.Type {
	case "postgres":
		// Use pg_dump for PostgreSQL
		cmd = exec.Command(
			"pg_dump",
			"-h", config.Host,
			"-p", fmt.Sprintf("%d", config.Port),
			"-U", config.User,
			"-d", config.Database,
			"-F", "c", // custom format
			"-f", backupFile,
		)
		// Set password via environment variable
		cmd.Env = append(os.Environ(), fmt.Sprintf("PGPASSWORD=%s", config.Password))

	case "mssql", "sqlserver":
		// Use sqlcmd for SQL Server backup
		query := fmt.Sprintf(
			"BACKUP DATABASE [%s] TO DISK = '%s' WITH FORMAT, COMPRESSION",
			config.Database,
			backupFile,
		)
		cmd = exec.Command(
			"sqlcmd",
			"-S", fmt.Sprintf("%s,%d", config.Host, config.Port),
			"-U", config.User,
			"-P", config.Password,
			"-Q", query,
		)

	default:
		return nil, fmt.Errorf("unsupported database type for backup: %s", config.Type)
	}

	// Execute backup command
	output, err := cmd.CombinedOutput()
	if err != nil {
		return nil, fmt.Errorf("backup failed: %w\nOutput: %s", err, string(output))
	}

	// Get file info
	fileInfo, err := os.Stat(backupFile)
	if err != nil {
		return nil, fmt.Errorf("failed to get backup file info: %w", err)
	}

	metadata := &models.BackupMetadata{
		Filename:     filename,
		CreatedAt:    time.Now(),
		DatabaseName: config.Database,
		Size:         fileInfo.Size(),
	}

	return metadata, nil
}

// RestoreBackup restores the database from the most recent backup
func (m *Manager) RestoreBackup() error {
	// Find the most recent backup
	backupFile, err := m.getLatestBackup()
	if err != nil {
		return err
	}

	config := m.db.GetConfig()
	var cmd *exec.Cmd

	switch config.Type {
	case "postgres":
		// Close existing connection before restore
		if err := m.db.Close(); err != nil {
			return fmt.Errorf("failed to close database connection: %w", err)
		}

		// Use pg_restore for PostgreSQL
		cmd = exec.Command(
			"pg_restore",
			"-h", config.Host,
			"-p", fmt.Sprintf("%d", config.Port),
			"-U", config.User,
			"-d", config.Database,
			"-c", // clean (drop) database objects before recreating
			"--if-exists",
			backupFile,
		)
		cmd.Env = append(os.Environ(), fmt.Sprintf("PGPASSWORD=%s", config.Password))

	case "mssql", "sqlserver":
		// For SQL Server, we need to close connections first
		// Use RESTORE DATABASE command
		query := fmt.Sprintf(
			"USE master; ALTER DATABASE [%s] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [%s] FROM DISK = '%s' WITH REPLACE; ALTER DATABASE [%s] SET MULTI_USER",
			config.Database, config.Database, backupFile, config.Database,
		)
		cmd = exec.Command(
			"sqlcmd",
			"-S", fmt.Sprintf("%s,%d", config.Host, config.Port),
			"-U", config.User,
			"-P", config.Password,
			"-Q", query,
		)

	default:
		return fmt.Errorf("unsupported database type for restore: %s", config.Type)
	}

	// Execute restore command
	output, err := cmd.CombinedOutput()
	if err != nil {
		return fmt.Errorf("restore failed: %w\nOutput: %s", err, string(output))
	}

	return nil
}

// getLatestBackup finds the most recent backup file
func (m *Manager) getLatestBackup() (string, error) {
	files, err := filepath.Glob(filepath.Join(m.backupPath, "*.backup"))
	if err != nil {
		return "", fmt.Errorf("failed to list backup files: %w", err)
	}

	if len(files) == 0 {
		return "", fmt.Errorf("no backup files found in %s", m.backupPath)
	}

	// Sort by modification time (newest first)
	sort.Slice(files, func(i, j int) bool {
		infoI, _ := os.Stat(files[i])
		infoJ, _ := os.Stat(files[j])
		return infoI.ModTime().After(infoJ.ModTime())
	})

	return files[0], nil
}

// ListBackups returns a list of all available backups
func (m *Manager) ListBackups() ([]models.BackupMetadata, error) {
	files, err := filepath.Glob(filepath.Join(m.backupPath, "*.backup"))
	if err != nil {
		return nil, fmt.Errorf("failed to list backup files: %w", err)
	}

	var backups []models.BackupMetadata
	for _, file := range files {
		fileInfo, err := os.Stat(file)
		if err != nil {
			continue
		}

		backups = append(backups, models.BackupMetadata{
			Filename:  filepath.Base(file),
			CreatedAt: fileInfo.ModTime(),
			Size:      fileInfo.Size(),
		})
	}

	// Sort by creation time (newest first)
	sort.Slice(backups, func(i, j int) bool {
		return backups[i].CreatedAt.After(backups[j].CreatedAt)
	})

	return backups, nil
}
