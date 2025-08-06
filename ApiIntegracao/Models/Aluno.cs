using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class Aluno: AuditableEntity
    {
        public Guid IdCettpro { get; set; }
        public required string Nome { get; set; }
        public string? NomeSocial { get; set; }
        public required string Cpf { get; set; }
        public string? Rg { get; set; }
        public DateTime DataNascimento { get; set; }
        public string? Email { get; set; }
        public int Raca { get; set; }
        public string? EmailInstitucional { get; set; }
    }
}
