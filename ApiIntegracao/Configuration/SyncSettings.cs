namespace ApiIntegracao.Configuration;

public class SyncSettings
{
    public bool AutoSyncEnabled { get; set; } = true;
    public int SyncIntervalHours { get; set; } = 1;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelaySeconds { get; set; } = 60;
    public int BatchSize { get; set; } = 100;
    public bool SyncOnStartup { get; set; } = true;
    public TimeSpan StartupDelay { get; set; } = TimeSpan.FromSeconds(10);
}