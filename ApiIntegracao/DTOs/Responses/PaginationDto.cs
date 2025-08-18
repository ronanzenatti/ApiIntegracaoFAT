namespace ApiIntegracao.DTOs.Responses
{
    /// <summary>
    /// Dados de paginação
    /// </summary>
    public class PaginationDto
    {
        /// <summary>
        /// Total de itens
        /// </summary>
        public int TotalItems { get; set; }

        /// <summary>
        /// Itens por página
        /// </summary>
        public int PerPage { get; set; }

        /// <summary>
        /// Página atual
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// Total de páginas
        /// </summary>
        public int TotalPages { get; set; }
    }
}
