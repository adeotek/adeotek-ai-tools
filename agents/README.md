# AI Agents

This directory contains intelligent AI agents that use Large Language Models (LLMs) to accomplish specific tasks.

## Available Agents

### Intelligent HTTP Agent

**Location**: [`/agents/http-agent`](./http-agent)
**Language**: Go
**Status**: ✅ Ready to use

An AI-powered HTTP request tool that acts as an intelligent `curl` alternative. It makes HTTP/HTTPS requests and provides natural language analysis of results.

[Read full documentation →](./http-agent/README.md)

## Planned Agents

### Database Optimization Agent
An intelligent agent that analyzes database performance and suggests optimizations.

**Features** (Planned):
- Analyze slow queries
- Suggest index improvements
- Identify query anti-patterns
- Monitor performance trends
- Auto-tune configuration

### Schema Migration Assistant
An AI agent to help with database schema migrations.

**Features** (Planned):
- Generate migration scripts
- Validate schema changes
- Detect breaking changes
- Suggest rollback strategies
- Test migrations

### Data Quality Analyzer
An agent that monitors and improves data quality.

**Features** (Planned):
- Detect anomalies in data
- Identify duplicate records
- Validate data integrity
- Suggest data cleaning operations
- Monitor data quality metrics

## Contributing

If you'd like to contribute an AI agent, please follow the patterns established in the [CLAUDE.md](../CLAUDE.md) documentation.

Each agent should:
- Be production-ready with proper testing
- Have comprehensive documentation
- Follow security best practices
- Include example usage
- Be containerized (Docker support)

---

**Last Updated**: 2025-11-05
