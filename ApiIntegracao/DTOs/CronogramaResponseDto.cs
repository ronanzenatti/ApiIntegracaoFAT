namespace ApiIntegracao.DTOs
{
    public class CronogramaResponseDto
    {
        public string Status { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public Guid IdTurma { get; set; }
        public int TotalAulasGeradas { get; set; }
        public List<AulaGeradaDto> AulasGeradas { get; set; } = new();
    }
}
