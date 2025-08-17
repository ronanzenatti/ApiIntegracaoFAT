namespace ApiIntegracao.DTOs
{
    public class AlunoResponseDto
    {
        public Guid IdAluno { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? NomeSocial { get; set; }
        public string Cpf { get; set; } = string.Empty;
        public string? Rg { get; set; }
        public DateTime DataNascimento { get; set; }
        public string? Email { get; set; }
        public string? EmailInstitucional { get; set; }
    }
}
