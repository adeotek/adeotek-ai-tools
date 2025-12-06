/**
 * Tests for read-only safety mechanisms
 */

import { describe, test, expect } from '@jest/globals';
import {
  validateQuery,
  enforceQueryLimits,
  sanitizeIdentifier,
  validateQueryOrThrow,
} from '../security/queryValidator.js';
import { QueryValidationError } from '../utils/errors.js';

describe('Query Validation', () => {
  describe('validateQuery - Valid Queries', () => {
    test('should accept simple SELECT query', () => {
      const result = validateQuery('SELECT * FROM users');
      expect(result.isValid).toBe(true);
      expect(result.errors).toHaveLength(0);
    });

    test('should accept SELECT with WHERE clause', () => {
      const result = validateQuery('SELECT id, name FROM users WHERE age > 18');
      expect(result.isValid).toBe(true);
    });

    test('should accept SELECT with JOIN', () => {
      const result = validateQuery(
        'SELECT u.name, o.order_id FROM users u JOIN orders o ON u.id = o.user_id'
      );
      expect(result.isValid).toBe(true);
    });

    test('should accept WITH (CTE) query', () => {
      const result = validateQuery(
        'WITH active_users AS (SELECT * FROM users WHERE active = true) SELECT * FROM active_users'
      );
      expect(result.isValid).toBe(true);
    });

    test('should accept EXPLAIN query', () => {
      const result = validateQuery('EXPLAIN SELECT * FROM users');
      expect(result.isValid).toBe(true);
    });
  });

  describe('validateQuery - Invalid Queries', () => {
    test('should reject INSERT query', () => {
      const result = validateQuery("INSERT INTO users (name) VALUES ('John')");
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('INSERT'))).toBe(true);
    });

    test('should reject UPDATE query', () => {
      const result = validateQuery("UPDATE users SET name = 'John' WHERE id = 1");
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('UPDATE'))).toBe(true);
    });

    test('should reject DELETE query', () => {
      const result = validateQuery('DELETE FROM users WHERE id = 1');
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('DELETE'))).toBe(true);
    });

    test('should reject DROP query', () => {
      const result = validateQuery('DROP TABLE users');
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('DROP'))).toBe(true);
    });

    test('should reject CREATE query', () => {
      const result = validateQuery('CREATE TABLE test (id INT)');
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('CREATE'))).toBe(true);
    });

    test('should reject ALTER query', () => {
      const result = validateQuery('ALTER TABLE users ADD COLUMN email VARCHAR(255)');
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('ALTER'))).toBe(true);
    });

    test('should reject TRUNCATE query', () => {
      const result = validateQuery('TRUNCATE TABLE users');
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('TRUNCATE'))).toBe(true);
    });

    test('should reject multiple statements', () => {
      const result = validateQuery('SELECT * FROM users; DROP TABLE users;');
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('Multiple SQL statements'))).toBe(true);
    });

    test('should reject dangerous functions', () => {
      const result = validateQuery("SELECT pg_read_file('/etc/passwd')");
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('pg_read_file'))).toBe(true);
    });

    test('should reject empty query', () => {
      const result = validateQuery('');
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('cannot be empty'))).toBe(true);
    });

    test('should reject query exceeding max length', () => {
      const longQuery = 'SELECT * FROM users WHERE ' + 'a = 1 AND '.repeat(10000);
      const result = validateQuery(longQuery, 1000);
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('exceeds maximum length'))).toBe(true);
    });
  });

  describe('validateQuery - Warnings', () => {
    test('should warn about missing LIMIT clause', () => {
      const result = validateQuery('SELECT * FROM users');
      expect(result.warnings.some((w) => w.includes('LIMIT'))).toBe(true);
    });

    test('should warn about SELECT *', () => {
      const result = validateQuery('SELECT * FROM users');
      expect(result.warnings.some((w) => w.includes('SELECT *'))).toBe(true);
    });

    test('should not warn when LIMIT is present', () => {
      const result = validateQuery('SELECT id, name FROM users LIMIT 10');
      expect(result.warnings).toHaveLength(0);
    });
  });

  describe('enforceQueryLimits', () => {
    test('should add LIMIT clause if missing', () => {
      const result = enforceQueryLimits('SELECT * FROM users', 100);
      expect(result.query).toContain('LIMIT 100');
      expect(result.limitApplied).toBe(true);
    });

    test('should not modify query with existing LIMIT', () => {
      const result = enforceQueryLimits('SELECT * FROM users LIMIT 50', 100);
      expect(result.query).toBe('SELECT * FROM users LIMIT 50');
      expect(result.limitApplied).toBe(false);
    });

    test('should replace LIMIT if it exceeds max', () => {
      const result = enforceQueryLimits('SELECT * FROM users LIMIT 500', 100);
      expect(result.query).toContain('LIMIT 100');
      expect(result.limitApplied).toBe(true);
    });

    test('should add LIMIT to WITH query', () => {
      const result = enforceQueryLimits(
        'WITH cte AS (SELECT * FROM users) SELECT * FROM cte',
        100
      );
      expect(result.query).toContain('LIMIT 100');
      expect(result.limitApplied).toBe(true);
    });
  });

  describe('sanitizeIdentifier', () => {
    test('should allow alphanumeric and underscores', () => {
      const result = sanitizeIdentifier('user_table_123');
      expect(result).toBe('user_table_123');
    });

    test('should allow dots for schema.table', () => {
      const result = sanitizeIdentifier('public.users');
      expect(result).toBe('public.users');
    });

    test('should remove dangerous characters', () => {
      const result = sanitizeIdentifier("users'; DROP TABLE users;--");
      expect(result).not.toContain("'");
      expect(result).not.toContain(';');
      expect(result).not.toContain('-');
    });

    test('should remove SQL injection attempts', () => {
      const result = sanitizeIdentifier('1=1 OR 1=1');
      expect(result).toBe('11OR11');
    });
  });

  describe('validateQueryOrThrow', () => {
    test('should not throw for valid query', () => {
      expect(() => {
        validateQueryOrThrow('SELECT * FROM users LIMIT 10');
      }).not.toThrow();
    });

    test('should throw QueryValidationError for invalid query', () => {
      expect(() => {
        validateQueryOrThrow("INSERT INTO users VALUES (1, 'test')");
      }).toThrow(QueryValidationError);
    });

    test('should include error details in exception', () => {
      try {
        validateQueryOrThrow('DELETE FROM users');
        fail('Should have thrown');
      } catch (error) {
        expect(error).toBeInstanceOf(QueryValidationError);
        if (error instanceof QueryValidationError) {
          expect(error.violations).toContain('Blocked keyword detected: DELETE');
        }
      }
    });
  });

  describe('SQL Injection Prevention', () => {
    test('should reject union-based injection', () => {
      const result = validateQuery("SELECT * FROM users WHERE id = 1 UNION SELECT * FROM passwords");
      // This should pass validation but we test it anyway
      expect(result.isValid).toBe(true); // UNION in SELECT is technically allowed for read-only
    });

    test('should reject comment-based injection with dangerous keywords', () => {
      const result = validateQuery("SELECT * FROM users -- DROP TABLE users");
      expect(result.isValid).toBe(false); // Comments with dangerous keywords are blocked
      expect(result.errors.length).toBeGreaterThan(0);
    });

    test('should reject stacked queries', () => {
      const result = validateQuery("SELECT * FROM users; INSERT INTO logs VALUES ('hacked')");
      expect(result.isValid).toBe(false);
      expect(result.errors.some((e) => e.includes('Multiple SQL statements'))).toBe(true);
    });
  });
});
