# SQL Migration Tool

A robust command-line tool for managing SQL database migrations with built-in backup and restore capabilities.

## Features

- **Multi-Database Support**: Works with PostgreSQL and Microsoft SQL Server
- **Migration Tracking**: Automatically tracks applied migrations in a database table
- **Backup Before Migration**: Optional backup creation before applying migrations
- **Easy Restore**: Restore from the latest backup with a single flag
- **Safe Migrations**: Applies migrations in transactions (when supported)
- **Version Control**: Migration scripts are versioned and applied in order
- **Checksum Validation**: Ensures migration integrity with MD5 checksums

## Installation

### Prerequisites

- Go 1.23 or higher
- Database tools:
  - For PostgreSQL: `pg_dump` and `pg_restore` (included with PostgreSQL client)
  - For SQL Server: `sqlcmd` (included with SQL Server tools)

### Build from Source

```bash
cd tools/sql-migration
go mod download
go build -o sql-migration cmd/sql-migration/main.go
```

### Install Globally

```bash
go install github.com/adeotek/adeotek-ai-tools/tools/sql-migration/cmd/sql-migration@latest
```

## Quick Start

### 1. Create Migration Scripts Directory

```bash
mkdir migrations
```

### 2. Create Your First Migration

Create a file `migrations/001_initial_schema.sql`:

```sql
-- Create users table
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create posts table
CREATE TABLE posts (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id),
    title VARCHAR(255) NOT NULL,
    content TEXT,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
```

### 3. Configure Database Connection

Create a `.env` file (copy from `.env.example`):

```bash
cp .env.example .env
# Edit .env with your database credentials
```

### 4. Run Migrations

```bash
# Run migrations without backup
./sql-migration

# Run migrations with backup
./sql-migration --backup

# Restore from latest backup
./sql-migration --restore

# List available backups
./sql-migration --list-backups
```

## Usage

### Command-Line Flags

| Flag | Environment Variable | Default | Description |
|------|---------------------|---------|-------------|
| `--db-type` | `DB_TYPE` | `postgres` | Database type (postgres or mssql) |
| `--db-host` | `DB_HOST` | `localhost` | Database host |
| `--db-port` | `DB_PORT` | `5432` | Database port |
| `--db-name` | `DB_NAME` | - | Database name (required) |
| `--db-user` | `DB_USER` | - | Database user (required) |
| `--db-password` | `DB_PASSWORD` | - | Database password |
| `--db-sslmode` | `DB_SSLMODE` | `disable` | PostgreSQL SSL mode |
| `--scripts-path` | `MIGRATION_SCRIPTS_PATH` | `./migrations` | Path to migration scripts |
| `--backup-path` | `BACKUP_PATH` | `./backups` | Path to store backups |
| `--table-name` | `MIGRATION_TABLE` | `schema_migrations` | Name of migrations tracking table |
| `--backup` | - | `false` | Create backup before migrations |
| `--restore` | - | `false` | Restore from last backup |
| `--list-backups` | - | `false` | List available backups |
| `--version` | - | `false` | Show version information |

### Examples

#### PostgreSQL

```bash
# Using environment variables
export DB_NAME=myapp
export DB_USER=postgres
export DB_PASSWORD=secret
./sql-migration --backup

# Using command-line flags
./sql-migration \
  --db-type=postgres \
  --db-host=localhost \
  --db-port=5432 \
  --db-name=myapp \
  --db-user=postgres \
  --db-password=secret \
  --backup
```

#### SQL Server

```bash
./sql-migration \
  --db-type=mssql \
  --db-host=localhost \
  --db-port=1433 \
  --db-name=MyDatabase \
  --db-user=sa \
  --db-password=YourPassword123 \
  --backup
```

#### Restore Database

```bash
# Restore from the latest backup
./sql-migration --restore

# List available backups first
./sql-migration --list-backups
```

## Migration Script Naming Convention

Migration scripts should follow one of these naming patterns:

- `001_description.sql` - Simple version number
- `V001__description.sql` - Flyway-style format
- `v001_description.sql` - Lowercase variant

Examples:
- `001_initial_schema.sql`
- `002_add_users_table.sql`
- `V003__add_indexes.sql`
- `v004_update_constraints.sql`

The tool will:
1. Parse the version number (001, 002, etc.)
2. Extract the description (underscores converted to spaces)
3. Apply migrations in version order
4. Track applied migrations to prevent re-running

## How It Works

### Migration Tracking

The tool creates a `schema_migrations` table (configurable) to track applied migrations:

```sql
CREATE TABLE schema_migrations (
    id SERIAL PRIMARY KEY,
    version VARCHAR(255) NOT NULL UNIQUE,
    description TEXT,
    applied_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    checksum VARCHAR(32) NOT NULL
);
```

### Migration Workflow

1. **Initialize**: Creates tracking table if it doesn't exist
2. **Scan**: Reads all `.sql` files from the migrations directory
3. **Compare**: Identifies unapplied migrations
4. **Backup** (if `--backup` flag): Creates database backup
5. **Apply**: Executes each pending migration in a transaction
6. **Record**: Saves migration metadata to tracking table

### Backup and Restore

#### PostgreSQL
- **Backup**: Uses `pg_dump` with custom format (`-F c`)
- **Restore**: Uses `pg_restore` with clean flag (`-c`)

#### SQL Server
- **Backup**: Uses `BACKUP DATABASE` T-SQL command
- **Restore**: Uses `RESTORE DATABASE` T-SQL command

Backups are stored in the configured backup directory with timestamps:
```
backups/
  myapp_20240115_143022.backup
  myapp_20240115_150045.backup
```

## Advanced Usage

### Multiple Environments

Create different .env files for each environment:

```bash
# Development
./sql-migration --backup

# Production (using different config)
export DB_HOST=prod-db.example.com
export DB_NAME=production_db
./sql-migration --backup
```

### CI/CD Integration

```bash
#!/bin/bash
# deploy-migrations.sh

set -e

# Always create backup in production
if [ "$ENVIRONMENT" = "production" ]; then
    ./sql-migration --backup
else
    ./sql-migration
fi
```

### Docker

Create a Dockerfile:

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
CMD ["--scripts-path=/migrations"]
```

## Error Handling

The tool implements robust error handling:

- **Connection Errors**: Validates database connectivity before proceeding
- **Migration Failures**: Rolls back failed migrations (transaction support)
- **Backup Failures**: Stops execution if backup fails (when `--backup` is used)
- **Missing Files**: Reports clear errors for missing migration scripts
- **Checksum Validation**: Detects modified migration scripts

## Troubleshooting

### "No backup files found"

```bash
# Create the backup directory
mkdir -p backups

# Or specify a different path
./sql-migration --backup-path=/path/to/backups
```

### "pg_dump: command not found"

Install PostgreSQL client tools:
```bash
# Ubuntu/Debian
sudo apt-get install postgresql-client

# macOS
brew install postgresql
```

### "Migration failed: transaction rolled back"

Check the migration SQL for errors:
1. Verify syntax is correct for your database type
2. Ensure referenced tables/columns exist
3. Check constraints and dependencies

### Permission Errors

Ensure the database user has necessary permissions:

```sql
-- PostgreSQL
GRANT ALL PRIVILEGES ON DATABASE mydb TO myuser;
GRANT ALL ON ALL TABLES IN SCHEMA public TO myuser;

-- SQL Server
ALTER ROLE db_owner ADD MEMBER myuser;
```

## Best Practices

1. **Always Backup in Production**: Use `--backup` flag for production deployments
2. **Test Migrations**: Test on a copy of production data before deploying
3. **Incremental Changes**: Keep migrations small and focused
4. **No Rollback Scripts**: This tool applies forward-only migrations
5. **Version Control**: Commit migration scripts to your repository
6. **Naming Convention**: Use consistent naming with sequential versions
7. **Idempotency**: While not required, consider using `IF NOT EXISTS` clauses

## Security Considerations

- **Credentials**: Use environment variables for sensitive data
- **Backups**: Ensure backup directory has proper permissions
- **Network**: Use SSL/TLS for database connections in production
- **Validation**: The tool validates migrations before applying
- **Transactions**: Migrations are applied in transactions when supported

## Limitations

- **Forward Only**: No automatic rollback/downgrade migrations
- **External Tools**: Requires database client tools (pg_dump, sqlcmd)
- **Transaction Support**: Some DDL operations may not be transactional
- **Single Database**: Operates on one database at a time

## Contributing

Contributions are welcome! Please follow the guidelines in the main repository [CLAUDE.md](../../CLAUDE.md).

## License

MIT License - see [LICENSE](../../LICENSE) for details.

## Related Projects

- **HTTP Agent**: [/agents/http-agent](../../agents/http-agent)
- **PostgreSQL MCP**: [/mcp-servers/postgres-mcp](../../mcp-servers/postgres-mcp)

## Support

For issues and questions:
- **Issues**: https://github.com/adeotek/adeotek-ai-tools/issues
- **Discussions**: https://github.com/adeotek/adeotek-ai-tools/discussions
