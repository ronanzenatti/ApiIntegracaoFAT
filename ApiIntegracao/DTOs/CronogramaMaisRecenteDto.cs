namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO para cronogramas mais recentes
    /// </summary>
    public class CronogramaMaisRecenteDto
    {
        public string IdTurmaFat { get; set; } = string.Empty;
        public string NomeTurma { get; set; } = string.Empty;
        public string NomeDisciplina { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public int TotalAulas { get; set; }
    }
}
