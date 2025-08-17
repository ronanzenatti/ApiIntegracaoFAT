namespace ApiIntegracao.DTOs
{
    public class PaginatedResponseDto<T>
    {
        public List<T> Data { get; set; } = new();
        public PaginationMetaDto Meta { get; set; } = new();
    }
}
