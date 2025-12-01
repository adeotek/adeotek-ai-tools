.PHONY: help build test run clean docker-build docker-up docker-down docker-logs restore format

# Default target
help:
	@echo "PostgreSQL Natural Language MCP Server - Available commands:"
	@echo ""
	@echo "  make build        - Build the solution"
	@echo "  make test         - Run all tests"
	@echo "  make run          - Run the application locally"
	@echo "  make clean        - Clean build artifacts"
	@echo "  make restore      - Restore NuGet packages"
	@echo "  make format       - Format code with dotnet format"
	@echo ""
	@echo "  make docker-build - Build Docker image"
	@echo "  make docker-up    - Start all Docker services"
	@echo "  make docker-down  - Stop all Docker services"
	@echo "  make docker-logs  - View Docker logs"
	@echo ""

# Build the solution
build:
	dotnet build PostgresNaturalLanguageMcp.sln

# Run tests
test:
	dotnet test PostgresNaturalLanguageMcp.sln --verbosity normal

# Run tests with coverage
test-coverage:
	dotnet test PostgresNaturalLanguageMcp.sln /p:CollectCoverage=true /p:CoverageReportFormat=opencover

# Run the application
run:
	cd src/PostgresNaturalLanguageMcp && dotnet run

# Clean build artifacts
clean:
	dotnet clean PostgresNaturalLanguageMcp.sln
	rm -rf src/PostgresNaturalLanguageMcp/bin src/PostgresNaturalLanguageMcp/obj
	rm -rf src/PostgresNaturalLanguageMcp.Tests/bin src/PostgresNaturalLanguageMcp.Tests/obj

# Restore NuGet packages
restore:
	dotnet restore PostgresNaturalLanguageMcp.sln

# Format code
format:
	dotnet format PostgresNaturalLanguageMcp.sln

# Build Docker image
docker-build:
	docker build -t postgres-nl-mcp:latest .

# Start Docker services
docker-up:
	docker-compose up -d

# Stop Docker services
docker-down:
	docker-compose down

# Stop Docker services and remove volumes
docker-down-volumes:
	docker-compose down -v

# View Docker logs
docker-logs:
	docker-compose logs -f

# Rebuild and restart Docker services
docker-restart: docker-down docker-build docker-up

# Watch mode (auto-reload on changes)
watch:
	cd src/PostgresNaturalLanguageMcp && dotnet watch run

# Watch mode for tests
watch-test:
	dotnet watch test

# Database commands
db-connect:
	docker exec -it postgres-nl-mcp-db psql -U postgres -d testdb

db-backup:
	docker exec postgres-nl-mcp-db pg_dump -U postgres testdb > backup_$$(date +%Y%m%d_%H%M%S).sql

db-logs:
	docker logs -f postgres-nl-mcp-db
