// Program.cs
using ApiIntegracao.Data;
using ApiIntegracao.Infrastructure.HttpClients;
using ApiIntegracao.Infrastructure.FileProcessing;
using ApiIntegracao.Services.Contracts;
using ApiIntegracao.Services.Implementations;
using ApiIntegracao.BackgroundServices;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/api-integracao-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .CreateLogger();

builder.Host.UseSerilog();

// Configurar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "API Integradora FAT",
        Version = "v1",
        Description = "API para integração entre CETTPRO e Portal FAT"
    });
});

// Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApiIntegracaoDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
           .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
           .EnableDetailedErrors(builder.Environment.IsDevelopment());
});

// HTTP Client com Polly
builder.Services.AddHttpClient<ICettproApiClient, CettproApiClient>()
    .AddPolicyHandler(GetRetryPolicy());

// Registrar serviços
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<ICronogramaService, CronogramaService>();
builder.Services.AddScoped<IFrequenciaService, FrequenciaService>();
builder.Services.AddScoped<IFileParser, AttendanceFileParser>();
builder.Services.AddScoped<IEmailUpdater, EmailUpdater>();
builder.Services.AddScoped<IAttendanceProcessor, AttendanceProcessor>();

// Background Service (apenas se habilitado)
if (builder.Configuration.GetValue<bool>("SyncSettings:AutoSyncEnabled"))
{
    builder.Services.AddHostedService<SyncBackgroundService>();
}

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApiIntegracaoDbContext>("database")
    .AddUrlGroup(new Uri(builder.Configuration["CettproApi:BaseUrl"]), "cettpro-api");

// Application Insights (se configurado)
if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:InstrumentationKey"]))
{
    builder.Services.AddApplicationInsightsTelemetry();
}

var app = builder.Build();

// Configurar pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Aplicar migrations automaticamente apenas em desenvolvimento
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApiIntegracaoDbContext>();

    try
    {
        Log.Information("Aplicando migrations em desenvolvimento...");
        context.Database.Migrate();
        Log.Information("Migrations aplicadas com sucesso");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Erro ao aplicar migrations");
        throw;
    }
}
else
{
    // Em produção, validar se o banco está atualizado
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApiIntegracaoDbContext>();

    var pendingMigrations = context.Database.GetPendingMigrations();
    if (pendingMigrations.Any())
    {
        Log.Warning("Existem {Count} migrations pendentes: {Migrations}",
            pendingMigrations.Count(),
            string.Join(", ", pendingMigrations));

        // Não falhar a aplicação, mas logar o aviso
        // As migrations devem ser executadas pelo pipeline
    }
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();

// Health check endpoint
app.MapHealthChecks("/health");

app.UseAuthorization();
app.MapControllers();

Log.Information("API Integradora FAT iniciada em {Environment}", app.Environment.EnvironmentName);

app.Run();