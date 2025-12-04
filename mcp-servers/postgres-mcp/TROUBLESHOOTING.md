# Build and Startup Troubleshooting

## Known Issues and Fixes

### 1. .NET 10 Target Framework

**Issue**: The project targets `net10.0` which may not be available yet (latest stable is .NET 9).

**Fix Options**:
a) If you have .NET 10 preview/RC installed, ensure the SDK is available:
```bash
dotnet --list-sdks
```

b) If .NET 10 is not available, downgrade to .NET 9:
```xml
<!-- In PostgresMcp.csproj, change: -->
<TargetFramework>net10.0</TargetFramework>
<!-- To: -->
<TargetFramework>net9.0</TargetFramework>
```

And update package versions:
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
<PackageReference Include="Npgsql" Version="9.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
```

### 2. Extension Method Syntax (FIXED)

**Issue**: Used incorrect extension method syntax.

**Status**: ✅ FIXED - Changed from `extension(IEndpointRouteBuilder endpoints)` to proper C# extension method syntax: `public static IEndpointRouteBuilder MapMcpProtocolEndpoints(this IEndpointRouteBuilder endpoints)`

### 3. Missing NuGet Packages

**Issue**: Some package versions may not exist for .NET 10.

**Fix**: Restore packages and check for version conflicts:
```bash
cd src/PostgresMcp
dotnet restore
dotnet list package --outdated
```

If packages fail to restore, downgrade to .NET 9 compatible versions.

### 4. Build Commands

**Clean build**:
```bash
cd mcp-servers/postgres-mcp
dotnet clean
dotnet restore
dotnet build
```

**Run locally**:
```bash
cd src/PostgresMcp
dotnet run
```

**Run with Docker**:
```bash
docker-compose build --no-cache
docker-compose up -d
```

### 5. Common Compilation Errors

**Error: "The type or namespace name 'JsonRpcBatchResponse' could not be found"**
- Status: Should not occur - type is defined in JsonRpcModels.cs

**Error: "Cannot convert from 'method group' to 'Delegate'"**
- Status: Should not occur - all delegates are properly typed

**Error: "'IEndpointRouteBuilder' does not contain a definition for 'MapMcpProtocolEndpoints'"**
- Status: Fixed - using proper extension method syntax

### 6. Runtime Errors

**Error: "Unable to connect to PostgreSQL"**
- Solution: Initialize the server first:
```bash
curl -X POST http://localhost:5000/mcp/initialize \
  -H "Content-Type: application/json" \
  -d '{"host": "localhost", "port": 5432, "username": "postgres", "password": "password"}'
```

**Error: "SSE connection fails"**
- Solution: Ensure proper headers are set (done automatically by the server)
- Check firewall settings for port 5000

### 7. Docker Build Issues

**Error: Docker build fails**
```bash
# Rebuild without cache
docker-compose build --no-cache postgres-mcp

# Check Dockerfile base image
# Ensure it uses compatible .NET version
```

## Verification Steps

1. **Check .NET SDK Version**:
```bash
dotnet --version
# Should show 9.0.x or 10.0.x
```

2. **Restore Packages**:
```bash
cd src/PostgresMcp
dotnet restore
```

3. **Build Project**:
```bash
dotnet build
# Should complete without errors
```

4. **Run Tests** (if available):
```bash
cd ../..
dotnet test
```

5. **Start Server**:
```bash
cd src/PostgresMcp
dotnet run
# Should see: "MCP Server ready"
```

6. **Test Endpoints**:
```bash
# Health check
curl http://localhost:5000/_health

# Server info
curl http://localhost:5000/

# Ping (requires JSON-RPC 2.0)
curl -X POST http://localhost:5000/mcp/v1/messages \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc": "2.0", "id": 1, "method": "ping", "params": {}}'
```

## Quick Fix: Downgrade to .NET 9

If .NET 10 is causing issues, here's a quick downgrade script:

```bash
cd mcp-servers/postgres-mcp/src/PostgresMcp

# Update csproj
sed -i 's/net10.0/net9.0/g' PostgresMcp.csproj
sed -i 's/Version="10.0.0"/Version="9.0.0"/g' PostgresMcp.csproj

# Restore and build
dotnet restore
dotnet build
dotnet run
```

## Package Version Compatibility

### For .NET 9:
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="9.0.0" />
<PackageReference Include="Npgsql" Version="9.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="9.0.0" />
```

### For .NET 10 (when available):
```xml
<PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
<PackageReference Include="Npgsql" Version="10.0.0" />
<PackageReference Include="Serilog.AspNetCore" Version="10.0.0" />
```

## Getting Help

1. Check logs in console output
2. Enable debug logging:
```bash
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
```

3. Review documentation:
   - README.md - User guide
   - CLAUDE.md - Technical documentation
   - examples/sample-requests.md - API examples
   - examples/INTEGRATION_GUIDE.md - Integration guide

4. Check GitHub issues:
   https://github.com/adeotek/adeotek-ai-tools/issues
