using ApiIntegracao.BackgroundServices;
using ApiIntegracao.Configuration;
using ApiIntegracao.Data;
using ApiIntegracao.HealthChecks;
using ApiIntegracao.Infrastructure.FileProcessing;
using ApiIntegracao.Infrastructure.HttpClients;
using ApiIntegracao.Services.Contracts;
using ApiIntegracao.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IFileParser, AttendanceFileParser>();

// Configuração do Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} [{SourceContext}]{NewLine}{Exception}")
    .WriteTo.File("logs/apiintegracao-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "API Integração FAT",
        Version = "v1",
        Description = "API de integração entre CETTPRO e Portal FAT"
    });
});

// Configuração do Entity Framework com MySQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiIntegracaoDbContext>(options =>
    options.UseMySql(connectionString,
        ServerVersion.AutoDetect(connectionString),
        mysqlOptions => mysqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)));

// Configuração do HttpClient para CETTPRO com Resilience
builder.Services.AddHttpClient<ICettproApiClient, CettproApiClient>(client =>
{
    var baseUrl = builder.Configuration["CettproApi:BaseUrl"] ?? "https://cettpro-appweb-crm-api-externo-hml.azurewebsites.net";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});

// Registro de Services
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<ICronogramaService, CronogramaService>();
builder.Services.AddScoped<IFrequenciaService, FrequenciaService>();
builder.Services.AddScoped<IEmailUpdater, EmailUpdater>();
builder.Services.AddScoped<IAttendanceProcessor, AttendanceProcessor>();

// Registro do Background Service
builder.Services.AddHostedService<SyncBackgroundService>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApiIntegracaoDbContext>("database")
    .AddTypeActivatedCheck<CettproApiHealthCheck>("cettpro_api");

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowPortalFat",
        policy =>
        {
            policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                new[] { "http://localhost:3000", "https://localhost:3001" })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

// Configuração de Application Insights (opcional - apenas se estiver configurado)
var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
if (!string.IsNullOrEmpty(appInsightsConnectionString))
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appInsightsConnectionString;
    });
}

// Memory Cache
builder.Services.AddMemoryCache();

// Configuração de opções
builder.Services.Configure<SyncSettings>(builder.Configuration.GetSection("SyncSettings"));
builder.Services.Configure<CettproApiSettings>(builder.Configuration.GetSection("CettproApi"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Integração FAT v1");
        c.RoutePrefix = string.Empty; // Swagger na raiz
    });
}
else
{
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

// Middleware de logging do Serilog
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
    };
});

app.UseHttpsRedirection();

app.UseCors("AllowPortalFat");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health checks endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// Aplicar migrations automaticamente no startup (apenas em desenvolvimento)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<ApiIntegracaoDbContext>();
            await context.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while migrating the database");
        }
    }
}

Log.Information("Starting API Integração FAT");
Log.Information($"Environment: {app.Environment.EnvironmentName}");
Log.Information($"Database: {connectionString?.Split(';')[0]}");

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}