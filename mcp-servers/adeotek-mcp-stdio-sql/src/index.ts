#!/usr/bin/env node
/**
 * Entry point for adeotek-sql-mcp server
 */

import { AdeoSqlMcpServer } from './server.js';
import logger from './utils/logger.js';

/**
 * Parse connection strings from command-line arguments or environment variables
 */
function parseConnectionStrings(): Map<string, string> {
  const connections = new Map<string, string>();

  // Parse command-line arguments: --connection "type=postgres;host=..."
  // or named connections: --postgres "type=postgres;..." --mssql "type=mssql;..."
  const args = process.argv.slice(2);
  for (let i = 0; i < args.length; i++) {
    const arg = args[i];
    if (arg.startsWith('--') && i + 1 < args.length) {
      const name = arg.slice(2); // Remove '--' prefix
      const connectionString = args[i + 1];

      if (connectionString && !connectionString.startsWith('--')) {
        connections.set(name, connectionString);
        i++; // Skip the next argument as we've consumed it
      }
    }
  }

  // Also check environment variables: SQL_CONNECTION_DEFAULT, SQL_CONNECTION_POSTGRES, etc.
  const envPrefix = 'SQL_CONNECTION_';
  for (const [key, value] of Object.entries(process.env)) {
    if (key.startsWith(envPrefix) && value) {
      const name = key.slice(envPrefix.length).toLowerCase();
      if (!connections.has(name)) {
        connections.set(name, value);
      }
    }
  }

  return connections;
}

async function main(): Promise<void> {
  try {
    const connections = parseConnectionStrings();

    if (connections.size === 0) {
      logger.error(
        'No database connections configured. Please provide connection strings via command-line arguments or environment variables.',
        {
          examples: [
            'Command-line: node dist/index.js --default "type=postgres;host=localhost;..."',
            'Environment: SQL_CONNECTION_DEFAULT="type=postgres;host=localhost;..."',
          ],
        }
      );
      process.exit(1);
    }

    logger.info('Starting server with configured connections', {
      connectionNames: Array.from(connections.keys()),
    });

    const server = new AdeoSqlMcpServer(connections);
    await server.start();
  } catch (error) {
    logger.error('Failed to start server', { error });
    process.exit(1);
  }
}

main();
