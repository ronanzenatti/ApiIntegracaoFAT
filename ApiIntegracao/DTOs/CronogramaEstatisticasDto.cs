namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO para estatísticas dos cronogramas
    /// </summary>
    public class CronogramaEstatisticasDto
    {
        public int TotalCronogramas { get; set; }
        public int TotalTurmas { get; set; }
        public int TotalAulasGeradas { get; set; }
        public double TotalHorasAula { get; set; }
        public Dictionary<string, int> CronogramasPorCurso { get; set; } = new();
        public Dictionary<int, int> AulasPorDiaSemana { get; set; } = new();
        public DateTime? DataPrimeiraAula { get; set; }
        public DateTime? DataUltimaAula { get; set; }
        public double MediaAulasPorCronograma { get; set; }
        public List<CronogramaMaisRecenteDto> CronogramasMaisRecentes { get; set; } = new();
    }
}
