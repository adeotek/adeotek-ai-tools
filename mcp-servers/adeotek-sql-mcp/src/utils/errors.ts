/**
 * Custom error classes for the adeotek-sql-mcp server
 */

export class McpError extends Error {
  constructor(
    message: string,
    public readonly code: string,
    public readonly statusCode: number = 500
  ) {
    super(message);
    this.name = 'McpError';
    Error.captureStackTrace(this, this.constructor);
  }
}

export class DatabaseConnectionError extends McpError {
  constructor(message: string, public readonly dbType: string) {
    super(message, 'DATABASE_CONNECTION_ERROR', 503);
    this.name = 'DatabaseConnectionError';
  }
}

export class QueryValidationError extends McpError {
  constructor(message: string, public readonly violations: string[]) {
    super(message, 'QUERY_VALIDATION_ERROR', 400);
    this.name = 'QueryValidationError';
  }
}

export class QueryExecutionError extends McpError {
  constructor(message: string, public readonly query?: string) {
    super(message, 'QUERY_EXECUTION_ERROR', 500);
    this.name = 'QueryExecutionError';
  }
}

export class ConfigurationError extends McpError {
  constructor(message: string) {
    super(message, 'CONFIGURATION_ERROR', 400);
    this.name = 'ConfigurationError';
  }
}

export class TimeoutError extends McpError {
  constructor(message: string, public readonly timeoutMs: number) {
    super(message, 'TIMEOUT_ERROR', 408);
    this.name = 'TimeoutError';
  }
}

export class ToolNotFoundError extends McpError {
  constructor(toolName: string) {
    super(`Tool not found: ${toolName}`, 'TOOL_NOT_FOUND', 404);
    this.name = 'ToolNotFoundError';
  }
}

export class PromptNotFoundError extends McpError {
  constructor(promptName: string) {
    super(`Prompt not found: ${promptName}`, 'PROMPT_NOT_FOUND', 404);
    this.name = 'PromptNotFoundError';
  }
}

/**
 * Error handler utility to convert errors to MCP-compatible format
 */
export function handleError(error: unknown): { message: string; code: string } {
  if (error instanceof McpError) {
    return {
      message: error.message,
      code: error.code,
    };
  }

  if (error instanceof Error) {
    return {
      message: error.message,
      code: 'INTERNAL_ERROR',
    };
  }

  return {
    message: 'An unknown error occurred',
    code: 'UNKNOWN_ERROR',
  };
}
