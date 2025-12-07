/**
 * Tests for database connection management
 */

import { describe, test, expect } from '@jest/globals';
import { parseConnectionString } from '../database/connection.js';
import { ConfigurationError } from '../utils/errors.js';

describe('Database Connection', () => {
  describe('parseConnectionString - PostgreSQL', () => {
    test('should parse complete PostgreSQL connection string', () => {
      const connStr =
        'type=postgres;host=localhost;port=5432;user=testuser;password=testpass;database=testdb';
      const config = parseConnectionString(connStr);

      expect(config.type).toBe('postgres');
      expect(config.host).toBe('localhost');
      expect(config.port).toBe(5432);
      expect(config.user).toBe('testuser');
      expect(config.password).toBe('testpass');
      expect(config.database).toBe('testdb');
    });

    test('should use default port for PostgreSQL', () => {
      const connStr = 'type=postgres;host=localhost;user=testuser;password=testpass';
      const config = parseConnectionString(connStr);

      expect(config.port).toBe(5432);
    });

    test('should parse SSL option', () => {
      const connStr =
        'type=postgres;host=localhost;user=testuser;password=testpass;ssl=true';
      const config = parseConnectionString(connStr);

      expect(config.ssl).toBe(true);
    });

    test('should parse timeout options', () => {
      const connStr =
        'type=postgres;host=localhost;user=testuser;password=testpass;connectionTimeout=60;commandTimeout=120';
      const config = parseConnectionString(connStr);

      expect(config.connectionTimeout).toBe(60);
      expect(config.requestTimeout).toBe(120);
    });
  });

  describe('parseConnectionString - SQL Server', () => {
    test('should parse complete SQL Server connection string', () => {
      const connStr =
        'type=mssql;host=localhost;port=1433;user=sa;password=StrongPass123;database=master';
      const config = parseConnectionString(connStr);

      expect(config.type).toBe('mssql');
      expect(config.host).toBe('localhost');
      expect(config.port).toBe(1433);
      expect(config.user).toBe('sa');
      expect(config.password).toBe('StrongPass123');
      expect(config.database).toBe('master');
    });

    test('should use default port for SQL Server', () => {
      const connStr = 'type=mssql;host=localhost;user=sa;password=StrongPass123';
      const config = parseConnectionString(connStr);

      expect(config.port).toBe(1433);
    });

    test('should parse alternative key names', () => {
      const connStr =
        'dbtype=mssql;server=localhost;user id=sa;pwd=StrongPass123;initial catalog=testdb';
      const config = parseConnectionString(connStr);

      expect(config.type).toBe('mssql');
      expect(config.host).toBe('localhost');
      expect(config.user).toBe('sa');
      expect(config.password).toBe('StrongPass123');
      expect(config.database).toBe('testdb');
    });
  });

  describe('parseConnectionString - Validation', () => {
    test('should throw error if type is missing', () => {
      const connStr = 'host=localhost;user=testuser;password=testpass';

      expect(() => parseConnectionString(connStr)).toThrow(ConfigurationError);
      expect(() => parseConnectionString(connStr)).toThrow(/Database type is required/);
    });

    test('should throw error if host is missing', () => {
      const connStr = 'type=postgres;user=testuser;password=testpass';

      expect(() => parseConnectionString(connStr)).toThrow(ConfigurationError);
      expect(() => parseConnectionString(connStr)).toThrow(/host is required/);
    });

    test('should throw error if user is missing', () => {
      const connStr = 'type=postgres;host=localhost;password=testpass';

      expect(() => parseConnectionString(connStr)).toThrow(ConfigurationError);
      expect(() => parseConnectionString(connStr)).toThrow(/user is required/);
    });

    test('should throw error if password is missing', () => {
      const connStr = 'type=postgres;host=localhost;user=testuser';

      expect(() => parseConnectionString(connStr)).toThrow(ConfigurationError);
      expect(() => parseConnectionString(connStr)).toThrow(/password is required/);
    });

    test('should allow database to be optional', () => {
      const connStr = 'type=postgres;host=localhost;user=testuser;password=testpass';

      expect(() => parseConnectionString(connStr)).not.toThrow();
    });
  });

  describe('parseConnectionString - Edge Cases', () => {
    test('should handle connection string with extra spaces', () => {
      const connStr =
        '  type = postgres ; host = localhost ; user = testuser ; password = testpass  ';
      const config = parseConnectionString(connStr);

      expect(config.type).toBe('postgres');
      expect(config.host).toBe('localhost');
    });

    test('should handle empty segments', () => {
      const connStr = 'type=postgres;;host=localhost;;user=testuser;password=testpass';
      const config = parseConnectionString(connStr);

      expect(config.type).toBe('postgres');
      expect(config.host).toBe('localhost');
    });

    test('should parse numeric string to number for port', () => {
      const connStr = 'type=postgres;host=localhost;port=5433;user=testuser;password=testpass';
      const config = parseConnectionString(connStr);

      expect(typeof config.port).toBe('number');
      expect(config.port).toBe(5433);
    });

    test('should handle boolean SSL values correctly', () => {
      const config1 = parseConnectionString(
        'type=postgres;host=localhost;user=test;password=test;ssl=true'
      );
      const config2 = parseConnectionString(
        'type=postgres;host=localhost;user=test;password=test;ssl=false'
      );
      const config3 = parseConnectionString(
        'type=postgres;host=localhost;user=test;password=test;ssl=1'
      );

      expect(config1.ssl).toBe(true);
      expect(config2.ssl).toBe(false);
      expect(config3.ssl).toBe(true);
    });
  });
});
