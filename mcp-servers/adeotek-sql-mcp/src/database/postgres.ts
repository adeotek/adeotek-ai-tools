/**
 * PostgreSQL database operations
 */

import pg from 'pg';
import type {
  DatabaseConfig,
  DatabaseInfo,
  TableInfo,
  TableSchema,
  QueryResult,
  QueryPlan,
  ColumnInfo,
  IndexInfo,
  ForeignKeyInfo,
  ConstraintInfo,
} from '../types/index.js';
import { DatabaseConnectionError, QueryExecutionError, TimeoutError } from '../utils/errors.js';
import logger from '../utils/logger.js';

const { Pool } = pg;

export class PostgresDatabase {
  private pool: pg.Pool | null = null;
  private config: DatabaseConfig;
  private temporaryPools: Map<string, pg.Pool> = new Map();

  constructor(config: DatabaseConfig) {
    this.config = config;
  }

  /**
   * Initialize connection pool
   */
  async connect(): Promise<void> {
    try {
      logger.debug('Attempting PostgreSQL connection', {
        host: this.config.host,
        port: this.config.port,
        user: this.config.user,
        database: this.config.database,
        ssl: this.config.ssl,
      });

      this.pool = new Pool({
        host: this.config.host,
        port: this.config.port,
        user: this.config.user,
        password: this.config.password,
        database: this.config.database,
        connectionTimeoutMillis: (this.config.connectionTimeout || 30) * 1000,
        max: 10,
        idleTimeoutMillis: 30000,
        ssl: this.config.ssl ? { rejectUnauthorized: false } : false,
      });

      // Test connection
      logger.debug('Testing PostgreSQL connection...');
      const client = await this.pool.connect();
      await client.query('SELECT 1');
      client.release();

      logger.info('PostgreSQL connection established', {
        host: this.config.host,
        port: this.config.port,
        database: this.config.database,
      });
    } catch (error) {
      const errorMessage =
        error instanceof Error ? error.message : String(error);
      const errorCode = (error as any)?.code;
      const errorDetail = (error as any)?.detail;

      logger.error('PostgreSQL connection failed', {
        host: this.config.host,
        port: this.config.port,
        user: this.config.user,
        database: this.config.database,
        errorMessage,
        errorCode,
        errorDetail,
        error,
      });

      throw new DatabaseConnectionError(
        `Failed to connect to PostgreSQL at ${this.config.host}:${this.config.port}: ${errorMessage}${errorCode ? ` (code: ${errorCode})` : ''}`,
        'postgres'
      );
    }
  }

  /**
   * Close connection pool
   */
  async disconnect(): Promise<void> {
    // Close all temporary pools
    for (const [dbName, pool] of this.temporaryPools.entries()) {
      try {
        await pool.end();
        logger.debug(`Temporary pool for database '${dbName}' closed`);
      } catch (error) {
        logger.error(`Error closing temporary pool for '${dbName}'`, { error });
      }
    }
    this.temporaryPools.clear();

    // Close main pool
    if (this.pool) {
      await this.pool.end();
      this.pool = null;
      logger.info('PostgreSQL connection closed');
    }
  }

  /**
   * Get or create a connection pool for a specific database
   */
  private async getPoolForDatabase(database: string): Promise<pg.Pool> {
    // If querying the same database as our main connection, use main pool
    if (database === this.config.database) {
      return this.pool!;
    }

    // Check if we already have a temporary pool for this database
    if (this.temporaryPools.has(database)) {
      return this.temporaryPools.get(database)!;
    }

    // Create a new temporary pool for this database
    logger.debug(`Creating temporary pool for database '${database}'`);
    const tempPool = new Pool({
      host: this.config.host,
      port: this.config.port,
      user: this.config.user,
      password: this.config.password,
      database: database, // Connect to the target database
      connectionTimeoutMillis: (this.config.connectionTimeout || 30) * 1000,
      max: 5, // Smaller pool for temporary connections
      idleTimeoutMillis: 30000,
      ssl: this.config.ssl ? { rejectUnauthorized: false } : false,
    });

    // Test the connection
    try {
      const client = await tempPool.connect();
      await client.query('SELECT 1');
      client.release();
      logger.debug(`Temporary pool for database '${database}' established`);
    } catch (error) {
      await tempPool.end();
      throw new DatabaseConnectionError(
        `Failed to connect to database '${database}': ${error instanceof Error ? error.message : String(error)}`,
        'postgres'
      );
    }

    this.temporaryPools.set(database, tempPool);
    return tempPool;
  }

  /**
   * List all databases
   */
  async listDatabases(): Promise<DatabaseInfo[]> {
    this.ensureConnected();

    const query = `
      SELECT
        datname as name,
        pg_size_pretty(pg_database_size(datname)) as size,
        pg_catalog.pg_get_userbyid(datdba) as owner,
        datcollate as encoding
      FROM pg_database
      WHERE datistemplate = false
      ORDER BY datname;
    `;

    try {
      const result = await this.pool!.query(query);
      return result.rows.map((row) => ({
        name: row.name,
        size: row.size,
        owner: row.owner,
        encoding: row.encoding,
      }));
    } catch (error) {
      throw new QueryExecutionError(
        `Failed to list databases: ${error instanceof Error ? error.message : String(error)}`,
        query
      );
    }
  }

  /**
   * List all tables in a database
   */
  async listTables(database: string, schema?: string): Promise<TableInfo[]> {
    this.ensureConnected();

    // Get the appropriate pool for the target database
    const pool = await this.getPoolForDatabase(database);

    // Build query based on whether schema is specified
    let query: string;
    let params: any[];

    if (schema) {
      // List tables from specific schema
      query = `
        SELECT
          table_schema as schema,
          table_name as name,
          table_type as type,
          pg_relation_size(quote_ident(table_schema) || '.' || quote_ident(table_name)) as size
        FROM information_schema.tables
        WHERE table_schema = $1
        ORDER BY table_name;
      `;
      params = [schema];
    } else {
      // List tables from all user schemas (exclude system schemas)
      query = `
        SELECT
          table_schema as schema,
          table_name as name,
          table_type as type,
          pg_relation_size(quote_ident(table_schema) || '.' || quote_ident(table_name)) as size
        FROM information_schema.tables
        WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
        ORDER BY table_schema, table_name;
      `;
      params = [];
    }

    try {
      const result = await pool.query(query, params);
      return result.rows.map((row) => ({
        schema: row.schema,
        name: row.name,
        type: row.type === 'BASE TABLE' ? 'table' : 'view',
        sizeEstimate: row.size ? `${Math.round(row.size / 1024)} KB` : undefined,
      }));
    } catch (error) {
      throw new QueryExecutionError(
        `Failed to list tables in database '${database}': ${error instanceof Error ? error.message : String(error)}`,
        query
      );
    }
  }

  /**
   * Get detailed schema information for a table
   */
  async describeTable(
    database: string,
    tableName: string,
    schema: string = 'public'
  ): Promise<TableSchema> {
    this.ensureConnected();

    // Get the appropriate pool for the target database
    const pool = await this.getPoolForDatabase(database);

    const [columns, indexes, foreignKeys, constraints] = await Promise.all([
      this.getColumns(pool, tableName, schema),
      this.getIndexes(pool, tableName, schema),
      this.getForeignKeys(pool, tableName, schema),
      this.getConstraints(pool, tableName, schema),
    ]);

    const primaryKey = columns.filter((col) => col.isPrimaryKey).map((col) => col.name);

    return {
      schema,
      table: tableName,
      columns,
      indexes,
      foreignKeys,
      constraints,
      primaryKey: primaryKey.length > 0 ? primaryKey : undefined,
    };
  }

  /**
   * Execute a SELECT query
   */
  async executeQuery(
    database: string,
    query: string,
    timeout: number = 30
  ): Promise<QueryResult> {
    this.ensureConnected();

    // Get the appropriate pool for the target database
    const pool = await this.getPoolForDatabase(database);

    const startTime = Date.now();

    try {
      // Set statement timeout
      await pool.query(`SET statement_timeout = ${timeout * 1000}`);

      const result = await pool.query(query);
      const executionTimeMs = Date.now() - startTime;

      return {
        columns: result.fields.map((field) => field.name),
        rows: result.rows,
        rowCount: result.rowCount || 0,
        executionTimeMs,
      };
    } catch (error) {
      if (error instanceof Error && error.message.includes('timeout')) {
        throw new TimeoutError(`Query execution timeout after ${timeout}s`, timeout * 1000);
      }
      throw new QueryExecutionError(
        `Failed to execute query in database '${database}': ${error instanceof Error ? error.message : String(error)}`,
        query
      );
    } finally {
      // Reset timeout
      await pool.query('RESET statement_timeout').catch(() => {
        /* ignore */
      });
    }
  }

  /**
   * Get query execution plan
   */
  async getQueryPlan(database: string, query: string): Promise<QueryPlan> {
    this.ensureConnected();

    // Get the appropriate pool for the target database
    const pool = await this.getPoolForDatabase(database);

    const explainQuery = `EXPLAIN (FORMAT JSON, ANALYZE false) ${query}`;

    try {
      const result = await pool.query(explainQuery);
      const planJson = result.rows[0]['QUERY PLAN'];

      return {
        plan: JSON.stringify(planJson, null, 2),
        format: 'json',
        estimatedCost: planJson[0]?.Plan?.['Total Cost'],
      };
    } catch (error) {
      throw new QueryExecutionError(
        `Failed to get query plan in database '${database}': ${error instanceof Error ? error.message : String(error)}`,
        explainQuery
      );
    }
  }

  // Private helper methods

  private ensureConnected(): void {
    if (!this.pool) {
      throw new DatabaseConnectionError('Not connected to PostgreSQL database', 'postgres');
    }
  }

  private async getColumns(
    pool: pg.Pool,
    tableName: string,
    schema: string
  ): Promise<ColumnInfo[]> {
    const query = `
      SELECT
        c.column_name as name,
        c.data_type as type,
        c.is_nullable = 'YES' as nullable,
        c.column_default as default_value,
        c.character_maximum_length as max_length,
        c.numeric_precision as precision,
        c.numeric_scale as scale,
        CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key,
        CASE WHEN fk.column_name IS NOT NULL THEN true ELSE false END as is_foreign_key
      FROM information_schema.columns c
      LEFT JOIN (
        SELECT ku.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage ku
          ON tc.constraint_name = ku.constraint_name
        WHERE tc.constraint_type = 'PRIMARY KEY'
          AND tc.table_schema = $1
          AND tc.table_name = $2
      ) pk ON c.column_name = pk.column_name
      LEFT JOIN (
        SELECT ku.column_name
        FROM information_schema.table_constraints tc
        JOIN information_schema.key_column_usage ku
          ON tc.constraint_name = ku.constraint_name
        WHERE tc.constraint_type = 'FOREIGN KEY'
          AND tc.table_schema = $1
          AND tc.table_name = $2
      ) fk ON c.column_name = fk.column_name
      WHERE c.table_schema = $1
        AND c.table_name = $2
      ORDER BY c.ordinal_position;
    `;

    const result = await pool.query(query, [schema, tableName]);
    return result.rows.map((row) => ({
      name: row.name,
      type: row.type,
      nullable: row.nullable,
      defaultValue: row.default_value,
      isPrimaryKey: row.is_primary_key,
      isForeignKey: row.is_foreign_key,
      maxLength: row.max_length,
      precision: row.precision,
      scale: row.scale,
    }));
  }

  private async getIndexes(
    pool: pg.Pool,
    tableName: string,
    schema: string
  ): Promise<IndexInfo[]> {
    const query = `
      SELECT
        i.relname as index_name,
        array_agg(a.attname ORDER BY a.attnum) as column_names,
        ix.indisunique as is_unique,
        ix.indisprimary as is_primary,
        am.amname as type
      FROM pg_index ix
      JOIN pg_class t ON t.oid = ix.indrelid
      JOIN pg_class i ON i.oid = ix.indexrelid
      JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = ANY(ix.indkey)
      JOIN pg_namespace n ON n.oid = t.relnamespace
      JOIN pg_am am ON am.oid = i.relam
      WHERE n.nspname = $1
        AND t.relname = $2
      GROUP BY i.relname, ix.indisunique, ix.indisprimary, am.amname
      ORDER BY i.relname;
    `;

    const result = await pool.query(query, [schema, tableName]);
    return result.rows.map((row) => ({
      name: row.index_name,
      columns: row.column_names,
      isUnique: row.is_unique,
      isPrimary: row.is_primary,
      type: row.type,
    }));
  }

  private async getForeignKeys(
    pool: pg.Pool,
    tableName: string,
    schema: string
  ): Promise<ForeignKeyInfo[]> {
    const query = `
      SELECT
        tc.constraint_name as name,
        array_agg(kcu.column_name ORDER BY kcu.ordinal_position) as columns,
        ccu.table_name as referenced_table,
        ccu.table_schema as referenced_schema,
        array_agg(ccu.column_name ORDER BY kcu.ordinal_position) as referenced_columns,
        rc.update_rule as on_update,
        rc.delete_rule as on_delete
      FROM information_schema.table_constraints tc
      JOIN information_schema.key_column_usage kcu
        ON tc.constraint_name = kcu.constraint_name
      JOIN information_schema.constraint_column_usage ccu
        ON ccu.constraint_name = tc.constraint_name
      JOIN information_schema.referential_constraints rc
        ON rc.constraint_name = tc.constraint_name
      WHERE tc.constraint_type = 'FOREIGN KEY'
        AND tc.table_schema = $1
        AND tc.table_name = $2
      GROUP BY tc.constraint_name, ccu.table_name, ccu.table_schema, rc.update_rule, rc.delete_rule
      ORDER BY tc.constraint_name;
    `;

    const result = await pool.query(query, [schema, tableName]);
    return result.rows.map((row) => ({
      name: row.name,
      columns: row.columns,
      referencedTable: row.referenced_table,
      referencedSchema: row.referenced_schema,
      referencedColumns: row.referenced_columns,
      onUpdate: row.on_update,
      onDelete: row.on_delete,
    }));
  }

  private async getConstraints(
    pool: pg.Pool,
    tableName: string,
    schema: string
  ): Promise<ConstraintInfo[]> {
    const query = `
      SELECT
        tc.constraint_name as name,
        tc.constraint_type as type,
        pg_get_constraintdef(c.oid) as definition
      FROM information_schema.table_constraints tc
      JOIN pg_namespace n ON n.nspname = tc.table_schema
      JOIN pg_class t ON t.relname = tc.table_name AND t.relnamespace = n.oid
      JOIN pg_constraint c ON c.conname = tc.constraint_name AND c.conrelid = t.oid
      WHERE tc.table_schema = $1
        AND tc.table_name = $2
      ORDER BY tc.constraint_type, tc.constraint_name;
    `;

    const result = await pool.query(query, [schema, tableName]);
    return result.rows.map((row) => ({
      name: row.name,
      type: row.type as ConstraintInfo['type'],
      definition: row.definition,
    }));
  }
}
