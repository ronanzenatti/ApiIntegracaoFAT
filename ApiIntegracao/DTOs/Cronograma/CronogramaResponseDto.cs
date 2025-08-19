namespace ApiIntegracao.DTOs.Cronograma
{
    /// <summary>
    /// DTO de resposta da geração de cronograma
    /// </summary>
    public class CronogramaResponseDto
    {
        /// <summary>
        /// Status da operação (Sucesso, Erro, Aviso)
        /// </summary>
        public string Status { get; set; } = "Sucesso";

        /// <summary>
        /// Mensagem descritiva do resultado
        /// </summary>
        public string Mensagem { get; set; } = string.Empty;

        /// <summary>
        /// ID da turma onde o cronograma foi gerado
        /// </summary>
        public Guid IdTurma { get; set; }

        /// <summary>
        /// ID da turma no Portal FAT
        /// </summary>
        public string IdTurmaFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome da turma
        /// </summary>
        public string NomeTurma { get; set; } = string.Empty;

        /// <summary>
        /// ID da disciplina no Portal FAT
        /// </summary>
        public string IdDisciplinaFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome da disciplina
        /// </summary>
        public string NomeDisciplina { get; set; } = string.Empty;

        /// <summary>
        /// Total de aulas geradas
        /// </summary>
        public int TotalAulasGeradas { get; set; }

        /// <summary>
        /// Total de horas-aula
        /// </summary>
        public double TotalHorasAula { get; set; }

        /// <summary>
        /// Data da primeira aula
        /// </summary>
        public DateTime? PrimeiraAula { get; set; }

        /// <summary>
        /// Data da última aula
        /// </summary>
        public DateTime? UltimaAula { get; set; }

        /// <summary>
        /// Lista das primeiras aulas geradas (para preview)
        /// </summary>
        public List<AulaGeradaDto> Aulas { get; set; } = new();

        /// <summary>
        /// Timestamp da geração
        /// </summary>
        public DateTime DataGeracao { get; set; } = DateTime.Now;

        /// <summary>
        /// Avisos durante a geração
        /// </summary>
        public List<string> Avisos { get; set; } = new();
    }
}
