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
        _logger.LogInformation("Sync Background Service is starting");

        // Verifica se a sincronização automática está habilitada
        if (!_syncSettings.Value.AutoSyncEnabled)
        {
            _logger.LogWarning("Auto sync is disabled. Background service will not run");
            return;
        }

        // Aguarda a aplicação estar pronta
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        _logger.LogInformation("Sync Background Service is running. Interval: {Interval} hours",
            _syncSettings.Value.SyncIntervalHours);

        // Executa a primeira sincronização
        await DoWork(stoppingToken);

        // Configura o timer para execuções periódicas
        var interval = TimeSpan.FromHours(_syncSettings.Value.SyncIntervalHours);

        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await DoWork(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
        }

        _logger.LogInformation("Sync Background Service is stopping");
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