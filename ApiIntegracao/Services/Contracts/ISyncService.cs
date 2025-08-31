namespace ApiIntegracao.Services.Contracts
{
    public interface ISyncService
    {
        Task<SyncResult> SyncCursosAsync();
        Task<SyncResult> SyncTurmasAsync();
        Task<SyncResult> SyncTurmasPorPeriodoAsync(DateTime dataInicial, DateTime dataFinal);
        Task<SyncResult> SyncAlunosAsync();
        Task<SyncResult> SyncAllAsync();
    }

    public class SyncResult
    {
        public bool Success { get; set; }
        public int TotalProcessed { get; set; }
        public int Inserted { get; set; }
        public int Updated { get; set; }
        public int Deleted { get; set; }
        public int? Added { get; set; }
        public List<string> Errors { get; set; } = new();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }
}
