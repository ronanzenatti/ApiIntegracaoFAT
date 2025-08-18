namespace ApiIntegracao.DTOs.Responses
{
    /// <summary>
    /// Metadados de paginação
    /// </summary>
    public class PaginationMetaDto
    {
        /// <summary>
        /// Informações de paginação
        /// </summary>
        public PaginationDto Pagination { get; set; } = new();

        /// <summary>
        /// Timestamp da última sincronização
        /// </summary>
        public DateTime? LastSyncTimestamp { get; set; }
    }
}
