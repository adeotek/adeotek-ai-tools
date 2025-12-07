/**
 * Tests for MCP prompts
 */

import { describe, test, expect } from '@jest/globals';
import { getPrompt, analyzeSchemaPrompt, queryAssistantPrompt, performanceReviewPrompt } from '../prompts/index.js';

describe('MCP Prompts', () => {
  describe('analyzeSchemaPrompt', () => {
    test('should generate prompt with database name', () => {
      const result = analyzeSchemaPrompt('testdb');

      expect(result.description).toContain('testdb');
      expect(result.messages).toHaveLength(2);
      expect(result.messages[0].role).toBe('system');
      expect(result.messages[1].role).toBe('user');
    });

    test('should include focus area in prompt', () => {
      const result = analyzeSchemaPrompt('testdb', 'indexes');

      expect(result.description).toContain('indexes');
      expect(result.messages[1].content).toContain('indexes');
    });

    test('should use default focus if not provided', () => {
      const result = analyzeSchemaPrompt('testdb');

      expect(result.description).toContain('general overview');
    });
  });

  describe('queryAssistantPrompt', () => {
    test('should generate prompt with requirement', () => {
      const requirement = 'Get all users who registered in the last 30 days';
      const result = queryAssistantPrompt('testdb', requirement);

      expect(result.description).toContain(requirement);
      expect(result.messages).toHaveLength(2);
      expect(result.messages[1].content).toContain(requirement);
    });

    test('should include database name', () => {
      const result = queryAssistantPrompt('mydb', 'test requirement');

      expect(result.description).toContain('mydb');
      expect(result.messages[1].content).toContain('mydb');
    });

    test('should emphasize read-only queries in system message', () => {
      const result = queryAssistantPrompt('testdb', 'test');

      expect(result.messages[0].content).toContain('read-only');
      expect(result.messages[0].content).toContain('SELECT');
    });
  });

  describe('performanceReviewPrompt', () => {
    test('should generate prompt with query', () => {
      const query = 'SELECT * FROM users WHERE created_at > NOW() - INTERVAL 30 DAY';
      const result = performanceReviewPrompt('testdb', query);

      expect(result.description).toContain('testdb');
      expect(result.messages).toHaveLength(2);
      expect(result.messages[1].content).toContain(query);
    });

    test('should include performance optimization guidance', () => {
      const result = performanceReviewPrompt('testdb', 'SELECT * FROM users');

      expect(result.messages[0].content).toContain('performance');
      expect(result.messages[0].content).toContain('optimization');
      expect(result.messages[1].content).toContain('execution plan');
    });
  });

  describe('getPrompt', () => {
    test('should return analyze-schema prompt', () => {
      const result = getPrompt('analyze-schema', { database: 'testdb' });

      expect(result.description).toContain('testdb');
      expect(result.messages).toHaveLength(2);
    });

    test('should return query-assistant prompt', () => {
      const result = getPrompt('query-assistant', {
        database: 'testdb',
        requirement: 'get all users',
      });

      expect(result.description).toContain('get all users');
      expect(result.messages).toHaveLength(2);
    });

    test('should return performance-review prompt', () => {
      const result = getPrompt('performance-review', {
        database: 'testdb',
        query: 'SELECT * FROM users',
      });

      expect(result.description).toContain('testdb');
      expect(result.messages).toHaveLength(2);
    });

    test('should throw error for unknown prompt', () => {
      expect(() => {
        getPrompt('unknown-prompt', { database: 'testdb' });
      }).toThrow('Unknown prompt');
    });

    test('should throw error for missing required arguments', () => {
      expect(() => {
        getPrompt('query-assistant', { database: 'testdb' });
      }).toThrow('Missing required argument: requirement');

      expect(() => {
        getPrompt('performance-review', { database: 'testdb' });
      }).toThrow('Missing required argument: query');
    });
  });
});
