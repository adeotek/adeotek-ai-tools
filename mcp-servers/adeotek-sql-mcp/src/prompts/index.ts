/**
 * MCP Prompts Implementation
 */

import type { PromptMessage } from '../types/index.js';

export interface PromptResult {
  description: string;
  messages: PromptMessage[];
}

/**
 * Prompt: analyze-schema
 * Analyze database schema and provide insights about structure, relationships, and potential issues
 */
export function analyzeSchemaPrompt(
  database: string,
  focus?: string
): PromptResult {
  const focusArea = focus || 'general overview';

  return {
    description: `Analyze the database schema for "${database}" with focus on: ${focusArea}`,
    messages: [
      {
        role: 'system',
        content: `You are a database schema analyst. Your task is to analyze the provided database schema and provide insights about structure, relationships, data integrity, and potential issues.`,
      },
      {
        role: 'user',
        content: `Please analyze the database schema for "${database}".

Focus area: ${focusArea}

Provide analysis on:
1. **Schema Structure**: Overview of tables, relationships, and organization
2. **Data Integrity**: Primary keys, foreign keys, constraints, and referential integrity
3. **Indexing Strategy**: Index coverage, missing indexes, redundant indexes
4. **Naming Conventions**: Consistency in naming tables, columns, and constraints
5. **Normalization**: Assessment of normalization level and potential denormalization opportunities
6. **Potential Issues**: Missing constraints, orphaned records, data type inconsistencies
7. **Best Practices**: Recommendations for improvements

Please use the sql_list_tables and sql_describe_table tools to gather the necessary information.`,
      },
    ],
  };
}

/**
 * Prompt: query-assistant
 * Help construct SQL queries based on natural language requirements
 */
export function queryAssistantPrompt(
  database: string,
  requirement: string
): PromptResult {
  return {
    description: `Assist with constructing SQL query for "${database}" based on: ${requirement}`,
    messages: [
      {
        role: 'system',
        content: `You are a SQL query assistant. Your task is to help users construct correct, efficient, and safe SQL queries based on their natural language requirements.

IMPORTANT RULES:
1. Generate ONLY read-only SELECT queries
2. Always include appropriate WHERE clauses for filtering
3. Use LIMIT clauses to prevent retrieving too much data
4. Use proper JOINs when querying multiple tables
5. Include ORDER BY when relevant for result ordering
6. Use aggregate functions (COUNT, SUM, AVG, etc.) when appropriate
7. Validate that the query follows SQL best practices`,
      },
      {
        role: 'user',
        content: `I need help constructing a SQL query for the "${database}" database.

Requirement: ${requirement}

Steps to follow:
1. Use sql_describe_table to understand the relevant table schemas
2. Construct the appropriate SQL SELECT query
3. Explain what the query does
4. Suggest any optimizations or best practices
5. Use sql_query to test the query if needed

Please provide:
- The SQL query
- Explanation of what it does
- Any assumptions made
- Potential optimizations or alternatives`,
      },
    ],
  };
}

/**
 * Prompt: performance-review
 * Review query performance and suggest optimizations
 */
export function performanceReviewPrompt(
  database: string,
  query: string
): PromptResult {
  return {
    description: `Review performance of query in "${database}" database`,
    messages: [
      {
        role: 'system',
        content: `You are a SQL performance optimization expert. Your task is to analyze SQL queries and execution plans to identify performance issues and suggest optimizations.

Focus areas:
1. Query structure and efficiency
2. Index usage and missing indexes
3. JOIN strategies and order
4. WHERE clause optimization
5. SELECT clause optimization (avoid SELECT *)
6. Subquery vs JOIN performance
7. Data type usage
8. Execution plan analysis`,
      },
      {
        role: 'user',
        content: `Please review the performance of this SQL query on the "${database}" database:

\`\`\`sql
${query}
\`\`\`

Steps to follow:
1. Use sql_get_query_plan to get the execution plan
2. Use sql_describe_table to understand the table structures involved
3. Analyze the query structure and execution plan
4. Identify performance bottlenecks

Please provide:
1. **Current Query Analysis**: What the query does and its current performance characteristics
2. **Execution Plan Review**: Analysis of the query execution plan
3. **Performance Issues**: Identified bottlenecks and inefficiencies
4. **Optimization Suggestions**: Specific recommendations with examples:
   - Index recommendations (which columns, why)
   - Query rewriting opportunities
   - JOIN optimization
   - WHERE clause improvements
   - Any other relevant optimizations
5. **Estimated Impact**: Expected performance improvement for each suggestion
6. **Optimized Query**: Rewritten version of the query if applicable

Note: Consider the trade-offs between query performance and maintainability.`,
      },
    ],
  };
}

/**
 * Get a prompt by name with arguments
 */
export function getPrompt(
  name: string,
  args: Record<string, string>
): PromptResult {
  switch (name) {
    case 'analyze-schema':
      return analyzeSchemaPrompt(args.database, args.focus);

    case 'query-assistant':
      if (!args.requirement) {
        throw new Error('Missing required argument: requirement');
      }
      return queryAssistantPrompt(args.database, args.requirement);

    case 'performance-review':
      if (!args.query) {
        throw new Error('Missing required argument: query');
      }
      return performanceReviewPrompt(args.database, args.query);

    default:
      throw new Error(`Unknown prompt: ${name}`);
  }
}
