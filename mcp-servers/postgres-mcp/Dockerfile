# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
# Install curl for healthcheck and clean up in same layer to reduce image size
RUN apk add --no-cache curl=~8
# Create non-root user and group with explicit UID/GID
RUN addgroup -g 1001 -S appgroup && \
    adduser -u 1001 -S appuser -G appgroup
# Set working directory and create logs directory with proper permissions
WORKDIR /app
RUN mkdir -p /app/logs && \
    chown -R appuser:appgroup /app
# Switch to non-root user
USER appuser
# Expose ports
EXPOSE 5000
EXPOSE 5001
# Environment variables
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_EnableDiagnostics=0
# Healthcheck using curl
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5000/_health || exit 1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Copy csproj and restore dependencies (cached layer)
COPY ["src/PostgresMcp/PostgresMcp.csproj", "PostgresMcp/"]
RUN dotnet restore "./PostgresMcp/PostgresMcp.csproj"
# Copy source code and build
COPY ./src/PostgresMcp/ ./PostgresMcp/
WORKDIR "/src/PostgresMcp"
RUN dotnet build "./PostgresMcp.csproj" -c $BUILD_CONFIGURATION --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PostgresMcp.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore \
    --no-build \
    /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app
# Copy published files with proper ownership
COPY --from=publish --chown=appuser:appgroup /app/publish .
# Add metadata
LABEL maintainer="Adeotek" \
      description="PostgreSQL MCP Server" \
      version="1.0" \
      org.opencontainers.image.source="https://github.com/adeotek/adeotek-ai-tools/tree/main/mcp-servers/postgres-mcp"
# Run as non-root user
USER appuser
# Use exec form for proper signal handling
ENTRYPOINT ["dotnet", "PostgresMcp.dll"]
