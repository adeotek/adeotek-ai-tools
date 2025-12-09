# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY src/AdeotekSqlMcp/AdeotekSqlMcp.csproj src/AdeotekSqlMcp/
RUN dotnet restore src/AdeotekSqlMcp/AdeotekSqlMcp.csproj

# Copy source code
COPY src/AdeotekSqlMcp/ src/AdeotekSqlMcp/

# Build
WORKDIR /src/src/AdeotekSqlMcp
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

# Create non-root user
RUN useradd -m -u 1000 mcpuser && chown -R mcpuser:mcpuser /app
USER mcpuser

# Copy from build
COPY --from=build /app/publish .

# Set environment variables
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV LOG_LEVEL=Information

ENTRYPOINT ["dotnet", "AdeotekSqlMcp.dll"]
