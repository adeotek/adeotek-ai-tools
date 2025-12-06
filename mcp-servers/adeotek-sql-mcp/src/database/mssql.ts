/**
 * Microsoft SQL Server database operations
 */

import sql from 'mssql';
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

export class MssqlDatabase {
  private pool: sql.ConnectionPool | null = null;
  private config: DatabaseConfig;

  constructor(config: DatabaseConfig) {
    this.config = config;
  }

  /**
   * Initialize connection pool
   */
  async connect(): Promise<void> {
    try {
      const sqlConfig: sql.config = {
        server: this.config.host,
        port: this.config.port,
        user: this.config.user,
        password: this.config.password,
        database: this.config.database,
        options: {
          encrypt: this.config.ssl || false,
          trustServerCertificate: true,
          connectTimeout: (this.config.connectionTimeout || 30) * 1000,
          requestTimeout: (this.config.requestTimeout || 30) * 1000,
        },
        pool: {
          max: 10,
          min: 0,
          idleTimeoutMillis: 30000,
        },
      };

      this.pool = await sql.connect(sqlConfig);

      logger.info('SQL Server connection established', {
        host: this.config.host,
        port: this.config.port,
        database: this.config.database,
      });
    } catch (error) {
      throw new DatabaseConnectionError(
        `Failed to connect to SQL Server: ${error instanceof Error ? error.message : String(error)}`,
        'mssql'
      );
    }
  }

  /**
   * Close connection pool
   */
  async disconnect(): Promise<void> {
    if (this.pool) {
      await this.pool.close();
      this.pool = null;
      logger.info('SQL Server connection closed');
    }
  }

  /**
   * List all databases
   */
  async listDatabases(): Promise<DatabaseInfo[]> {
    this.ensureConnected();

    const query = `
      SELECT
        name,
        database_id,
        create_date as created,
        SUSER_SNAME(owner_sid) as owner,
        collation_name as encoding
      FROM sys.databases
      WHERE name NOT IN ('master', 'tempdb', 'model', 'msdb')
      ORDER BY name;
    `;

    try {
      const result = await this.pool!.request().query(query);
      return result.recordset.map((row) => ({
        name: row.name,
        owner: row.owner,
        created: row.created,
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
  async listTables(database: string, schema: string = 'dbo'): Promise<TableInfo[]> {
    this.ensureConnected();

    const query = `
      USE [${database}];
      SELECT
        s.name as [schema],
        t.name as name,
        t.type_desc as type,
        p.rows as row_count,
        CAST(ROUND(((SUM(a.total_pages) * 8) / 1024.00), 2) AS NUMERIC(36, 2)) AS size_mb
      FROM sys.tables t
      INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
      INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
      INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
      LEFT JOIN sys.schemas s ON t.schema_id = s.schema_id
      WHERE s.name = '${schema}'
        AND t.is_ms_shipped = 0
        AND i.OBJECT_ID > 255
      GROUP BY s.name, t.name, t.type_desc, p.Rows
      ORDER BY t.name;
    `;

    try {
      const result = await this.pool!.request().query(query);
      return result.recordset.map((row) => ({
        schema: row.schema,
        name: row.name,
        type: row.type.toLowerCase().includes('view') ? 'view' : 'table',
        rowCount: row.row_count,
        sizeEstimate: `${row.size_mb} MB`,
      }));
    } catch (error) {
      throw new QueryExecutionError(
        `Failed to list tables: ${error instanceof Error ? error.message : String(error)}`,
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
    schema: string = 'dbo'
  ): Promise<TableSchema> {
    this.ensureConnected();

    const [columns, indexes, foreignKeys, constraints] = await Promise.all([
      this.getColumns(database, tableName, schema),
      this.getIndexes(database, tableName, schema),
      this.getForeignKeys(database, tableName, schema),
      this.getConstraints(database, tableName, schema),
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
  async executeQuery(query: string, _timeout: number = 30): Promise<QueryResult> {
    this.ensureConnected();

    const startTime = Date.now();

    try {
      const request = this.pool!.request();
      // Note: timeout is configured at connection level in config.options.requestTimeout

      const result = await request.query(query);
      const executionTimeMs = Date.now() - startTime;

      const columns = result.recordset.columns
        ? Object.keys(result.recordset.columns)
        : result.recordset.length > 0
          ? Object.keys(result.recordset[0])
          : [];

      return {
        columns,
        rows: result.recordset,
        rowCount: result.recordset.length,
        executionTimeMs,
      };
    } catch (error) {
      if (error instanceof Error && error.message.includes('Timeout')) {
        throw new TimeoutError(`Query execution timeout after ${_timeout}s`, _timeout * 1000);
      }
      throw new QueryExecutionError(
        `Failed to execute query: ${error instanceof Error ? error.message : String(error)}`,
        query
      );
    }
  }

  /**
   * Get query execution plan
   */
  async getQueryPlan(query: string): Promise<QueryPlan> {
    this.ensureConnected();

    try {
      // Enable SHOWPLAN_XML
      await this.pool!.request().query('SET SHOWPLAN_XML ON');

      const result = await this.pool!.request().query(query);
      const plan = result.recordset[0]['Microsoft SQL Server 2005 XML Showplan'];

      // Disable SHOWPLAN_XML
      await this.pool!.request().query('SET SHOWPLAN_XML OFF');

      return {
        plan,
        format: 'xml',
      };
    } catch (error) {
      // Make sure to disable SHOWPLAN_XML
      await this.pool!.request().query('SET SHOWPLAN_XML OFF').catch(() => {
        /* ignore */
      });

      throw new QueryExecutionError(
        `Failed to get query plan: ${error instanceof Error ? error.message : String(error)}`,
        query
      );
    }
  }

  // Private helper methods

  private ensureConnected(): void {
    if (!this.pool) {
      throw new DatabaseConnectionError('Not connected to SQL Server database', 'mssql');
    }
  }

  private async getColumns(
    database: string,
    tableName: string,
    schema: string
  ): Promise<ColumnInfo[]> {
    const query = `
      USE [${database}];
      SELECT
        c.name,
        t.name as type,
        c.is_nullable as nullable,
        dc.definition as default_value,
        c.max_length,
        c.precision,
        c.scale,
        CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END as is_primary_key,
        CASE WHEN fk.parent_column_id IS NOT NULL THEN 1 ELSE 0 END as is_foreign_key
      FROM sys.columns c
      INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
      LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
      LEFT JOIN (
        SELECT ic.object_id, ic.column_id
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
        WHERE i.is_primary_key = 1
      ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
      LEFT JOIN sys.foreign_key_columns fk ON c.object_id = fk.parent_object_id AND c.column_id = fk.parent_column_id
      WHERE c.object_id = OBJECT_ID('[${schema}].[${tableName}]')
      ORDER BY c.column_id;
    `;

    const result = await this.pool!.request().query(query);
    return result.recordset.map((row) => ({
      name: row.name,
      type: row.type,
      nullable: row.nullable,
      defaultValue: row.default_value,
      isPrimaryKey: row.is_primary_key === 1,
      isForeignKey: row.is_foreign_key === 1,
      maxLength: row.max_length,
      precision: row.precision,
      scale: row.scale,
    }));
  }

  private async getIndexes(
    database: string,
    tableName: string,
    schema: string
  ): Promise<IndexInfo[]> {
    const query = `
      USE [${database}];
      SELECT
        i.name as index_name,
        STRING_AGG(c.name, ', ') as column_names,
        i.is_unique,
        i.is_primary_key,
        i.type_desc as type
      FROM sys.indexes i
      INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
      INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
      WHERE i.object_id = OBJECT_ID('[${schema}].[${tableName}]')
      GROUP BY i.name, i.is_unique, i.is_primary_key, i.type_desc
      ORDER BY i.name;
    `;

    const result = await this.pool!.request().query(query);
    return result.recordset.map((row) => ({
      name: row.index_name,
      columns: row.column_names ? row.column_names.split(', ') : [],
      isUnique: row.is_unique,
      isPrimary: row.is_primary_key,
      type: row.type,
    }));
  }

  private async getForeignKeys(
    database: string,
    tableName: string,
    schema: string
  ): Promise<ForeignKeyInfo[]> {
    const query = `
      USE [${database}];
      SELECT
        fk.name,
        STRING_AGG(c.name, ', ') as columns,
        OBJECT_NAME(fk.referenced_object_id) as referenced_table,
        OBJECT_SCHEMA_NAME(fk.referenced_object_id) as referenced_schema,
        STRING_AGG(rc.name, ', ') as referenced_columns,
        fk.delete_referential_action_desc as on_delete,
        fk.update_referential_action_desc as on_update
      FROM sys.foreign_keys fk
      INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
      INNER JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
      INNER JOIN sys.columns rc ON fkc.referenced_object_id = rc.object_id AND fkc.referenced_column_id = rc.column_id
      WHERE fk.parent_object_id = OBJECT_ID('[${schema}].[${tableName}]')
      GROUP BY fk.name, fk.referenced_object_id, fk.delete_referential_action_desc, fk.update_referential_action_desc
      ORDER BY fk.name;
    `;

    const result = await this.pool!.request().query(query);
    return result.recordset.map((row) => ({
      name: row.name,
      columns: row.columns ? row.columns.split(', ') : [],
      referencedTable: row.referenced_table,
      referencedSchema: row.referenced_schema,
      referencedColumns: row.referenced_columns ? row.referenced_columns.split(', ') : [],
      onDelete: row.on_delete,
      onUpdate: row.on_update,
    }));
  }

  private async getConstraints(
    database: string,
    tableName: string,
    schema: string
  ): Promise<ConstraintInfo[]> {
    const query = `
      USE [${database}];
      SELECT
        name,
        type_desc as type,
        OBJECT_DEFINITION(object_id) as definition
      FROM sys.objects
      WHERE parent_object_id = OBJECT_ID('[${schema}].[${tableName}]')
        AND type IN ('C', 'D', 'F', 'PK', 'UQ')
      ORDER BY type_desc, name;
    `;

    const result = await this.pool!.request().query(query);
    return result.recordset.map((row) => {
      let type: ConstraintInfo['type'];
      switch (row.type) {
        case 'PRIMARY_KEY_CONSTRAINT':
          type = 'PRIMARY KEY';
          break;
        case 'FOREIGN_KEY_CONSTRAINT':
          type = 'FOREIGN KEY';
          break;
        case 'UNIQUE_CONSTRAINT':
          type = 'UNIQUE';
          break;
        case 'CHECK_CONSTRAINT':
          type = 'CHECK';
          break;
        case 'DEFAULT_CONSTRAINT':
          type = 'DEFAULT';
          break;
        default:
          type = 'CHECK';
      }

      return {
        name: row.name,
        type,
        definition: row.definition || row.type,
      };
    });
  }
}
