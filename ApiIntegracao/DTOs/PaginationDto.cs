namespace ApiIntegracao.DTOs
{
    public class PaginationDto
    {
        public int TotalItems { get; set; }
        public int PerPage { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
    }
}
