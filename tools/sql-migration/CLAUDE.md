# SQL Migration Tool - Context for Claude

This document provides detailed context about the SQL Migration Tool for Claude (both CLI and Web) interactions.

## Overview

**Purpose**: A command-line tool for managing database schema migrations with built-in backup and restore capabilities.

**Technology Stack**: Go 1.23+, PostgreSQL driver (lib/pq), SQL Server driver (go-mssqldb)

**Key Features**:
- Multi-database support (PostgreSQL and SQL Server)
- Automatic migration tracking via database table
- Optional backup before applying migrations (--backup flag)
- Easy restore from latest backup (--restore flag)
- Transaction-based migration application
- Checksum validation for migration integrity
- Support for multiple naming conventions

## Architecture

### Project Structure

```
sql-migration/
├── cmd/
│   └── sql-migration/
│       └── main.go              # CLI entry point
├── internal/
│   ├── models/
│   │   └── config.go            # Data models and configuration
│   ├── database/
│   │   └── database.go          # Database connection wrapper
│   ├── migration/
│   │   └── migration.go         # Migration logic and tracking
│   └── backup/
│       └── backup.go            # Backup and restore operations
├── migrations/                   # Sample migration scripts
│   ├── 001_initial_schema.sql
│   └── 002_add_posts_table.sql
├── backups/                      # Backup storage directory
├── config/                       # Configuration files (planned)
├── .env.example                  # Environment variable template
├── .gitignore                    # Git ignore patterns
├── README.md                     # User-facing documentation
├── CLAUDE.md                     # This file - context for Claude
└── go.mod                        # Go module definition
```

### Core Components

#### 1. Models Package (`internal/models/`)

Defines data structures used throughout the application:

- **Config**: Application configuration (database, migration, backup settings)
- **DatabaseConfig**: Database connection parameters
- **MigrationConfig**: Migration-specific settings (scripts path, table name)
- **BackupConfig**: Backup storage path
- **MigrationRecord**: Database representation of applied migration
- **MigrationScript**: File-based migration script representation
- **BackupMetadata**: Information about a backup file

#### 2. Database Package (`internal/database/`)

Provides database connectivity abstraction:

- **Database**: Wrapper around sql.DB with configuration
- **New()**: Creates connection based on database type
- **buildConnectionString()**: Builds type-specific connection strings
- Supports both PostgreSQL and SQL Server

**Connection String Formats**:
```go
// PostgreSQL
"host=%s port=%d user=%s password=%s dbname=%s sslmode=%s"

// SQL Server
"server=%s;port=%d;database=%s;user id=%s;password=%s;"
```

#### 3. Migration Package (`internal/migration/`)

Handles migration script management and execution:

- **Manager**: Core migration management
- **Initialize()**: Creates schema_migrations tracking table
- **GetAppliedMigrations()**: Retrieves migration history from database
- **GetPendingMigrations()**: Identifies unapplied migration scripts
- **ApplyMigration()**: Executes a migration in a transaction
- **readMigrationScripts()**: Scans directory for .sql files
- **parseFilename()**: Extracts version and description from filenames

**Migration Tracking Table**:
```sql
CREATE TABLE IF NOT EXISTS schema_migrations (
    id SERIAL PRIMARY KEY,
    version VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    checksum VARCHAR(32) NOT NULL
);
```

**Supported Filename Patterns**:
- `001_description.sql` - Simple format
- `V001__description.sql` - Flyway-style
- `v001_description.sql` - Lowercase variant

#### 4. Backup Package (`internal/backup/`)

Manages database backup and restore operations:

- **Manager**: Backup management
- **CreateBackup()**: Creates database backup using native tools
- **RestoreBackup()**: Restores from latest backup
- **ListBackups()**: Returns all available backups
- **getLatestBackup()**: Finds most recent backup file

**Backup Strategies**:
- **PostgreSQL**: Uses `pg_dump` (custom format) and `pg_restore`
- **SQL Server**: Uses `BACKUP DATABASE` and `RESTORE DATABASE` T-SQL

**Backup File Naming**: `{database}_{timestamp}.backup`
Example: `myapp_20240115_143022.backup`

### CLI Workflow

1. **Parse Flags**: Read command-line arguments and environment variables
2. **Validate Configuration**: Ensure required parameters are provided
3. **Connect to Database**: Establish connection and verify connectivity
4. **Handle Special Flags**:
   - `--version`: Show version and exit
   - `--list-backups`: List backups and exit
   - `--restore`: Restore from backup and exit
5. **Initialize Migration Tracking**: Create table if it doesn't exist
6. **Identify Pending Migrations**: Compare scripts with applied migrations
7. **Create Backup** (if `--backup` flag and pending migrations exist)
8. **Apply Migrations**: Execute each pending migration in order
9. **Report Results**: Display success/failure status

## Configuration

### Environment Variables

All configuration can be set via environment variables:

| Variable | Default | Description |
|----------|---------|-------------|
| `DB_TYPE` | `postgres` | Database type (postgres or mssql) |
| `DB_HOST` | `localhost` | Database host |
| `DB_PORT` | `5432` | Database port |
| `DB_NAME` | - | Database name (required) |
| `DB_USER` | - | Database user (required) |
| `DB_PASSWORD` | - | Database password |
| `DB_SSLMODE` | `disable` | PostgreSQL SSL mode |
| `MIGRATION_SCRIPTS_PATH` | `./migrations` | Path to migration scripts |
| `MIGRATION_TABLE` | `schema_migrations` | Migration tracking table name |
| `BACKUP_PATH` | `./backups` | Backup storage directory |

### Command-Line Flags

All flags can override environment variables:

- `--db-type`, `--db-host`, `--db-port`, `--db-name`, `--db-user`, `--db-password`, `--db-sslmode`
- `--scripts-path`: Path to migration scripts
- `--backup-path`: Path for backup storage
- `--table-name`: Migration tracking table name
- `--backup`: Create backup before migrations (boolean)
- `--restore`: Restore from latest backup (boolean)
- `--list-backups`: List available backups (boolean)
- `--version`: Show version information (boolean)

## Key Features Implementation

### 1. --backup Flag

**Purpose**: Create a database backup before applying migrations, but only if there are unapplied scripts.

**Implementation** (`cmd/sql-migration/main.go:106-115`):
```go
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
```

**Behavior**:
- Only runs if there are pending migrations
- If backup fails, the entire process stops (fail-fast)
- Creates timestamped backup file
- Reports backup size in megabytes

### 2. --restore Flag

**Purpose**: Restore database from the most recent backup without running any migrations.

**Implementation** (`cmd/sql-migration/main.go:90-96`):
```go
// Handle restore flag
if *doRestore {
    fmt.Println("\n=== Restoring from backup ===")
    if err := backupMgr.RestoreBackup(); err != nil {
        log.Fatalf("Failed to restore backup: %v", err)
    }
    fmt.Println("✓ Database restored successfully from latest backup")
    os.Exit(0)
}
```

**Behavior**:
- Finds the most recent backup file
- Restores database using native tools
- Exits immediately after restore (no migrations run)
- Fails if no backup files exist

### 3. Migration Tracking

Migrations are tracked in the database to prevent re-application:

1. Each migration file is hashed (MD5)
2. When applied, version, description, timestamp, and checksum are recorded
3. On subsequent runs, only new migrations are applied
4. Checksums detect if migration files have been modified

### 4. Transaction Support

Migrations are executed within database transactions:

```go
tx, err := m.db.GetDB().Begin()
defer tx.Rollback()

// Execute migration
_, err = tx.Exec(script.Content)

// Record migration
_, err = tx.Exec(recordQuery, ...)

// Commit
err = tx.Commit()
```

**Benefits**:
- Atomic application (all or nothing)
- Automatic rollback on error
- Consistent database state

## Development Guidelines

### Adding New Features

1. **New Database Type**:
   - Add connection string builder in `database/database.go:71-92`
   - Add backup/restore logic in `backup/backup.go:27-69` and `backup/backup.go:72-129`
   - Update documentation

2. **New Flag**:
   - Add flag definition in `main.go`
   - Add corresponding environment variable support
   - Implement flag logic in main workflow
   - Update README.md and .env.example

3. **Enhanced Migration Tracking**:
   - Modify `models.MigrationRecord` structure
   - Update table creation in `migration.Initialize()`
   - Adjust queries in `migration.GetAppliedMigrations()`

### Code Quality Standards

**Go Best Practices**:
- Follow standard Go project layout
- Use structured error handling with `fmt.Errorf` and `%w`
- Implement defer for cleanup (db.Close(), tx.Rollback())
- Use meaningful variable and function names
- Add GoDoc comments for exported functions
- Keep functions focused and small

**Security**:
- Never log passwords or sensitive data
- Use parameterized queries (already implemented)
- Validate file paths before operations
- Set appropriate file permissions for backups (0755)
- Support SSL/TLS for database connections

**Error Handling**:
- Fail fast on critical errors (connection, backup)
- Provide detailed error messages
- Use `log.Fatalf()` for unrecoverable errors
- Wrap errors with context using `fmt.Errorf("context: %w", err)`

### Testing

**Manual Testing**:
```bash
# Build
go build -o sql-migration cmd/sql-migration/main.go

# Test without database (should fail gracefully)
./sql-migration --db-name=test --db-user=test

# Test with database
export DB_NAME=testdb
export DB_USER=postgres
export DB_PASSWORD=secret
./sql-migration

# Test backup flag
./sql-migration --backup

# Test list backups
./sql-migration --list-backups

# Test restore
./sql-migration --restore
```

**Unit Testing** (Planned):
- Mock database connections
- Test filename parsing
- Test checksum calculation
- Test configuration loading
- Test error conditions

## Troubleshooting

### Common Issues

**1. "pg_dump: command not found"**
- Install PostgreSQL client tools
- Ubuntu: `apt-get install postgresql-client`
- macOS: `brew install postgresql`

**2. "No backup files found"**
- Ensure backup directory exists
- Check `--backup-path` or `BACKUP_PATH` setting
- Verify backups were created successfully

**3. "Migration failed: transaction rolled back"**
- Check SQL syntax in migration script
- Verify database permissions
- Review error message for specific issue
- Test migration script manually

**4. "Connection refused"**
- Verify database is running
- Check host and port settings
- Ensure user has connection privileges
- Check firewall rules

**5. Build Errors**
- Run `go mod tidy` to update dependencies
- Ensure Go 1.23+ is installed
- Check for syntax errors in code
- Verify import paths are correct

## Dependencies

### Direct Dependencies

```go
require (
    github.com/lib/pq v1.10.9                    // PostgreSQL driver
    github.com/microsoft/go-mssqldb v1.9.5       // SQL Server driver
)
```

### Indirect Dependencies
- Various Azure SDK packages (for SQL Server driver)
- Cryptography libraries (golang.org/x/crypto)
- UUID library (github.com/google/uuid)

### External Tools Required

**PostgreSQL**:
- `pg_dump`: Backup creation
- `pg_restore`: Backup restoration

**SQL Server**:
- `sqlcmd`: Backup and restore operations

## Best Practices for Users

### Migration Scripts

1. **Use Sequential Versioning**: 001, 002, 003, etc.
2. **Descriptive Names**: `001_create_users_table.sql`
3. **Idempotent When Possible**: Use `IF NOT EXISTS` clauses
4. **Small Migrations**: One logical change per file
5. **Test First**: Test on development before production
6. **No Rollbacks in Script**: This tool is forward-only

### Production Deployment

1. **Always Use --backup**: Safety first
2. **Test Restore Process**: Verify backups work
3. **Monitor Disk Space**: Backups consume space
4. **Version Control Scripts**: Commit migrations to Git
5. **Backup Retention**: Implement cleanup policy
6. **Use Environment Variables**: Avoid hardcoding credentials

### Security

1. **Secure Credentials**: Use environment variables
2. **Limit Permissions**: Use dedicated migration user
3. **SSL/TLS**: Enable for production databases
4. **Audit Trail**: Monitor who runs migrations
5. **Backup Security**: Encrypt and secure backup files

## Integration Examples

### Docker

```dockerfile
FROM golang:1.23-alpine AS builder
WORKDIR /app
COPY . .
RUN go build -o sql-migration cmd/sql-migration/main.go

FROM alpine:latest
RUN apk add --no-cache postgresql-client
COPY --from=builder /app/sql-migration /usr/local/bin/
COPY migrations /migrations
ENTRYPOINT ["sql-migration"]
CMD ["--scripts-path=/migrations", "--backup"]
```

### CI/CD Pipeline

```yaml
# GitHub Actions example
- name: Run Database Migrations
  env:
    DB_HOST: ${{ secrets.DB_HOST }}
    DB_NAME: ${{ secrets.DB_NAME }}
    DB_USER: ${{ secrets.DB_USER }}
    DB_PASSWORD: ${{ secrets.DB_PASSWORD }}
  run: |
    ./sql-migration --backup
```

### Kubernetes Job

```yaml
apiVersion: batch/v1
kind: Job
metadata:
  name: db-migration
spec:
  template:
    spec:
      containers:
      - name: migrator
        image: myregistry/sql-migration:latest
        args: ["--backup"]
        env:
        - name: DB_HOST
          value: postgres-service
        - name: DB_NAME
          valueFrom:
            secretKeyRef:
              name: db-credentials
              key: database
        - name: DB_USER
          valueFrom:
            secretKeyRef:
              name: db-credentials
              key: username
        - name: DB_PASSWORD
          valueFrom:
            secretKeyRef:
              name: db-credentials
              key: password
      restartPolicy: OnFailure
```

## Future Enhancements

### Planned Features
- [ ] Rollback migrations (down migrations)
- [ ] Migration dry-run mode
- [ ] Support for MySQL/MariaDB
- [ ] Web UI for migration management
- [ ] Backup compression
- [ ] Backup encryption
- [ ] Remote backup storage (S3, Azure Blob)
- [ ] Migration validation before execution
- [ ] Parallel migration execution
- [ ] Custom backup naming patterns
- [ ] Backup retention policies

### Potential Improvements
- Configuration file support (YAML/TOML)
- Interactive mode for confirming migrations
- Migration rollback scripts support
- Better progress reporting for large migrations
- Migration performance metrics
- Email notifications on completion
- Webhook support for integrations
- Advanced logging with log levels

## Working with Claude

### Common Development Tasks

**Adding a New Database Type**:
1. Read `internal/database/database.go` to understand connection logic
2. Add case in `buildConnectionString()` function
3. Read `internal/backup/backup.go` to understand backup logic
4. Add backup commands for new database type
5. Test with sample database
6. Update README.md with new database type

**Debugging Migration Issues**:
1. Check migration script SQL syntax
2. Verify database permissions
3. Test migration manually with database client
4. Add debug logging if needed
5. Check transaction support for operation

**Enhancing Error Messages**:
1. Identify error location in code
2. Add context with `fmt.Errorf()`
3. Include helpful troubleshooting hints
4. Test error scenarios

### Code Locations Reference

- **CLI Entry Point**: `cmd/sql-migration/main.go:1`
- **Flag Definitions**: `cmd/sql-migration/main.go:18-33`
- **Backup Logic**: `cmd/sql-migration/main.go:106-115`
- **Restore Logic**: `cmd/sql-migration/main.go:90-96`
- **Database Connection**: `internal/database/database.go:16-38`
- **Migration Application**: `internal/migration/migration.go:123-155`
- **Backup Creation**: `internal/backup/backup.go:27-69`
- **Backup Restoration**: `internal/backup/backup.go:72-129`

## Questions for Claude

When working on this project, you can ask Claude:

- "How does the --backup flag work in sql-migration?"
- "Where is the migration tracking table created?"
- "How can I add support for MySQL?"
- "How does the restore process work?"
- "What happens if a migration fails mid-execution?"
- "How are migration filenames parsed?"
- "Where should I add a new command-line flag?"
- "How can I improve error handling in the backup process?"

Claude has full context from this document and can help with:
- Adding new features
- Debugging issues
- Code refactoring
- Documentation updates
- Testing strategies
- Performance optimization

---

**Last Updated**: 2024-01-08
**Version**: 1.0.0
**Maintainer**: AdeoTEK Team
