# Base stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/_health || exit 1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/PostgresMcp/PostgresMcp.csproj", "PostgresMcp/"]
RUN dotnet restore "./PostgresMcp/PostgresMcp.csproj"
COPY ./src/PostgresMcp/ ./PostgresMcp/
WORKDIR "/src/PostgresMcp"
RUN dotnet build "./PostgresMcp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./PostgresMcp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN mkdir -p /app/logs
ENTRYPOINT ["dotnet", "PostgresMcp.dll"]
