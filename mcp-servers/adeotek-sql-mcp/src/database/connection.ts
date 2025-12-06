/**
 * Database connection factory and management
 */

import type { DatabaseConfig } from '../types/index.js';
import { PostgresDatabase } from './postgres.js';
import { MssqlDatabase } from './mssql.js';
import { ConfigurationError } from '../utils/errors.js';
import logger from '../utils/logger.js';

export type DatabaseConnection = PostgresDatabase | MssqlDatabase;

/**
 * Parse connection string into DatabaseConfig
 */
export function parseConnectionString(connectionString: string): DatabaseConfig {
  const config: Partial<DatabaseConfig> = {};

  // Try parsing as connection string format
  const params = connectionString.split(';').filter((p) => p.trim().length > 0);

  for (const param of params) {
    const [key, value] = param.split('=').map((s) => s.trim());
    if (!key || !value) continue;

    const lowerKey = key.toLowerCase();

    switch (lowerKey) {
      case 'type':
      case 'dbtype':
        if (value === 'mssql' || value === 'postgres') {
          config.type = value;
        }
        break;
      case 'host':
      case 'server':
      case 'data source':
        config.host = value;
        break;
      case 'port':
        config.port = parseInt(value, 10);
        break;
      case 'user':
      case 'username':
      case 'user id':
      case 'uid':
        config.user = value;
        break;
      case 'password':
      case 'pwd':
        config.password = value;
        break;
      case 'database':
      case 'initial catalog':
        config.database = value;
        break;
      case 'connectiontimeout':
      case 'connect timeout':
        config.connectionTimeout = parseInt(value, 10);
        break;
      case 'commandtimeout':
      case 'request timeout':
        config.requestTimeout = parseInt(value, 10);
        break;
      case 'ssl':
      case 'encrypt':
        config.ssl = value.toLowerCase() === 'true' || value === '1';
        break;
    }
  }

  // Validate required fields
  if (!config.type) {
    throw new ConfigurationError('Database type is required (type=mssql or type=postgres)');
  }
  if (!config.host) {
    throw new ConfigurationError('Database host is required');
  }
  if (!config.user) {
    throw new ConfigurationError('Database user is required');
  }
  if (!config.password) {
    throw new ConfigurationError('Database password is required');
  }

  // Set defaults
  if (!config.port) {
    config.port = config.type === 'mssql' ? 1433 : 5432;
  }

  return config as DatabaseConfig;
}

/**
 * Create a database connection based on configuration
 */
export async function createConnection(
  connectionString: string
): Promise<DatabaseConnection> {
  const config = parseConnectionString(connectionString);

  logger.info('Creating database connection', {
    type: config.type,
    host: config.host,
    port: config.port,
    database: config.database,
  });

  let connection: DatabaseConnection;

  switch (config.type) {
    case 'postgres':
      connection = new PostgresDatabase(config);
      break;
    case 'mssql':
      connection = new MssqlDatabase(config);
      break;
    default:
      throw new ConfigurationError(`Unsupported database type: ${config.type}`);
  }

  await connection.connect();
  return connection;
}

/**
 * Close a database connection
 */
export async function closeConnection(connection: DatabaseConnection): Promise<void> {
  try {
    await connection.disconnect();
  } catch (error) {
    logger.error('Error closing database connection', { error });
  }
}
