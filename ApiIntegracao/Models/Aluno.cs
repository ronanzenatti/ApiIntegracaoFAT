using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class Aluno : AuditableEntity
    {
        public Guid IdCettpro { get; set; }
        public required string Nome { get; set; }
        public string? NomeSocial { get; set; }
        public string? NomePai { get; set; }
        public string? NomeMae { get; set; }

        public required string Cpf { get; set; }
        public string? Rg { get; set; }

        public Guid? MunicipioId { get; set; }

        public DateTime DataNascimento { get; set; }

        public int Genero { get; set; }
        public int Sexo { get; set; }
        public string? Nacionalidade { get; set; }
        public int EstadoCivil { get; set; }

        public int Raca { get; set; }
        public string? Email { get; set; }
        public string? EmailInstitucional { get; set; }
    }
}
