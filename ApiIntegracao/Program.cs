using ApiIntegracao.BackgroundServices;
using ApiIntegracao.Configuration;
using ApiIntegracao.Data;
using ApiIntegracao.Extensions;
using ApiIntegracao.HealthChecks;
using ApiIntegracao.Infrastructure.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; // Adicionado para o OpenApiSecurityScheme
using Serilog;
using Serilog.Events;
using System.Text;

// Configuração do Serilog
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
    Log.Information("Starting API Integração FAT");

    var builder = WebApplication.CreateBuilder(args);

    // Usar Serilog
    builder.Host.UseSerilog();

    // Adicionar configurações
    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables()
        .AddUserSecrets<Program>(optional: true);

    // --- Configuração JWT (Correta como estava) ---
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"];

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });
    builder.Services.AddAuthorization();

    // --- Configurar services ---
    builder.Services.AddControllers();

    // --- CONFIGURAÇÃO DO SWAGGER COM SUPORTE A JWT ---
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "API Integração FAT",
            Version = "v1",
            Description = "API de integração entre CETTPRO e Portal FAT"
        });

        // --- INCLUIR OS COMENTÁRIOS XML ---
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        c.IncludeXmlComments(xmlPath);

        // --- REGISTRAR OS FILTROS ---
        c.SchemaFilter<LoginRequestSchemaFilter>();

        // Adiciona o campo para inserir o token JWT na UI do Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Insira o seu token obtido no endpoint auth \n Usuário: PortalFAT_App \n Secret: UMA_SENHA_FORTE_E_SECRETA_PARA_O_PORTAL_GERADA_AQUI"
        });

        // Aplica a exigência de segurança a todos os endpoints
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] {}
            }
        });

        // Ordena os endpoints, colocando "Auth" no topo
        c.OrderActionsBy(apiDesc =>
        {
            var relativePath = apiDesc.RelativePath ?? "";
            if (relativePath.StartsWith("api/v1/Auth", StringComparison.OrdinalIgnoreCase))
            {
                return "0"; // Coloca no topo
            }
            return apiDesc.RelativePath; // Ordena os outros pelo caminho
        });
    });
    // --- FIM DA CONFIGURAÇÃO DO SWAGGER ---

    // Outros Services
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
        options.SizeLimit = 100;
        options.CompactionPercentage = 0.25;
    });

    // Configuração de opções
    builder.Services.Configure<SyncSettings>(builder.Configuration.GetSection("SyncSettings"));
    builder.Services.Configure<CettproApiSettings>(builder.Configuration.GetSection("CettproApi"));

    // Application Insights
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

// Métodos auxiliares
void ConfigureMiddleware(WebApplication app)
{
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/error");
        app.UseHsts();
    }

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Integração FAT v1");
        c.RoutePrefix = string.Empty;
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.ShowExtensions();

        // --- OCULTAR OS SCHEMAS ---
        c.DefaultModelsExpandDepth(-1);
    });

    app.Use(async (context, next) =>
    {
        if (context.Request.Path == "/")
        {
            context.Response.Redirect("/index.html", permanent: false);
            return;
        }
        await next();
    });


    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex != null || httpContext.Response.StatusCode >= 500)
                return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400)
                return LogEventLevel.Warning;
            if (elapsed > 3000)
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

    // Middlewares de autenticação e autorização
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

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
        if (app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

public partial class Program { }