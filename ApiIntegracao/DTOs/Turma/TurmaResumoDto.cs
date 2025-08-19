namespace ApiIntegracao.DTOs.Turma
{
    /// <summary>
    /// DTO para resumo de turma (listagens simplificadas)
    /// </summary>
    public class TurmaResumoDto
    {
        /// <summary>
        /// ID da turma
        /// </summary>
        public Guid IdTurma { get; set; }

        /// <summary>
        /// Nome da turma
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Nome do curso
        /// </summary>
        public string NomeCurso { get; set; } = string.Empty;

        /// <summary>
        /// Data de início
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Status da turma
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Descrição do status
        /// </summary>
        public string StatusDescricao { get; set; } = string.Empty;

        /// <summary>
        /// Total de alunos matriculados
        /// </summary>
        public int TotalAlunos { get; set; }
    }
}
