using ApiIntegracao.Configuration;
using ApiIntegracao.Services.Contracts;
using Microsoft.Extensions.Options;

namespace ApiIntegracao.BackgroundServices;

public class SyncBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncBackgroundService> _logger;
    private readonly IOptions<SyncSettings> _syncSettings;


    public SyncBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SyncBackgroundService> logger,
        IOptions<SyncSettings> syncSettings)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _syncSettings = syncSettings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue<bool>("SyncSettings:AutoSyncEnabled"))
        {
            _logger.LogInformation("Sincronização automática está desabilitada");
            return;
        }

        var intervalHours = _configuration.GetValue<int>("SyncSettings:SyncIntervalHours", 12);
        var delay = TimeSpan.FromHours(intervalHours);

        // Aguardar 30 segundos antes de iniciar
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

                _logger.LogInformation("Iniciando sincronização automática...");

                // MODIFICADO: Usar novo método de turmas ativas ao invés do método antigo
                var tasks = new[]
                {
                        syncService.SyncCursosAsync(),
                        syncService.SyncTurmasAtivasAsync(), // ALTERADO AQUI
                        syncService.SyncAlunosAsync()
                    };

                var results = await Task.WhenAll(tasks);

                if (results.All(r => r.Success))
                    _logger.LogInformation("Sincronização automática concluída com sucesso");
                else
                    _logger.LogWarning("Sincronização automática concluída com alguns erros");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização automática");
            }

            await Task.Delay(delay, stoppingToken);
        }
    }

    private async Task DoWork(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting synchronization at {Time}", DateTimeOffset.Now);

        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

                // Sincroniza Cursos
                _logger.LogInformation("Synchronizing courses...");
                var cursosResult = await syncService.SyncCursosAsync();
                _logger.LogInformation("Courses sync completed. Added: {Added}, Updated: {Updated}, Errors: {Errors}",
                    cursosResult.Added, cursosResult.Updated, cursosResult.Errors);

                // Sincroniza Turmas
                _logger.LogInformation("Synchronizing classes...");
                var turmasResult = await syncService.SyncTurmasAsync();
                _logger.LogInformation("Classes sync completed. Added: {Added}, Updated: {Updated}, Errors: {Errors}",
                    turmasResult.Added, turmasResult.Updated, turmasResult.Errors);

                // Sincroniza Alunos
                _logger.LogInformation("Synchronizing students...");
                var alunosResult = await syncService.SyncAlunosAsync();
                _logger.LogInformation("Students sync completed. Added: {Added}, Updated: {Updated}, Errors: {Errors}",
                    alunosResult.Added, alunosResult.Updated, alunosResult.Errors);

                _logger.LogInformation("Synchronization completed successfully at {Time}", DateTimeOffset.Now);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during synchronization");

            // Não relança a exceção para não parar o BackgroundService
            // O próximo ciclo tentará novamente
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Sync Background Service is stopping");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}

// Classe para resultados de sincronização
public class SyncResult
{
    public int Added { get; set; }
    public int Updated { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = new();
}