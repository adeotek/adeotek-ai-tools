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

  constructor() {
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
                connectionString: {
                  type: 'string',
                  description:
                    'Database connection string (e.g., "type=postgres;host=localhost;port=5432;user=user;password=pass;database=mydb")',
                },
              },
              required: ['connectionString'],
            },
          },
          {
            name: 'sql_list_tables',
            description: 'List all tables in a specified database with metadata',
            inputSchema: {
              type: 'object',
              properties: {
                connectionString: {
                  type: 'string',
                  description: 'Database connection string',
                },
                database: {
                  type: 'string',
                  description: 'Database name',
                },
                schema: {
                  type: 'string',
                  description: 'Schema name (optional, defaults to "public" for PostgreSQL or "dbo" for SQL Server)',
                },
              },
              required: ['connectionString', 'database'],
            },
          },
          {
            name: 'sql_describe_table',
            description:
              'Get detailed schema information for a specific table including columns, indexes, foreign keys, and constraints',
            inputSchema: {
              type: 'object',
              properties: {
                connectionString: {
                  type: 'string',
                  description: 'Database connection string',
                },
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
              },
              required: ['connectionString', 'database', 'table'],
            },
          },
          {
            name: 'sql_query',
            description:
              'Execute a read-only SELECT query with automatic safety validation and row limits',
            inputSchema: {
              type: 'object',
              properties: {
                connectionString: {
                  type: 'string',
                  description: 'Database connection string',
                },
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
              },
              required: ['connectionString', 'database', 'query'],
            },
          },
          {
            name: 'sql_get_query_plan',
            description:
              'Get the execution plan for a query without executing it (useful for performance analysis)',
            inputSchema: {
              type: 'object',
              properties: {
                connectionString: {
                  type: 'string',
                  description: 'Database connection string',
                },
                database: {
                  type: 'string',
                  description: 'Database name',
                },
                query: {
                  type: 'string',
                  description: 'SQL SELECT statement to analyze',
                },
              },
              required: ['connectionString', 'database', 'query'],
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

        // Get or create connection
        const connectionString = (args?.connectionString as string) || '';
        const connection = await this.getConnection(connectionString);

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
   * Get or create a database connection
   */
  private async getConnection(connectionString: string): Promise<DatabaseConnection> {
    if (!this.connections.has(connectionString)) {
      const connection = await createConnection(connectionString);
      this.connections.set(connectionString, connection);
    }
    return this.connections.get(connectionString)!;
  }

  /**
   * Start the MCP server
   */
  async start(): Promise<void> {
    const transport = new StdioServerTransport();
    await this.server.connect(transport);

    logger.info('adeotek-sql-mcp server started', {
      protocol: '2025-11-25',
      transport: 'stdio',
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
    for (const [connectionString, connection] of this.connections.entries()) {
      try {
        await closeConnection(connection);
        this.connections.delete(connectionString);
      } catch (error) {
        logger.error('Error closing connection', { error });
      }
    }

    await this.server.close();
    logger.info('Server stopped');
  }
}
