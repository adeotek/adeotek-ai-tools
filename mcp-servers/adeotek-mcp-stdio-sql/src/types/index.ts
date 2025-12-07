/**
 * Type definitions for adeotek-sql-mcp
 */

export type DatabaseType = 'mssql' | 'postgres';

export interface DatabaseConfig {
  type: DatabaseType;
  host: string;
  port: number;
  user: string;
  password: string;
  database?: string;
  connectionTimeout?: number;
  requestTimeout?: number;
  ssl?: boolean;
}

export interface DatabaseInfo {
  name: string;
  size?: string;
  owner?: string;
  created?: Date;
  encoding?: string;
}

export interface TableInfo {
  schema: string;
  name: string;
  type: 'table' | 'view' | 'materialized view';
  rowCount?: number;
  sizeEstimate?: string;
}

export interface ColumnInfo {
  name: string;
  type: string;
  nullable: boolean;
  defaultValue?: string;
  isPrimaryKey: boolean;
  isForeignKey: boolean;
  maxLength?: number;
  precision?: number;
  scale?: number;
}

export interface IndexInfo {
  name: string;
  columns: string[];
  isUnique: boolean;
  isPrimary: boolean;
  type?: string;
}

export interface ForeignKeyInfo {
  name: string;
  columns: string[];
  referencedTable: string;
  referencedSchema: string;
  referencedColumns: string[];
  onDelete?: string;
  onUpdate?: string;
}

export interface ConstraintInfo {
  name: string;
  type: 'PRIMARY KEY' | 'FOREIGN KEY' | 'UNIQUE' | 'CHECK' | 'DEFAULT';
  definition: string;
}

export interface TableSchema {
  schema: string;
  table: string;
  columns: ColumnInfo[];
  indexes: IndexInfo[];
  foreignKeys: ForeignKeyInfo[];
  constraints: ConstraintInfo[];
  primaryKey?: string[];
}

export interface QueryResult {
  columns: string[];
  rows: Record<string, unknown>[];
  rowCount: number;
  executionTimeMs: number;
}

export interface QueryPlan {
  plan: string;
  format: 'text' | 'json' | 'xml';
  estimatedCost?: number;
}

export interface ValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
}

export interface ToolResponse<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
  metadata?: Record<string, unknown>;
}

export interface PromptMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
}
