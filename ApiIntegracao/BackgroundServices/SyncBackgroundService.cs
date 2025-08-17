using ApiIntegracao.Configuration;
using ApiIntegracao.Services.Contracts;
using Microsoft.Extensions.Options;

namespace ApiIntegracao.BackgroundServices
{
    public class SyncBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SyncBackgroundService> _logger;
        private readonly SyncSettings _syncSettings;
        private Timer? _timer;

        public SyncBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<SyncBackgroundService> logger,
            IOptions<SyncSettings> syncSettings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _syncSettings = syncSettings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SyncBackgroundService iniciado");

            if (!_syncSettings.AutoSyncEnabled)
            {
                _logger.LogInformation("Sincronização automática está desabilitada");
                return;
            }

            // Executar primeira sincronização após 1 minuto do startup
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ExecuteSyncAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro durante sincronização automática");
                }

                // Aguardar o intervalo configurado antes da próxima sincronização
                var delay = TimeSpan.FromHours(_syncSettings.SyncIntervalHours);
                _logger.LogInformation($"Próxima sincronização em {delay.TotalHours} horas");

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task ExecuteSyncAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Iniciando sincronização automática...");

            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<ISyncService>();

            try
            {
                // Executar sincronizações em ordem de dependência
                var cursoResult = await syncService.SyncCursosAsync();
                LogSyncResult("Cursos", cursoResult);

                var turmaResult = await syncService.SyncTurmasAsync();
                LogSyncResult("Turmas", turmaResult);

                var alunoResult = await syncService.SyncAlunosAsync();
                LogSyncResult("Alunos", alunoResult);

                _logger.LogInformation("Sincronização automática concluída com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar sincronização completa");
                throw;
            }
        }

        private void LogSyncResult(string entity, SyncResult result)
        {
            if (result.Success)
            {
                _logger.LogInformation(
                    $"Sincronização de {entity} concluída: " +
                    $"Total: {result.TotalProcessed}, " +
                    $"Inseridos: {result.Inserted}, " +
                    $"Atualizados: {result.Updated}, " +
                    $"Removidos: {result.Deleted}, " +
                    $"Duração: {result.Duration.TotalSeconds:F2}s");
            }
            else
            {
                _logger.LogWarning(
                    $"Sincronização de {entity} falhou: {string.Join(", ", result.Errors)}");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("SyncBackgroundService está parando...");
            _timer?.Change(Timeout.Infinite, 0);
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            base.Dispose();
        }
    }

}