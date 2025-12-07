using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace Adeotek.Mcp.Http.Sql.Extensions;

[ExcludeFromCodeCoverage]
public static class HostBuilderExtensions
{
    public static async Task WebApplicationRunAsync(string[] args,
        Action<WebApplicationBuilder> configureServicesAction,
        Action<WebApplication>? configureBeforeHttpsRedirectionMiddlewares = null,
        Action<WebApplication>? configureAfterHttpsRedirectionMiddlewares = null)
    {
        Log.Logger = BuildLogger(BuildConfiguration());

        try
        {
            Log.Information("Initializing {Assembly}", Assembly.GetExecutingAssembly().FullName);

            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();
            await builder
                .ConfigureServices(configureServicesAction)
                .ConfigurePipeline(app =>
                {
                    // Use Serilog for web server internal logging
                    app.UseSerilogRequestLogging();

                    // Before HTTPS redirection middlewares
                    configureBeforeHttpsRedirectionMiddlewares?.Invoke(app);

                    if (configureAfterHttpsRedirectionMiddlewares is null)
                    {
                        Log.Information(
                            "Service v{Version} running in [{Environment}] mode",
                            Assembly.GetExecutingAssembly().GetName().Version?.ToString(), app.Environment.EnvironmentName);
                        return;
                    }

                    var redirectToHttps =
                        app.Configuration.GetValue("RedirectToHttps", !app.Environment.IsDevelopment());
                    if (redirectToHttps)
                    {
                        app.UseHttpsRedirection();
                    }

                    // After HTTPS redirection middlewares
                    configureAfterHttpsRedirectionMiddlewares.Invoke(app);

                    Log.Information(
                        "Service v{Version} running in [{Environment}] mode with RedirectToHttps ({RedirectToHttps})",
                        Assembly.GetExecutingAssembly().GetName().Version?.ToString(), app.Environment.EnvironmentName,
                        redirectToHttps);
                })
                .RunAsync();
        }
        catch (HostAbortedException e)
        {
            Log.Warning("[Ignore if running ef commands] Error: {ErrorMessage}", e.Message);
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Start-up failed");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
                optional: true)
            .AddEnvironmentVariables()
            .Build();

    private static Serilog.Core.Logger BuildLogger(IConfiguration configuration) =>
        new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(theme: AnsiConsoleTheme.Code)
            .CreateLogger();

    private static WebApplication ConfigureServices(this WebApplicationBuilder builder,
        Action<WebApplicationBuilder> configureServicesAction)
    {
        builder.Host.UseSerilog();
        configureServicesAction.Invoke(builder);
        return builder.Build();
    }

    private static WebApplication ConfigurePipeline(this WebApplication app,
        Action<WebApplication> configureMiddlewares)
    {
        configureMiddlewares.Invoke(app);
        return app;
    }
}
