namespace ApiIntegracao.DTOs
{
    public class ParticipanteArquivoDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? EntradaAula { get; set; }
        public DateTime? SaidaAula { get; set; }
        public int DuracaoMinutos { get; set; }
    }
}

