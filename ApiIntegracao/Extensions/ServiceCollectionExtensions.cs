using ApiIntegracao.Data;
using ApiIntegracao.Infrastructure.FileProcessing;
using ApiIntegracao.Infrastructure.HttpClients;
using ApiIntegracao.Services.Contracts;
using ApiIntegracao.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace ApiIntegracao.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Services da aplicação
        services.AddScoped<IFileParser, AttendanceFileParser>();
        services.AddScoped<ISyncService, SyncService>();
        services.AddScoped<ICronogramaService, CronogramaService>();
        services.AddScoped<IFrequenciaService, FrequenciaService>();
        services.AddScoped<IEmailUpdater, EmailUpdater>();
        services.AddScoped<IAttendanceProcessor, AttendanceProcessor>();

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApiIntegracaoDbContext>(options =>
            options.UseMySql(connectionString,
                ServerVersion.AutoDetect(connectionString),
                mysqlOptions =>
                {
                    mysqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);

                    // Adicionar timeout de comando
                    mysqlOptions.CommandTimeout(30);
                }));

        return services;
    }

    public static IServiceCollection AddCettproHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ICettproApiClient, CettproApiClient>(client =>
        {
            var baseUrl = configuration["CettproApi:BaseUrl"]
                ?? "https://cettpro-appweb-crm-api-externo-hml.azurewebsites.net";

            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            // Adicionar User-Agent para identificação
            client.DefaultRequestHeaders.Add("User-Agent", "ApiIntegracaoFAT/1.0");
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
        });

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowPortalFat", policy =>
            {
                var origins = configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:3000", "https://localhost:3001" };

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetPreflightMaxAge(TimeSpan.FromSeconds(86400)); // Cache preflight por 24h
            });
        });

        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "API Integração FAT",
                Version = "v1",
                Description = "API de integração entre CETTPRO e Portal FAT",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Equipe FAT",
                    Email = "suporte@fat.com.br"
                },
                License = new Microsoft.OpenApi.Models.OpenApiLicense
                {
                    Name = "Uso Interno"
                }
            });

            // Adicionar suporte para XML comments (documentação)
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Adicionar suporte para autenticação (quando implementar)
            // c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { ... });
        });

        return services;
    }
}