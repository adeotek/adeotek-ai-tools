/**
 * MCP Tools Implementation
 */

import type { DatabaseConnection } from '../database/connection.js';
import { validateQueryOrThrow, enforceQueryLimits, sanitizeIdentifier } from '../security/queryValidator.js';
import type { ToolResponse, DatabaseInfo, TableInfo, TableSchema, QueryResult, QueryPlan } from '../types/index.js';
import { handleError } from '../utils/errors.js';
import logger from '../utils/logger.js';

/**
 * Tool: sql_list_databases
 * List all databases available on the configured server
 */
export async function listDatabases(
  connection: DatabaseConnection
): Promise<ToolResponse<DatabaseInfo[]>> {
  try {
    logger.info('Executing sql_list_databases tool');

    const databases = await connection.listDatabases();

    return {
      success: true,
      data: databases,
      metadata: {
        count: databases.length,
        timestamp: new Date().toISOString(),
      },
    };
  } catch (error) {
    logger.error('Error listing databases', { error });
    const errorInfo = handleError(error);
    return {
      success: false,
      error: errorInfo.message,
      metadata: {
        errorCode: errorInfo.code,
      },
    };
  }
}

/**
 * Tool: sql_list_tables
 * List all tables in a specified database
 */
export async function listTables(
  connection: DatabaseConnection,
  database: string,
  schema?: string
): Promise<ToolResponse<TableInfo[]>> {
  try {
    logger.info('Executing sql_list_tables tool', { database, schema });

    // Sanitize inputs
    const sanitizedDatabase = sanitizeIdentifier(database);
    const sanitizedSchema = schema ? sanitizeIdentifier(schema) : undefined;

    const tables = await connection.listTables(sanitizedDatabase, sanitizedSchema);

    return {
      success: true,
      data: tables,
      metadata: {
        database: sanitizedDatabase,
        schema: sanitizedSchema,
        count: tables.length,
        timestamp: new Date().toISOString(),
      },
    };
  } catch (error) {
    logger.error('Error listing tables', { error, database, schema });
    const errorInfo = handleError(error);
    return {
      success: false,
      error: errorInfo.message,
      metadata: {
        errorCode: errorInfo.code,
      },
    };
  }
}

/**
 * Tool: sql_describe_table
 * Get detailed schema information for a specific table
 */
export async function describeTable(
  connection: DatabaseConnection,
  database: string,
  table: string,
  schema?: string
): Promise<ToolResponse<TableSchema>> {
  try {
    logger.info('Executing sql_describe_table tool', { database, table, schema });

    // Sanitize inputs
    const sanitizedDatabase = sanitizeIdentifier(database);
    const sanitizedTable = sanitizeIdentifier(table);
    const sanitizedSchema = schema ? sanitizeIdentifier(schema) : undefined;

    const tableSchema = await connection.describeTable(
      sanitizedDatabase,
      sanitizedTable,
      sanitizedSchema
    );

    return {
      success: true,
      data: tableSchema,
      metadata: {
        database: sanitizedDatabase,
        table: sanitizedTable,
        schema: sanitizedSchema || tableSchema.schema,
        columnCount: tableSchema.columns.length,
        indexCount: tableSchema.indexes.length,
        foreignKeyCount: tableSchema.foreignKeys.length,
        timestamp: new Date().toISOString(),
      },
    };
  } catch (error) {
    logger.error('Error describing table', { error, database, table, schema });
    const errorInfo = handleError(error);
    return {
      success: false,
      error: errorInfo.message,
      metadata: {
        errorCode: errorInfo.code,
      },
    };
  }
}

/**
 * Tool: sql_query
 * Execute a read-only SELECT query
 */
export async function executeQuery(
  connection: DatabaseConnection,
  database: string,
  query: string,
  maxRows: number = 1000
): Promise<ToolResponse<QueryResult>> {
  try {
    logger.info('Executing sql_query tool', { database, queryPreview: query.substring(0, 100) });

    // Validate query for read-only compliance
    validateQueryOrThrow(query);

    // Enforce row limits
    const limitedQuery = enforceQueryLimits(query, Math.min(maxRows, 10000));
    const finalQuery = limitedQuery.query;

    // Execute query
    const result = await connection.executeQuery(finalQuery);

    return {
      success: true,
      data: result,
      metadata: {
        database,
        rowCount: result.rowCount,
        executionTimeMs: result.executionTimeMs,
        limitApplied: limitedQuery.limitApplied,
        maxRows: Math.min(maxRows, 10000),
        timestamp: new Date().toISOString(),
      },
    };
  } catch (error) {
    logger.error('Error executing query', { error, database });
    const errorInfo = handleError(error);
    return {
      success: false,
      error: errorInfo.message,
      metadata: {
        errorCode: errorInfo.code,
      },
    };
  }
}

/**
 * Tool: sql_get_query_plan
 * Get the execution plan for a query without executing it
 */
export async function getQueryPlan(
  connection: DatabaseConnection,
  database: string,
  query: string
): Promise<ToolResponse<QueryPlan>> {
  try {
    logger.info('Executing sql_get_query_plan tool', { database, queryPreview: query.substring(0, 100) });

    // Validate query for read-only compliance
    validateQueryOrThrow(query);

    // Get query plan
    const plan = await connection.getQueryPlan(query);

    return {
      success: true,
      data: plan,
      metadata: {
        database,
        format: plan.format,
        timestamp: new Date().toISOString(),
      },
    };
  } catch (error) {
    logger.error('Error getting query plan', { error, database });
    const errorInfo = handleError(error);
    return {
      success: false,
      error: errorInfo.message,
      metadata: {
        errorCode: errorInfo.code,
      },
    };
  }
}
