/**
 * Query validation and security layer
 * Implements multiple layers of protection to ensure read-only access
 */

import { QueryValidationError } from '../utils/errors.js';
import { ValidationResult } from '../types/index.js';
import logger from '../utils/logger.js';

/**
 * List of dangerous SQL keywords that modify data or schema
 */
const BLOCKED_KEYWORDS = [
  // Data modification
  'INSERT',
  'UPDATE',
  'DELETE',
  'TRUNCATE',
  'MERGE',
  'UPSERT',
  'REPLACE',
  'COPY',
  // Schema modification
  'CREATE',
  'ALTER',
  'DROP',
  'RENAME',
  'COMMENT',
  // Permissions
  'GRANT',
  'REVOKE',
  // Transaction control
  'BEGIN',
  'COMMIT',
  'ROLLBACK',
  'SAVEPOINT',
  'START TRANSACTION',
  // Locking
  'LOCK',
  'UNLOCK',
  // Maintenance
  'VACUUM',
  'ANALYZE',
  'REINDEX',
  'CLUSTER',
  'CHECKPOINT',
  // Configuration
  'SET',
  'RESET',
  // Messaging
  'LISTEN',
  'NOTIFY',
  'UNLISTEN',
  // Procedural
  'DO',
  'CALL',
  'EXECUTE',
  'EXEC',
  'DECLARE',
  // Data definition
  'INDEX',
  'SEQUENCE',
  'VIEW',
  'TRIGGER',
  'FUNCTION',
  'PROCEDURE',
  'SCHEMA',
  'DATABASE',
  'TABLE',
  'CONSTRAINT',
];

/**
 * Dangerous SQL functions that could be used for unauthorized access
 */
const BLOCKED_FUNCTIONS = [
  'pg_read_file',
  'pg_read_binary_file',
  'pg_execute',
  'pg_terminate_backend',
  'pg_cancel_backend',
  'pg_sleep',
  'pg_reload_conf',
  'pg_rotate_logfile',
  'xp_cmdshell', // SQL Server
  'sp_executesql', // SQL Server
  'OPENROWSET', // SQL Server
  'OPENDATASOURCE', // SQL Server
];

/**
 * Pattern-based validation rules
 */
const DANGEROUS_PATTERNS = [
  /;\s*(INSERT|UPDATE|DELETE|DROP|CREATE|ALTER|TRUNCATE|GRANT|REVOKE)/i,
  /--.*?(INSERT|UPDATE|DELETE|DROP|CREATE|ALTER)/i,
  /\/\*.*?(INSERT|UPDATE|DELETE|DROP|CREATE|ALTER).*?\*\//i,
  /INTO\s+OUTFILE/i,
  /LOAD_FILE/i,
  /\$\$/i, // PostgreSQL procedural code blocks
];

/**
 * Validate a SQL query for read-only compliance
 */
export function validateQuery(query: string, maxQueryLength = 50000): ValidationResult {
  const errors: string[] = [];
  const warnings: string[] = [];

  // Basic validation
  if (!query || query.trim().length === 0) {
    errors.push('Query cannot be empty');
    return { isValid: false, errors, warnings };
  }

  if (query.length > maxQueryLength) {
    errors.push(`Query exceeds maximum length of ${maxQueryLength} characters`);
    return { isValid: false, errors, warnings };
  }

  // Normalize query for analysis (preserve original for execution)
  const normalizedQuery = query.trim().toUpperCase();

  // Check if query starts with SELECT, WITH, or EXPLAIN
  const allowedStarters = ['SELECT', 'WITH', 'EXPLAIN', 'SHOW', 'DESCRIBE', 'DESC'];
  const startsWithAllowed = allowedStarters.some((starter) =>
    normalizedQuery.startsWith(starter)
  );

  if (!startsWithAllowed) {
    errors.push(
      `Query must start with one of: ${allowedStarters.join(', ')}. Got: ${normalizedQuery.split(' ')[0] || 'empty'}`
    );
  }

  // Check for blocked keywords
  for (const keyword of BLOCKED_KEYWORDS) {
    const regex = new RegExp(`\\b${keyword}\\b`, 'i');
    if (regex.test(query)) {
      errors.push(`Blocked keyword detected: ${keyword}`);
    }
  }

  // Check for blocked functions
  for (const func of BLOCKED_FUNCTIONS) {
    const regex = new RegExp(`\\b${func}\\s*\\(`, 'i');
    if (regex.test(query)) {
      errors.push(`Blocked function detected: ${func}`);
    }
  }

  // Check for dangerous patterns
  for (const pattern of DANGEROUS_PATTERNS) {
    if (pattern.test(query)) {
      errors.push(`Dangerous SQL pattern detected: ${pattern.toString()}`);
    }
  }

  // Check for multiple statements (semicolons)
  const statementCount = query.split(';').filter((s) => s.trim().length > 0).length;
  if (statementCount > 1) {
    errors.push('Multiple SQL statements are not allowed');
  }

  // Warnings for potentially problematic queries
  if (!/LIMIT\s+\d+/i.test(query) && normalizedQuery.startsWith('SELECT')) {
    warnings.push('Query does not include a LIMIT clause - results may be truncated');
  }

  if (/SELECT\s+\*/i.test(query)) {
    warnings.push('Using SELECT * may retrieve more data than necessary');
  }

  // Log validation result
  if (errors.length > 0) {
    logger.warn('Query validation failed', {
      errors,
      queryPreview: query.substring(0, 100),
    });
  }

  return {
    isValid: errors.length === 0,
    errors,
    warnings,
  };
}

/**
 * Enforce query execution limits
 */
export function enforceQueryLimits(
  query: string,
  maxRows: number = 10000
): { query: string; limitApplied: boolean } {
  // Check if query already has a LIMIT clause
  const limitMatch = query.match(/LIMIT\s+(\d+)/i);

  if (limitMatch) {
    const existingLimit = parseInt(limitMatch[1], 10);
    if (existingLimit <= maxRows) {
      return { query, limitApplied: false };
    }
    // Replace with max allowed limit
    return {
      query: query.replace(/LIMIT\s+\d+/i, `LIMIT ${maxRows}`),
      limitApplied: true,
    };
  }

  // Add LIMIT clause if not present and query is a SELECT
  if (/^\s*(SELECT|WITH)/i.test(query)) {
    return {
      query: `${query.trim()} LIMIT ${maxRows}`,
      limitApplied: true,
    };
  }

  return { query, limitApplied: false };
}

/**
 * Sanitize database/schema/table names to prevent SQL injection
 */
export function sanitizeIdentifier(identifier: string): string {
  // Remove any non-alphanumeric characters except underscore and dot
  const sanitized = identifier.replace(/[^a-zA-Z0-9_.]/g, '');

  if (sanitized !== identifier) {
    logger.warn('Identifier was sanitized', { original: identifier, sanitized });
  }

  return sanitized;
}

/**
 * Validate and throw if query is not safe
 */
export function validateQueryOrThrow(query: string, maxQueryLength?: number): void {
  const result = validateQuery(query, maxQueryLength);

  if (!result.isValid) {
    throw new QueryValidationError(
      `Query validation failed: ${result.errors.join(', ')}`,
      result.errors
    );
  }

  // Log warnings
  if (result.warnings.length > 0) {
    logger.warn('Query validation warnings', { warnings: result.warnings });
  }
}
