namespace ApiIntegracao.Configuration
{
    public class SyncSettings
    {
        public bool AutoSyncEnabled { get; set; } = true;
        public int SyncIntervalHours { get; set; } = 6;
        public int MaxRetryAttempts { get; set; } = 3;
    }
}
