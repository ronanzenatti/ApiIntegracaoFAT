using ApiIntegracao.BackgroundServices;
using ApiIntegracao.Configuration;
using ApiIntegracao.Data;
using ApiIntegracao.Extensions;
using ApiIntegracao.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Configura��o do Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "ApiIntegracaoFAT")
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} [{SourceContext}]{NewLine}{Exception}")
    .WriteTo.File("logs/apiintegracao-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10_485_760, // 10MB
        rollOnFileSizeLimit: true)
    .CreateLogger();

try
{
    Log.Information("Starting API Integra��o FAT");

    var builder = WebApplication.CreateBuilder(args);

    // Usar Serilog
    builder.Host.UseSerilog();

    // Adicionar configura��es
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>(optional: true); // Para desenvolvimento seguro

    // Configurar services usando extension methods
    builder.Services.AddControllers();
    builder.Services.AddSwagger();
    builder.Services.AddDatabase(builder.Configuration);
    builder.Services.AddCettproHttpClient(builder.Configuration);
    builder.Services.AddApiServices();
    builder.Services.AddCustomCors(builder.Configuration);

    // Background Service
    builder.Services.AddHostedService<SyncBackgroundService>();

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApiIntegracaoDbContext>("database", tags: new[] { "ready" })
        .AddCheck<CettproApiHealthCheck>("cettpro_api", tags: new[] { "live" });

    // Memory Cache
    builder.Services.AddMemoryCache(options =>
    {
        options.SizeLimit = 100; // Limite de 100 itens
        options.CompactionPercentage = 0.25; // Compactar 25% quando atingir o limite
    });

    // Configura��o de op��es
    builder.Services.Configure<SyncSettings>(builder.Configuration.GetSection("SyncSettings"));
    builder.Services.Configure<CettproApiSettings>(builder.Configuration.GetSection("CettproApi"));

    // Application Insights (opcional)
    var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
    if (!string.IsNullOrEmpty(appInsightsConnectionString))
    {
        builder.Services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = appInsightsConnectionString;
            options.EnableAdaptiveSampling = true;
            options.EnableDependencyTrackingTelemetryModule = true;
        });
    }

    var app = builder.Build();

    // Configure middleware pipeline
    ConfigureMiddleware(app);

    // Aplicar migrations em desenvolvimento
    if (app.Environment.IsDevelopment())
    {
        await ApplyMigrationsAsync(app);
    }

    Log.Information($"Environment: {app.Environment.EnvironmentName}");
    Log.Information($"Database: {builder.Configuration.GetConnectionString("DefaultConnection")?.Split(';')[0]}");

    await app.RunAsync();
}
catch (HostAbortedException)
{
    // Esperado quando o host � parado graciosamente
    Log.Information("Host shutdown gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// M�todos auxiliares locais
void ConfigureMiddleware(WebApplication app)
{
    // Exception handling
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    // Swagger
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Integra��o FAT v1");
            c.RoutePrefix = string.Empty;
            c.DisplayRequestDuration();
            c.EnableDeepLinking();
            c.ShowExtensions();
        });
    }

    // Request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode >= 500)
                return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400)
                return LogEventLevel.Warning;
            if (elapsed > 3000) // Requisi��es lentas
                return LogEventLevel.Warning;
            return LogEventLevel.Information;
        };
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
        };
    });

    // Security headers
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });

    app.UseHttpsRedirection();
    app.UseCors("AllowPortalFat");

    // Rate limiting (quando implementar)
    // app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health checks com UI
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(x => new
                {
                    name = x.Key,
                    status = x.Value.Status.ToString(),
                    description = x.Value.Description,
                    duration = x.Value.Duration.TotalMilliseconds
                })
            });
            await context.Response.WriteAsync(result);
        }
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live")
    });
}

async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ApiIntegracaoDbContext>();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        if (pendingMigrations.Any())
        {
            Log.Information($"Applying {pendingMigrations.Count()} pending migrations");
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        else
        {
            Log.Information("No pending migrations");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred while migrating the database");
        // Em desenvolvimento, voc� pode querer que a aplica��o falhe se as migrations n�o funcionarem
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

// Tornar a classe Program p�blica para testes
public partial class Program { }