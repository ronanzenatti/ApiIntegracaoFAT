namespace ApiIntegracao.DTOs
{

    public class FrequenciaResponseDto
    {
        public string Status { get; set; } = string.Empty;
        public string Mensagem { get; set; } = string.Empty;
        public List<string> Inconsistencias { get; set; } = new();
    }
}
