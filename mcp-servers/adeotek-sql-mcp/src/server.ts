/**
 * MCP Server Implementation for adeotek-sql-mcp
 * Implements MCP Protocol 2025-11-25 with stdio transport
 */

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListPromptsRequestSchema,
  GetPromptRequestSchema,
} from '@modelcontextprotocol/sdk/types.js';
import type { DatabaseConnection } from './database/connection.js';
import { createConnection, closeConnection } from './database/connection.js';
import * as tools from './tools/index.js';
import * as prompts from './prompts/index.js';
import logger from './utils/logger.js';
import { handleError } from './utils/errors.js';

export class AdeoSqlMcpServer {
  private server: Server;
  private connections: Map<string, DatabaseConnection> = new Map();
  private connectionStrings: Map<string, string>;

  constructor(connectionStrings: Map<string, string>) {
    this.connectionStrings = connectionStrings;

    this.server = new Server(
      {
        name: 'adeotek-sql-mcp',
        version: '1.0.0',
      },
      {
        capabilities: {
          tools: {},
          prompts: {},
        },
      }
    );

    this.setupHandlers();
  }

  /**
   * Setup MCP request handlers
   */
  private setupHandlers(): void {
    // List available tools
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      return {
        tools: [
          {
            name: 'sql_list_databases',
            description: 'List all databases available on the configured SQL server',
            inputSchema: {
              type: 'object',
              properties: {
                connection: {
                  type: 'string',
                  description:
                    'Connection name (optional, defaults to "default" or the only configured connection)',
                },
              },
              required: [],
            },
          },
          {
            name: 'sql_list_tables',
            description: 'List all tables in a specified database with metadata',
            inputSchema: {
              type: 'object',
              properties: {
                database: {
                  type: 'string',
                  description: 'Database name',
                },
                schema: {
                  type: 'string',
                  description:
                    'Schema name (optional, defaults to "public" for PostgreSQL or "dbo" for SQL Server)',
                },
                connection: {
                  type: 'string',
                  description: 'Connection name (optional)',
                },
              },
              required: ['database'],
            },
          },
          {
            name: 'sql_describe_table',
            description:
              'Get detailed schema information for a specific table including columns, indexes, foreign keys, and constraints',
            inputSchema: {
              type: 'object',
              properties: {
                database: {
                  type: 'string',
                  description: 'Database name',
                },
                table: {
                  type: 'string',
                  description: 'Table name',
                },
                schema: {
                  type: 'string',
                  description: 'Schema name (optional)',
                },
                connection: {
                  type: 'string',
                  description: 'Connection name (optional)',
                },
              },
              required: ['database', 'table'],
            },
          },
          {
            name: 'sql_query',
            description:
              'Execute a read-only SELECT query with automatic safety validation and row limits',
            inputSchema: {
              type: 'object',
              properties: {
                database: {
                  type: 'string',
                  description: 'Database name',
                },
                query: {
                  type: 'string',
                  description: 'SQL SELECT statement (read-only queries only)',
                },
                maxRows: {
                  type: 'number',
                  description: 'Maximum rows to return (default: 1000, max: 10000)',
                  default: 1000,
                },
                connection: {
                  type: 'string',
                  description: 'Connection name (optional)',
                },
              },
              required: ['database', 'query'],
            },
          },
          {
            name: 'sql_get_query_plan',
            description:
              'Get the execution plan for a query without executing it (useful for performance analysis)',
            inputSchema: {
              type: 'object',
              properties: {
                database: {
                  type: 'string',
                  description: 'Database name',
                },
                query: {
                  type: 'string',
                  description: 'SQL SELECT statement to analyze',
                },
                connection: {
                  type: 'string',
                  description: 'Connection name (optional)',
                },
              },
              required: ['database', 'query'],
            },
          },
        ],
      };
    });

    // Handle tool calls
    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        logger.info('Tool call received', { tool: name });

        // Get pre-configured connection (optional parameter, defaults to 'default' or first connection)
        const connectionName = (args?.connection as string) || undefined;
        const connection = await this.getConnection(connectionName);

        let result;

        switch (name) {
          case 'sql_list_databases':
            result = await tools.listDatabases(connection);
            break;

          case 'sql_list_tables':
            result = await tools.listTables(
              connection,
              args?.database as string,
              args?.schema as string | undefined
            );
            break;

          case 'sql_describe_table':
            result = await tools.describeTable(
              connection,
              args?.database as string,
              args?.table as string,
              args?.schema as string | undefined
            );
            break;

          case 'sql_query':
            result = await tools.executeQuery(
              connection,
              args?.database as string,
              args?.query as string,
              args?.maxRows as number | undefined
            );
            break;

          case 'sql_get_query_plan':
            result = await tools.getQueryPlan(
              connection,
              args?.database as string,
              args?.query as string
            );
            break;

          default:
            throw new Error(`Unknown tool: ${name}`);
        }

        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(result, null, 2),
            },
          ],
        };
      } catch (error) {
        logger.error('Tool execution error', { tool: name, error });
        const errorInfo = handleError(error);
        return {
          content: [
            {
              type: 'text',
              text: JSON.stringify(
                {
                  success: false,
                  error: errorInfo.message,
                  code: errorInfo.code,
                },
                null,
                2
              ),
            },
          ],
          isError: true,
        };
      }
    });

    // List available prompts
    this.server.setRequestHandler(ListPromptsRequestSchema, async () => {
      return {
        prompts: [
          {
            name: 'analyze-schema',
            description:
              'Analyze database schema and provide insights about structure, relationships, and potential issues',
            arguments: [
              {
                name: 'database',
                description: 'Database to analyze',
                required: true,
              },
              {
                name: 'focus',
                description:
                  'Specific area to focus on (e.g., "tables", "relationships", "indexes", "performance")',
                required: false,
              },
            ],
          },
          {
            name: 'query-assistant',
            description: 'Help construct SQL queries based on natural language requirements',
            arguments: [
              {
                name: 'database',
                description: 'Target database',
                required: true,
              },
              {
                name: 'requirement',
                description: 'Natural language description of what to query',
                required: true,
              },
            ],
          },
          {
            name: 'performance-review',
            description: 'Review query performance and suggest optimizations',
            arguments: [
              {
                name: 'database',
                description: 'Database name',
                required: true,
              },
              {
                name: 'query',
                description: 'SQL query to analyze',
                required: true,
              },
            ],
          },
        ],
      };
    });

    // Handle prompt requests
    this.server.setRequestHandler(GetPromptRequestSchema, async (request) => {
      const { name, arguments: args } = request.params;

      try {
        logger.info('Prompt request received', { prompt: name });

        const promptResult = prompts.getPrompt(name, (args || {}) as Record<string, string>);

        return {
          description: promptResult.description,
          messages: promptResult.messages.map((msg) => ({
            role: msg.role,
            content: {
              type: 'text',
              text: msg.content,
            },
          })),
        };
      } catch (error) {
        logger.error('Prompt execution error', { prompt: name, error });
        const errorInfo = handleError(error);
        throw new Error(`Failed to get prompt: ${errorInfo.message}`);
      }
    });
  }

  /**
   * Get a pre-configured database connection by name
   */
  private async getConnection(connectionName?: string): Promise<DatabaseConnection> {
    // Default to 'default' or the first configured connection
    const name = connectionName || 'default';
    let actualName = name;

    // If the requested connection doesn't exist, try to use the first available one
    if (!this.connectionStrings.has(actualName)) {
      if (this.connectionStrings.size === 1) {
        // Only one connection configured, use it
        actualName = Array.from(this.connectionStrings.keys())[0];
        logger.debug(`Connection '${name}' not found, using '${actualName}'`);
      } else {
        throw new Error(
          `Connection '${name}' not configured. Available connections: ${Array.from(
            this.connectionStrings.keys()
          ).join(', ')}`
        );
      }
    }

    // Create connection if not already created
    if (!this.connections.has(actualName)) {
      const connectionString = this.connectionStrings.get(actualName)!;
      logger.info(`Creating connection '${actualName}'`);
      const connection = await createConnection(connectionString);
      this.connections.set(actualName, connection);
    }

    return this.connections.get(actualName)!;
  }

  /**
   * Initialize all configured database connections
   */
  private async initializeConnections(): Promise<void> {
    logger.info('Initializing database connections', {
      count: this.connectionStrings.size,
      names: Array.from(this.connectionStrings.keys()),
    });

    for (const [name, connectionString] of this.connectionStrings.entries()) {
      try {
        const connection = await createConnection(connectionString);
        this.connections.set(name, connection);
        logger.info(`Connection '${name}' initialized successfully`);
      } catch (error) {
        logger.error(`Failed to initialize connection '${name}'`, { error });
        throw error;
      }
    }
  }

  /**
   * Start the MCP server
   */
  async start(): Promise<void> {
    // Initialize all database connections
    await this.initializeConnections();

    const transport = new StdioServerTransport();
    await this.server.connect(transport);

    logger.info('adeotek-sql-mcp server started', {
      protocol: '2025-11-25',
      transport: 'stdio',
      connections: Array.from(this.connections.keys()),
    });

    // Handle graceful shutdown
    process.on('SIGINT', async () => {
      await this.stop();
      process.exit(0);
    });

    process.on('SIGTERM', async () => {
      await this.stop();
      process.exit(0);
    });
  }

  /**
   * Stop the MCP server and close all connections
   */
  async stop(): Promise<void> {
    logger.info('Shutting down adeotek-sql-mcp server');

    // Close all database connections
    for (const [connectionName, connection] of this.connections.entries()) {
      try {
        await closeConnection(connection);
        this.connections.delete(connectionName);
        logger.info(`Connection '${connectionName}' closed`);
      } catch (error) {
        logger.error(`Error closing connection '${connectionName}'`, { error });
      }
    }

    await this.server.close();
    logger.info('Server stopped');
  }
}
