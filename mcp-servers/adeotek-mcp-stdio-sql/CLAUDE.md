# Adeotek SQL MCP Server - Context for Claude

This document provides comprehensive technical context about the Adeotek SQL MCP Server for Claude (both CLI and Web) interactions.

## Project Overview

**Purpose**: A production-ready Model Context Protocol (MCP) server providing read-only access to SQL databases (Microsoft SQL Server and PostgreSQL) with comprehensive security safeguards.

**Version**: 1.0.0

**MCP Protocol**: 2025-11-25 (stdio transport)

**Technology Stack**: TypeScript 5.7, Node.js 18+, @modelcontextprotocol/sdk, mssql, pg, Winston, Jest

**Location in Repository**: `/mcp-servers/adeotek-sql-mcp`

**Key Capabilities**:
- Full MCP Protocol 2025-11-25 implementation with stdio transport
- Five MCP tools for database operations
- Three MCP prompts for schema analysis and query assistance
- Multi-database support (PostgreSQL and SQL Server)
- Comprehensive read-only security with multiple validation layers
- TypeScript with ES2022+ features and full type safety
- Extensive test coverage (>80%)

## Architecture

### Project Structure

```
mcp-servers/adeotek-sql-mcp/
├── src/
│   ├── index.ts                      # Entry point and server startup
│   ├── server.ts                     # MCP server implementation
│   ├── tools/
│   │   └── index.ts                  # All 5 MCP tools
│   ├── prompts/
│   │   └── index.ts                  # All 3 MCP prompts
│   ├── database/
│   │   ├── connection.ts             # Connection factory and parsing
│   │   ├── postgres.ts               # PostgreSQL implementation
│   │   └── mssql.ts                  # SQL Server implementation
│   ├── security/
│   │   └── queryValidator.ts         # Read-only validation layer
│   ├── types/
│   │   └── index.ts                  # TypeScript type definitions
│   ├── utils/
│   │   ├── logger.ts                 # Winston logging
│   │   └── errors.ts                 # Custom error classes
│   └── __tests__/                    # Comprehensive test suite
│       ├── safety.test.ts            # Security validation tests
│       ├── database.test.ts          # Connection parsing tests
│       └── prompts.test.ts           # Prompt template tests
├── package.json                      # Dependencies and scripts
├── tsconfig.json                     # TypeScript configuration
├── jest.config.js                    # Jest test configuration
├── .eslintrc.json                    # ESLint rules
├── .prettierrc                       # Prettier formatting
├── .editorconfig                     # Editor configuration
├── .gitignore                        # Git ignore patterns
├── README.md                         # User-facing documentation
└── CLAUDE.md                         # This file - Claude context
```

### Design Patterns

1. **Factory Pattern**: `createConnection()` creates the appropriate database instance
2. **Strategy Pattern**: Different database implementations (PostgreSQL, SQL Server) with common interface
3. **Validation Pattern**: Multi-layer query validation for security
4. **Error Handling Pattern**: Custom error classes with consistent structure
5. **Dependency Injection**: Database connections injected into tools
6. **MCP Protocol Pattern**: Tools and prompts registered with MCP server

### Key Components

**Server** (`src/server.ts`):
- MCP server implementation using @modelcontextprotocol/sdk
- stdio transport for client-server communication
- Tool and prompt registration
- Request handling and routing
- Connection management and lifecycle

**Database Implementations**:
- **PostgresDatabase** (`src/database/postgres.ts`): PostgreSQL operations using pg package
- **MssqlDatabase** (`src/database/mssql.ts`): SQL Server operations using mssql package
- **Connection Factory** (`src/database/connection.ts`): Connection string parsing and factory

**Security** (`src/security/queryValidator.ts`):
- Query validation with blocked keyword detection
- SQL injection prevention
- Input sanitization
- Row limit enforcement
- Pattern-based dangerous query detection

**Tools** (`src/tools/index.ts`):
- sql_list_databases
- sql_list_tables
- sql_describe_table
- sql_query
- sql_get_query_plan

**Prompts** (`src/prompts/index.ts`):
- analyze-schema
- query-assistant
- performance-review

**Utilities**:
- **Logger** (`src/utils/logger.ts`): Winston-based logging with sensitive data sanitization
- **Errors** (`src/utils/errors.ts`): Custom error classes for different error types

## MCP Protocol Implementation

This server implements MCP Protocol 2025-11-25 with stdio transport.

### Supported MCP Methods

#### Tools

**`tools/list`**: List all available tools
**`tools/call`**: Execute a tool with arguments

#### Prompts

**`prompts/list`**: List all available prompts
**`prompts/get`**: Get a prompt with argument substitution

### Tool Schemas

All tools use JSON Schema for input validation. Example:

```typescript
{
  name: 'sql_query',
  description: 'Execute a read-only SELECT query',
  inputSchema: {
    type: 'object',
    properties: {
      connectionString: { type: 'string', description: '...' },
      database: { type: 'string', description: '...' },
      query: { type: 'string', description: '...' },
      maxRows: { type: 'number', default: 1000 }
    },
    required: ['connectionString', 'database', 'query']
  }
}
```

### Response Format

All tool responses follow this structure:

```typescript
{
  success: boolean;
  data?: any;
  error?: string;
  metadata?: {
    // Additional context
  }
}
```

## MCP Tools

### Tool 1: sql_list_databases

**Purpose**: List all databases on the server

**Implementation**: `src/tools/index.ts::listDatabases()`

**Database-Specific Queries**:
- PostgreSQL: Queries `pg_database` system catalog
- SQL Server: Queries `sys.databases` catalog view

**Security**: Read-only system catalog access

### Tool 2: sql_list_tables

**Purpose**: List all tables in a database with metadata

**Implementation**: `src/tools/index.ts::listTables()`

**Database-Specific Queries**:
- PostgreSQL: Queries `information_schema.tables` with size estimates
- SQL Server: Queries `sys.tables` with row counts and sizes

**Security**: Identifier sanitization, schema filtering

### Tool 3: sql_describe_table

**Purpose**: Get detailed schema information for a table

**Implementation**: `src/tools/index.ts::describeTable()`

**Returns**:
- Columns (name, type, nullable, default, PK/FK status)
- Indexes (name, columns, unique, primary)
- Foreign keys (name, columns, referenced table/columns)
- Constraints (name, type, definition)

**Database-Specific Queries**:
- PostgreSQL: Multiple queries to `information_schema` and `pg_catalog`
- SQL Server: Queries to `sys.columns`, `sys.indexes`, `sys.foreign_keys`, etc.

### Tool 4: sql_query

**Purpose**: Execute read-only SELECT query with validation

**Implementation**: `src/tools/index.ts::executeQuery()`

**Security Layers**:
1. `validateQueryOrThrow()`: Checks for blocked keywords and patterns
2. `enforceQueryLimits()`: Adds/enforces LIMIT clause (max 10,000 rows)
3. Database query timeout (30 seconds default)
4. Result set size limits

**Validation**:
- Must start with SELECT, WITH, or EXPLAIN
- Blocks INSERT, UPDATE, DELETE, DROP, CREATE, ALTER, etc.
- Blocks dangerous functions (pg_read_file, xp_cmdshell, etc.)
- Prevents multiple statements (semicolon detection)
- Detects SQL injection patterns

### Tool 5: sql_get_query_plan

**Purpose**: Get execution plan without executing query

**Implementation**: `src/tools/index.ts::getQueryPlan()`

**Database-Specific**:
- PostgreSQL: Uses `EXPLAIN (FORMAT JSON, ANALYZE false)`
- SQL Server: Uses `SET SHOWPLAN_XML ON/OFF`

**Security**: Same validation as sql_query, but doesn't execute data retrieval

## MCP Prompts

### Prompt 1: analyze-schema

**Purpose**: Analyze database schema and provide insights

**Arguments**:
- `database` (required): Database to analyze
- `focus` (optional): Specific focus area

**Prompt Template**: Guides AI to:
1. List tables and relationships
2. Analyze data integrity (PKs, FKs, constraints)
3. Review indexing strategy
4. Check naming conventions
5. Assess normalization
6. Identify potential issues
7. Provide recommendations

### Prompt 2: query-assistant

**Purpose**: Help construct SQL queries from natural language

**Arguments**:
- `database` (required): Target database
- `requirement` (required): Natural language description

**Prompt Template**: Guides AI to:
1. Understand table schemas
2. Construct appropriate SELECT query
3. Explain the query
4. Suggest optimizations
5. Follow read-only best practices

### Prompt 3: performance-review

**Purpose**: Review query performance and suggest optimizations

**Arguments**:
- `database` (required): Database name
- `query` (required): SQL query to analyze

**Prompt Template**: Guides AI to:
1. Get and analyze execution plan
2. Identify performance bottlenecks
3. Suggest index improvements
4. Recommend query rewrites
5. Provide estimated impact

## Read-Only Security

### Validation Layers

**Layer 1: Keyword Blocking**

Blocks these operations:
- Data modification: INSERT, UPDATE, DELETE, TRUNCATE, MERGE, UPSERT, REPLACE, COPY
- Schema modification: CREATE, ALTER, DROP, RENAME, COMMENT
- Permissions: GRANT, REVOKE
- Transaction control: BEGIN, COMMIT, ROLLBACK, SAVEPOINT
- Locking: LOCK, UNLOCK
- Maintenance: VACUUM, ANALYZE, REINDEX, CLUSTER, CHECKPOINT
- Configuration: SET, RESET
- Messaging: LISTEN, NOTIFY, UNLISTEN
- Procedural: DO, CALL, EXECUTE, DECLARE

**Layer 2: Function Blocking**

Blocks dangerous functions:
- PostgreSQL: pg_read_file, pg_read_binary_file, pg_execute, pg_terminate_backend, pg_sleep
- SQL Server: xp_cmdshell, sp_executesql, OPENROWSET, OPENDATASOURCE

**Layer 3: Pattern Detection**

Blocks dangerous patterns:
- Multiple statements (semicolons)
- SQL injection attempts
- Procedural code blocks ($$)
- INTO OUTFILE
- LOAD_FILE

**Layer 4: Input Sanitization**

Sanitizes identifiers:
- Removes non-alphanumeric characters (except underscore and dot)
- Prevents SQL injection in database/table/column names

**Layer 5: Query Limits**

Enforces limits:
- Maximum rows: 10,000 (configurable down to 1,000)
- Query timeout: 30 seconds (configurable)
- Query length: 50,000 characters max

### Validation Implementation

```typescript
// src/security/queryValidator.ts

export function validateQuery(query: string): ValidationResult {
  // 1. Check for empty query
  // 2. Check query length
  // 3. Normalize and check starting keyword
  // 4. Check for blocked keywords (regex-based)
  // 5. Check for blocked functions (regex-based)
  // 6. Check for dangerous patterns
  // 7. Check for multiple statements
  // 8. Generate warnings (missing LIMIT, SELECT *)

  return { isValid, errors, warnings };
}
```

## Configuration

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
- `host` / `server` / `data source`: Database server host (required)
- `port`: Port number (default: 5432 for PostgreSQL, 1433 for SQL Server)
- `user` / `username` / `user id` / `uid`: Database user (required)
- `password` / `pwd`: Database password (required)
- `database` / `initial catalog`: Database name (optional)
- `connectionTimeout` / `connect timeout`: Connection timeout in seconds
- `commandTimeout` / `request timeout`: Query timeout in seconds
- `ssl` / `encrypt`: Enable SSL/TLS (true/false)

### Environment Variables

- `LOG_LEVEL`: Logging level (debug, info, warn, error) - default: info
- `NODE_ENV`: Environment (development, production) - default: development

## Development Workflow

### Local Development Setup

```bash
# Install dependencies
npm install

# Build TypeScript
npm run build

# Run in development mode
npm run dev

# Watch mode (auto-rebuild)
npm run watch

# Run tests
npm test

# Run tests with coverage
npm run test:coverage

# Watch tests
npm run test:watch

# Lint code
npm run lint

# Format code
npm run format

# Type check
npm run typecheck
```

### Running Tests

```bash
# All tests
npm test

# Specific test file
npm test -- safety.test.ts

# Coverage report
npm run test:coverage

# Watch mode
npm run test:watch
```

### Code Quality

The project enforces code quality through:

**TypeScript**:
- Strict mode enabled
- ES2022 target
- NodeNext module resolution
- No unused locals/parameters
- No implicit returns
- Full type coverage

**ESLint**:
- TypeScript recommended rules
- Prettier integration
- No console.log (use logger)
- Explicit return types
- No explicit any

**Prettier**:
- Single quotes
- 2-space indentation
- 100-char line width
- Trailing commas (ES5)
- LF line endings

**Jest**:
- 80% coverage threshold
- ES modules support
- TypeScript integration

## Testing

### Test Categories

**Safety Tests** (`src/__tests__/safety.test.ts`):
- Query validation (valid/invalid queries)
- Blocked keyword detection
- SQL injection prevention
- Query limit enforcement
- Identifier sanitization
- 50+ test cases

**Database Tests** (`src/__tests__/database.test.ts`):
- Connection string parsing
- PostgreSQL configuration
- SQL Server configuration
- Validation and error handling
- Edge cases

**Prompt Tests** (`src/__tests__/prompts.test.ts`):
- All prompt templates
- Argument handling
- Error cases

### Writing Tests

Follow Jest conventions:

```typescript
describe('Feature', () => {
  describe('SubFeature', () => {
    test('should do something', () => {
      // Arrange
      const input = ...;

      // Act
      const result = functionUnderTest(input);

      // Assert
      expect(result).toBe(...);
    });
  });
});
```

### Coverage Goals

- **Overall**: >80%
- **Statements**: >80%
- **Branches**: >80%
- **Functions**: >80%
- **Lines**: >80%

## Comparison with Other MCP Servers

### vs postgres-mcp (.NET)

| Feature | adeotek-sql-mcp | postgres-mcp |
|---------|-----------------|--------------|
| Language | TypeScript/Node.js | C# .NET 10 |
| Databases | PostgreSQL + SQL Server | PostgreSQL only |
| Protocol | MCP 2025-11-25 (stdio) | MCP 2024-11-05 (HTTP + SSE) |
| Tools | 5 tools | 2 tools |
| Prompts | 3 prompts | 4 prompts |
| Transport | stdio only | HTTP + stdio |
| Resources | No | Yes |
| SSE Notifications | No | Yes |

### vs postgres-nl-mcp (.NET)

| Feature | adeotek-sql-mcp | postgres-nl-mcp |
|---------|-----------------|-----------------|
| Language | TypeScript/Node.js | C# .NET 9 |
| Databases | PostgreSQL + SQL Server | PostgreSQL only |
| AI Integration | No (prompts only) | Yes (Semantic Kernel) |
| Natural Language | Via prompts | Direct NL-to-SQL |
| Complexity | Lightweight | Advanced |
| Dependencies | Minimal | AI API required |

**When to use adeotek-sql-mcp**:
- Need both PostgreSQL and SQL Server support
- Prefer TypeScript/Node.js ecosystem
- Want lightweight server without AI dependencies
- Need stdio transport for MCP clients

**When to use postgres-mcp**:
- PostgreSQL only
- Need SSE notifications
- Prefer .NET ecosystem
- Want HTTP endpoint access

**When to use postgres-nl-mcp**:
- PostgreSQL only
- Need AI-powered natural language queries
- Want automatic relationship detection
- Have AI API keys or local LLM

## Error Handling

### Custom Error Classes

```typescript
// src/utils/errors.ts

class McpError extends Error
class DatabaseConnectionError extends McpError
class QueryValidationError extends McpError
class QueryExecutionError extends McpError
class ConfigurationError extends McpError
class TimeoutError extends McpError
class ToolNotFoundError extends McpError
class PromptNotFoundError extends McpError
```

### Error Handling Pattern

```typescript
try {
  // Execute operation
} catch (error) {
  logger.error('Operation failed', { error });
  const errorInfo = handleError(error);
  return {
    success: false,
    error: errorInfo.message,
    metadata: { errorCode: errorInfo.code }
  };
}
```

## Logging

### Logger Implementation

Uses Winston with:
- Structured JSON logging
- Timestamp in all logs
- Log levels: error, warn, info, debug, trace
- Sensitive data sanitization
- Development-friendly console output

### Sensitive Data Sanitization

Automatically redacts:
- password
- apiKey
- token
- secret
- authorization
- connectionString

### Logging Levels

- **error**: Failures and exceptions
- **warn**: Validation warnings, deprecated usage
- **info**: Operation starts/completions, connections
- **debug**: Detailed operation info (queries, results)
- **trace**: Very verbose (disabled in production)

## Performance Considerations

- **Connection Pooling**: Reuses connections (pg: 10 max, mssql: 10 max)
- **Query Timeouts**: Prevents long-running queries (30s default)
- **Row Limits**: Automatic LIMIT enforcement (max 10,000)
- **Async/Await**: All I/O operations are asynchronous
- **Efficient Queries**: Database-specific optimized queries
- **Minimal Dependencies**: Small package footprint

## Troubleshooting

### Common Issues

**Issue**: Connection string parsing fails

**Solution**: Check format matches `key=value;key=value` pattern, ensure required fields (type, host, user, password)

**Issue**: Query validation fails

**Solution**: Ensure query starts with SELECT/WITH/EXPLAIN, remove modification keywords, check for dangerous patterns

**Issue**: Query timeout

**Solution**: Optimize query, add indexes, use WHERE clauses, add LIMIT

**Issue**: Tests fail on import

**Solution**: Ensure `type: "module"` in package.json, use `.js` extensions in imports

### Debug Mode

Set `LOG_LEVEL=debug` for detailed logging:

```bash
LOG_LEVEL=debug npm run dev
```

## Working with Claude

### When Adding Features

1. **Read existing code**: Use Read tool to understand implementation
2. **Follow TypeScript patterns**: Use modern ES2022+ features
3. **Update tests**: Add comprehensive test coverage
4. **Update documentation**: Update README.md and this CLAUDE.md
5. **Test thoroughly**: Run `npm test` and `npm run build`

### When Debugging

1. **Check logs**: Look at Winston output
2. **Run tests**: `npm test` to identify failures
3. **Type check**: `npm run typecheck` for type errors
4. **Review error classes**: Check `src/utils/errors.ts` for error types
5. **Enable debug logging**: Set `LOG_LEVEL=debug`

### When Refactoring

1. **Maintain MCP compliance**: Don't break tool/prompt APIs
2. **Update tests**: Ensure all tests pass
3. **Follow patterns**: Maintain consistency with existing code
4. **Document changes**: Update JSDoc comments and markdown docs
5. **Preserve security**: Don't compromise read-only safeguards

## Code Quality Standards

### TypeScript Standards

- Use strict mode
- Explicit return types on functions
- No `any` type (use `unknown` if needed)
- Use interfaces for object shapes
- Use type aliases for unions
- Prefer const over let
- Use async/await over promises
- Use template literals over concatenation

### Code Organization

- One class/interface per file
- Group related functionality
- Keep files under 500 lines
- Use barrel exports (index.ts)
- Separate concerns (database, security, tools)

### Documentation

- JSDoc comments for all public functions
- Include @param and @returns
- Document complex logic inline
- Keep README.md and CLAUDE.md updated

### Security-First Development

- **Always validate queries**: Every query through validateQueryOrThrow
- **Multiple validation layers**: Don't rely on single check
- **Test security**: Write tests for blocked operations
- **Document security**: Explain why operations are blocked
- **Never trust input**: Sanitize all user input

## Future Enhancements

- [ ] MySQL support
- [ ] SQLite support
- [ ] Connection pooling optimization
- [ ] Query result caching
- [ ] Streaming for large result sets
- [ ] WebSocket transport (in addition to stdio)
- [ ] Query history and auditing
- [ ] Advanced schema analysis
- [ ] Cost estimation before query execution
- [ ] Real-time query monitoring
- [ ] Multi-database query aggregation
- [ ] GraphQL endpoint

## Related Documentation

- **Main Repository Context**: `/CLAUDE.md` - Repository-wide guidelines
- **User Documentation**: `README.md` - User-facing documentation
- **MCP Specification**: https://modelcontextprotocol.io/
- **Related Projects**:
  - `/mcp-servers/postgres-mcp/CLAUDE.md` - .NET PostgreSQL MCP server
  - `/mcp-servers/postgres-nl-mcp/CLAUDE.md` - .NET PostgreSQL with AI

## Questions for Claude

When working on adeotek-sql-mcp, you can ask:

**Architecture and Implementation**:
- "How does query validation work?"
- "What operations are blocked and why?"
- "How do I add support for a new database?"
- "How does connection pooling work?"
- "How are prompts different from tools?"

**Database-Specific**:
- "How does the PostgreSQL implementation differ from SQL Server?"
- "What system catalogs are queried?"
- "How are execution plans retrieved?"

**Security**:
- "How is SQL injection prevented?"
- "What are the validation layers?"
- "How do I add a new blocked keyword?"
- "How is sensitive data sanitized in logs?"

**Testing and Development**:
- "How do I write tests for a new tool?"
- "How do I test with a real database?"
- "How do I debug connection issues?"
- "How do I add a new MCP tool?"

Claude has full context from this document and can help with development, debugging, and architecture decisions for the Adeotek SQL MCP Server project.
