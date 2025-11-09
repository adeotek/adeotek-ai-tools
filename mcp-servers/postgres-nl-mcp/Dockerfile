# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY src/PostgresNaturalLanguageMcp/PostgresNaturalLanguageMcp.csproj src/PostgresNaturalLanguageMcp/
RUN dotnet restore src/PostgresNaturalLanguageMcp/PostgresNaturalLanguageMcp.csproj

# Copy remaining source files
COPY src/ src/

# Build the application
WORKDIR /src/src/PostgresNaturalLanguageMcp
RUN dotnet build PostgresNaturalLanguageMcp.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish PostgresNaturalLanguageMcp.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app/logs

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 5000
EXPOSE 5001

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "PostgresNaturalLanguageMcp.dll"]
