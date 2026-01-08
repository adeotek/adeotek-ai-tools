# AI Tools

This directory contains standalone utility tools and libraries for AI development.

## Available Tools

### SQL Migration Tool

**Location**: `sql-migration/`
**Status**: ✅ Production Ready

A robust command-line tool for managing SQL database migrations with built-in backup and restore capabilities.

**Key Features**:
- Multi-database support (PostgreSQL and SQL Server)
- Automatic migration tracking via database table
- Optional backup before applying migrations (`--backup` flag)
- Easy restore from latest backup (`--restore` flag)
- Transaction-based migration application
- Checksum validation for migration integrity
- Support for multiple naming conventions

**Quick Start**:
```bash
cd sql-migration
go build -o sql-migration cmd/sql-migration/main.go
export DB_NAME=mydb DB_USER=postgres DB_PASSWORD=secret
./sql-migration --backup
```

[Read full documentation →](./sql-migration/README.md)

---

## Planned Tools

### Vector Database Toolkit
A comprehensive toolkit for working with vector databases.

**Features** (Planned):
- Vector embedding utilities
- Similarity search helpers
- Integration with popular vector DBs (Pinecone, Weaviate, Qdrant)
- Batch processing utilities
- Performance optimization tools

### Prompt Engineering Library
A library of reusable prompt templates and utilities.

**Features** (Planned):
- Prompt template management
- Variable substitution
- Chain-of-thought helpers
- Few-shot learning templates
- Prompt optimization tools

### LLM Response Parser
Tools for parsing and validating LLM responses.

**Features** (Planned):
- JSON extraction from text
- Schema validation
- Retry logic with error correction
- Confidence scoring
- Response caching

### Data Preparation Toolkit
Tools for preparing data for AI/ML workflows.

**Features** (Planned):
- Data cleaning utilities
- Format converters
- Data augmentation
- Train/test split helpers
- Data validation

### Monitoring and Observability
Tools for monitoring AI applications.

**Features** (Planned):
- LLM call tracking
- Cost monitoring
- Performance metrics
- Error tracking
- Usage analytics

## Contributing

If you'd like to contribute a tool, please follow the patterns established in the [CLAUDE.md](../CLAUDE.md) documentation.

Each tool should:
- Be reusable and well-documented
- Include unit tests
- Have clear examples
- Follow best practices
- Be easy to integrate

---

**Last Updated**: 2024-01-08
