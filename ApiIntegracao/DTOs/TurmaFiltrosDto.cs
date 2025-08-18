namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO para filtros de busca de turmas
    /// </summary>
    public class TurmaFiltrosDto
    {
        /// <summary>
        /// ID do curso para filtrar
        /// </summary>
        public Guid? IdCurso { get; set; }

        /// <summary>
        /// Status da turma para filtrar
        /// </summary>
        public int? Status { get; set; }

        /// <summary>
        /// Data de início (filtro a partir de)
        /// </summary>
        public DateTime? DataInicioAPartirDe { get; set; }

        /// <summary>
        /// Data de início (filtro até)
        /// </summary>
        public DateTime? DataInicioAte { get; set; }

        /// <summary>
        /// Apenas turmas ativas
        /// </summary>
        public bool? ApenasAtivas { get; set; }

        /// <summary>
        /// Apenas turmas com código Portal FAT
        /// </summary>
        public bool? ApenasComCodigoFat { get; set; }
    }

}
