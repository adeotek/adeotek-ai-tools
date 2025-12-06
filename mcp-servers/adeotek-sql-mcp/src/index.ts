#!/usr/bin/env node
/**
 * Entry point for adeotek-sql-mcp server
 */

import { AdeoSqlMcpServer } from './server.js';
import logger from './utils/logger.js';

async function main(): Promise<void> {
  try {
    const server = new AdeoSqlMcpServer();
    await server.start();
  } catch (error) {
    logger.error('Failed to start server', { error });
    process.exit(1);
  }
}

main();
