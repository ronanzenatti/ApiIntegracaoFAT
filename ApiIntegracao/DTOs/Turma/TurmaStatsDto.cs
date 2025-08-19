namespace ApiIntegracao.DTOs.Turma
{
    /// <summary>
    /// DTO para estatísticas de turmas
    /// </summary>
    public class TurmaStatsDto
    {
        /// <summary>
        /// Total de turmas cadastradas
        /// </summary>
        public int TotalTurmas { get; set; }

        /// <summary>
        /// Total de turmas abertas para inscrições
        /// </summary>
        public int TurmasAbertas { get; set; }

        /// <summary>
        /// Total de turmas em execução
        /// </summary>
        public int TurmasEmExecucao { get; set; }

        /// <summary>
        /// Total de turmas finalizadas
        /// </summary>
        public int TurmasFinalizadas { get; set; }

        /// <summary>
        /// Total de turmas canceladas
        /// </summary>
        public int TurmasCanceladas { get; set; }

        /// <summary>
        /// Total de turmas com código do Portal FAT
        /// </summary>
        public int TurmasComCodigoFat { get; set; }

        /// <summary>
        /// Total de matrículas relacionadas
        /// </summary>
        public int TotalMatriculas { get; set; }

        /// <summary>
        /// Total de aulas geradas
        /// </summary>
        public int TotalAulasGeradas { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime UltimaAtualizacao { get; set; }
    }
}
