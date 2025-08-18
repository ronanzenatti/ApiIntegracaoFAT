namespace ApiIntegracao.DTOs
{
    public class PaginationMetaDto
    {
        public PaginationDto Pagination { get; set; } = new();
        public DateTime LastSyncTimestamp { get; set; }
    }
}
