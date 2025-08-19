namespace ApiIntegracao.DTOs.Curso
{
    /// <summary>
    /// DTO para estatísticas de cursos
    /// </summary>
    public class CursoStatsDto
    {
        /// <summary>
        /// Total de cursos cadastrados
        /// </summary>
        public int TotalCursos { get; set; }

        /// <summary>
        /// Total de cursos ativos
        /// </summary>
        public int CursosAtivos { get; set; }

        /// <summary>
        /// Total de cursos inativos
        /// </summary>
        public int CursosInativos { get; set; }

        /// <summary>
        /// Total de cursos com código do Portal FAT
        /// </summary>
        public int CursosComCodigoFat { get; set; }

        /// <summary>
        /// Total de turmas relacionadas
        /// </summary>
        public int TotalTurmas { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime UltimaAtualizacao { get; set; }
    }

}
