namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO de resposta detalhada para turma
    /// </summary>
    public class TurmaDetalhadaDto : TurmaResponseDto
    {
        /// <summary>
        /// Descrição do status da turma
        /// </summary>
        public string StatusDescricao { get; set; } = string.Empty;

        /// <summary>
        /// Total de alunos matriculados
        /// </summary>
        public int TotalMatriculas { get; set; }

        /// <summary>
        /// Total de aulas geradas no cronograma
        /// </summary>
        public int TotalAulasGeradas { get; set; }
    }
}
