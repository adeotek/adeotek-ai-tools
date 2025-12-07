# Adeotek SQL MCP Server

A production-ready Model Context Protocol (MCP) server providing read-only access to SQL databases (Microsoft SQL Server and PostgreSQL). Built with security-first design principles and comprehensive safety mechanisms.

## Features

- **Multi-Database Support**: Works with both Microsoft SQL Server and PostgreSQL
- **Read-Only by Design**: Multiple layers of protection prevent data modification
- **MCP Protocol 2025-11-25**: Full compliance with the latest MCP specification
- **5 Powerful Tools**: Database listing, table exploration, schema analysis, querying, and performance planning
- **3 AI Prompts**: Schema analysis, query assistance, and performance review
- **Security First**: SQL injection prevention, query validation, and input sanitization
- **TypeScript**: Modern ES2022+ TypeScript with full type safety
- **Comprehensive Testing**: >80% code coverage with extensive test suite

## Installation

```bash
npm install adeotek-sql-mcp
```

Or install from source:

```bash
git clone https://github.com/adeotek/adeotek-ai-tools.git
cd adeotek-ai-tools/adeotek-sql-mcp
npm install
npm run build
```

## Quick Start

### Starting the Server

The server requires at least one database connection to be configured at startup. You can provide connection strings via command-line arguments or environment variables.

**Command-Line Arguments**:
```bash
# Single connection (named 'default')
node dist/index.js --default "type=postgres;host=localhost;port=5432;user=myuser;password=mypass"

# Multiple named connections
node dist/index.js --postgres "type=postgres;host=localhost;..." --mssql "type=mssql;host=localhost;..."
```

**Environment Variables**:
```bash
# Set connection via environment variable
export SQL_CONNECTION_DEFAULT="type=postgres;host=localhost;port=5432;user=myuser;password=mypass"
node dist/index.js
```

The server uses stdio transport and integrates with MCP clients like Claude Desktop, VS Code, or custom integrations.

### Connection String Format

**PostgreSQL**:
```
type=postgres;host=localhost;port=5432;user=myuser;password=mypass;database=mydb;ssl=true
```

**SQL Server**:
```
type=mssql;host=localhost;port=1433;user=sa;password=StrongPass123;database=master;ssl=true
```

**Supported Keys**:
- `type`: "postgres" or "mssql" (required)
- `host`/`server`: Database server host (required)
- `port`: Port number (default: 5432 for PostgreSQL, 1433 for SQL Server)
- `user`/`username`: Database user (required)
- `password`/`pwd`: Database password (required)
- `database`: Database name (optional, defaults to "postgres" for PostgreSQL or "master" for SQL Server)
- `ssl`/`encrypt`: Enable SSL/TLS (true/false)

**Note**: If you don't specify a database, the server will connect to the system database (`postgres` or `master`) by default. You can then specify the target database in individual tool calls, or list all available databases using the `sql_list_databases` tool.

## MCP Tools

### 1. sql_list_databases

List all databases available on the configured server.

**Input**:
```json
{
  "connection": "default"
}
```

Note: The `connection` parameter is optional. If omitted, uses the "default" connection or the only configured connection.

**Output**:
```json
{
  "success": true,
  "data": [
    {
      "name": "mydb",
      "size": "42 MB",
      "owner": "postgres",
      "encoding": "UTF8"
    }
  ]
}
```

### 2. sql_list_tables

List all tables in a specified database with metadata.

**Input**:
```json
{
  "database": "mydb",
  "schema": "public",
  "connection": "postgres"
}
```

Note: The `connection` and `schema` parameters are optional.

**Output**:
```json
{
  "success": true,
  "data": [
    {
      "schema": "public",
      "name": "users",
      "type": "table",
      "rowCount": 1523,
      "sizeEstimate": "128 KB"
    }
  ]
}
```

### 3. sql_describe_table

Get detailed schema information for a specific table.

**Input**:
```json
{
  "database": "mydb",
  "table": "users",
  "schema": "public",
  "connection": "default"
}
```

Note: The `connection` and `schema` parameters are optional.

**Output**:
```json
{
  "success": true,
  "data": {
    "schema": "public",
    "table": "users",
    "columns": [
      {
        "name": "id",
        "type": "integer",
        "nullable": false,
        "isPrimaryKey": true,
        "isForeignKey": false
      }
    ],
    "indexes": [...],
    "foreignKeys": [...],
    "constraints": [...]
  }
}
```

### 4. sql_query

Execute a read-only SELECT query with automatic safety validation.

**Input**:
```json
{
  "database": "mydb",
  "query": "SELECT id, name, email FROM users WHERE created_at > '2024-01-01' LIMIT 100",
  "maxRows": 1000,
  "connection": "default"
}
```

Note: The `connection` and `maxRows` parameters are optional (maxRows defaults to 1000, max 10000).

**Output**:
```json
{
  "success": true,
  "data": {
    "columns": ["id", "name", "email"],
    "rows": [
      { "id": 1, "name": "John", "email": "john@example.com" }
    ],
    "rowCount": 42,
    "executionTimeMs": 15
  }
}
```

### 5. sql_get_query_plan

Get the execution plan for a query without executing it.

**Input**:
```json
{
  "database": "mydb",
  "query": "SELECT * FROM users WHERE email LIKE '%@example.com%'",
  "connection": "default"
}
```

Note: The `connection` parameter is optional.

**Output**:
```json
{
  "success": true,
  "data": {
    "plan": "...",
    "format": "json",
    "estimatedCost": 125.5
  }
}
```

## MCP Prompts

### 1. analyze-schema

Analyze database schema and provide insights.

**Arguments**:
- `database` (required): Database to analyze
- `focus` (optional): Specific area to focus on (e.g., "tables", "relationships", "indexes")

### 2. query-assistant

Help construct SQL queries based on natural language requirements.

**Arguments**:
- `database` (required): Target database
- `requirement` (required): Natural language description of what to query

### 3. performance-review

Review query performance and suggest optimizations.

**Arguments**:
- `database` (required): Database name
- `query` (required): SQL query to analyze

## Integration Examples

### Claude Desktop

Add to your Claude Desktop configuration (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "adeotek-sql-mcp": {
      "command": "node",
      "args": [
        "/path/to/adeotek-sql-mcp/dist/index.js",
        "--default",
        "type=postgres;host=localhost;port=5432;user=myuser;password=mypass"
      ]
    }
  }
}
```

For multiple connections:

```json
{
  "mcpServers": {
    "adeotek-sql-mcp": {
      "command": "node",
      "args": [
        "/path/to/adeotek-sql-mcp/dist/index.js",
        "--postgres",
        "type=postgres;host=localhost;port=5432;user=pguser;password=pgpass",
        "--mssql",
        "type=mssql;host=localhost;port=1433;user=sa;password=sqlpass"
      ]
    }
  }
}
```

### VS Code

Add to your `mcp.json` configuration:

```json
{
  "servers": {
    "adeotek-sql-mcp": {
      "type": "stdio",
      "command": "node",
      "args": [
        "/path/to/adeotek-sql-mcp/dist/index.js",
        "--default",
        "type=postgres;host=localhost;port=5432;user=myuser;password=mypass"
      ]
    }
  },
  "inputs": []
}
```

Alternatively, use environment variables:

```json
{
  "servers": {
    "adeotek-sql-mcp": {
      "type": "stdio",
      "command": "node",
      "args": ["/path/to/adeotek-sql-mcp/dist/index.js"],
      "env": {
        "SQL_CONNECTION_DEFAULT": "type=postgres;host=localhost;port=5432;user=myuser;password=mypass"
      }
    }
  },
  "inputs": []
}
```

### Programmatic Usage

```typescript
import { AdeoSqlMcpServer } from 'adeotek-sql-mcp';

const connections = new Map<string, string>();
connections.set('default', 'type=postgres;host=localhost;port=5432;user=myuser;password=mypass');
connections.set('mssql', 'type=mssql;host=localhost;port=1433;user=sa;password=sqlpass');

const server = new AdeoSqlMcpServer(connections);
await server.start();
```

## Security Features

### Read-Only Protection

Multiple layers of protection ensure only read operations are allowed:

1. **Query Validation**: Blocks INSERT, UPDATE, DELETE, DROP, CREATE, ALTER, TRUNCATE, and other modification keywords
2. **Pattern Detection**: Identifies dangerous SQL patterns and functions
3. **Input Sanitization**: Sanitizes database/table/column names to prevent SQL injection
4. **Row Limits**: Automatic LIMIT clause enforcement (max 10,000 rows)
5. **Query Timeout**: Prevents long-running queries (configurable, default 30s)

### Blocked Operations

The following operations are strictly blocked:

**Data Modification**: INSERT, UPDATE, DELETE, TRUNCATE, MERGE, COPY

**Schema Modification**: CREATE, ALTER, DROP, RENAME

**Permissions**: GRANT, REVOKE

**Transactions**: BEGIN, COMMIT, ROLLBACK, SAVEPOINT

**Locking**: LOCK, UNLOCK

**Maintenance**: VACUUM, ANALYZE, REINDEX

**Configuration**: SET, RESET

**Dangerous Functions**: pg_read_file, pg_execute, xp_cmdshell, sp_executesql, etc.

## Development

### Prerequisites

- Node.js 18+
- TypeScript 5.7+
- PostgreSQL 12+ and/or SQL Server 2019+ (for testing)

### Setup

```bash
# Clone repository
git clone https://github.com/adeotek/adeotek-ai-tools.git
cd adeotek-ai-tools/adeotek-sql-mcp

# Install dependencies
npm install

# Build
npm run build

# Run tests
npm test

# Run tests with coverage
npm run test:coverage

# Lint and format
npm run lint
npm run format
```

### Project Structure

```
adeotek-sql-mcp/
├── src/
│   ├── index.ts              # Entry point
│   ├── server.ts             # MCP server implementation
│   ├── tools/                # MCP tool implementations
│   ├── prompts/              # MCP prompt implementations
│   ├── database/             # Database connection managers
│   ├── security/             # Query validation and safety
│   ├── types/                # TypeScript type definitions
│   ├── utils/                # Utilities (logging, errors)
│   └── __tests__/            # Comprehensive test suite
├── package.json
├── tsconfig.json
├── jest.config.js
└── README.md
```

## Testing

The project includes comprehensive tests with >80% coverage:

```bash
# Run all tests
npm test

# Run specific test suite
npm test -- safety.test.ts

# Run with coverage
npm run test:coverage

# Watch mode
npm run test:watch
```

Test categories:
- **Safety Tests**: Query validation, SQL injection prevention
- **Database Tests**: Connection parsing, configuration
- **Prompt Tests**: All prompt templates
- **Protocol Tests**: MCP compliance (coming soon)

## Environment Variables

- `LOG_LEVEL`: Logging level (debug, info, warn, error) - default: info
- `NODE_ENV`: Environment (development, production) - default: development

## Troubleshooting

### Connection Issues

**Problem**: "Failed to connect to database"

**Solutions**:
- Verify connection string format
- Check database server is running
- Verify credentials are correct
- Check firewall settings
- Ensure SSL settings match server configuration

### Query Validation Errors

**Problem**: "Blocked keyword detected"

**Solutions**:
- Ensure query starts with SELECT, WITH, or EXPLAIN
- Remove any data modification keywords
- Check for dangerous functions
- Review blocked operations list

### Rate Limiting / Timeouts

**Problem**: "Query execution timeout"

**Solutions**:
- Optimize query with proper indexes
- Add WHERE clauses to filter data
- Use LIMIT to reduce result set size
- Check database performance

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass (`npm test`)
6. Format code (`npm run format`)
7. Commit changes (`git commit -m 'Add amazing feature'`)
8. Push to branch (`git push origin feature/amazing-feature`)
9. Open a Pull Request

## License

MIT License - see LICENSE file for details

## Support

- **Issues**: https://github.com/adeotek/adeotek-ai-tools/issues
- **Documentation**: https://github.com/adeotek/adeotek-ai-tools/tree/main/adeotek-sql-mcp
- **MCP Specification**: https://modelcontextprotocol.io/

## Acknowledgments

- Built with [@modelcontextprotocol/sdk](https://github.com/modelcontextprotocol/sdk)
- Uses [mssql](https://github.com/tediousjs/node-mssql) for SQL Server
- Uses [pg](https://github.com/brianc/node-postgres) for PostgreSQL
- Logging powered by [Winston](https://github.com/winstonjs/winston)
