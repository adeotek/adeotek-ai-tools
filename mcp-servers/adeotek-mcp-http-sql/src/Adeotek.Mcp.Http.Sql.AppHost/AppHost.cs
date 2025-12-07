var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Adeotek_Mcp_Http_Sql>("mcpserver")
    .WithHttpHealthCheck("/health");

await builder.Build().RunAsync();
