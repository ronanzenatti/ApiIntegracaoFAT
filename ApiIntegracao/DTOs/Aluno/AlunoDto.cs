using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs.Aluno
{
    public class AlunoDto
    {
        [JsonPropertyName("idAluno")]
        public Guid IdAluno { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("nomeSocial")]
        public string? NomeSocial { get; set; }

        [JsonPropertyName("nomePai")]
        public string? NomePai { get; set; }

        [JsonPropertyName("nomeMae")]
        public string? NomeMae { get; set; }

        [JsonPropertyName("cpf")]
        public string Cpf { get; set; } = string.Empty;

        [JsonPropertyName("rg")]
        public string? Rg { get; set; }

        [JsonPropertyName("municipioId")]
        public Guid? MunicipioId { get; set; }

        [JsonPropertyName("dataNascimento")]
        public DateTime DataNascimento { get; set; }

        [JsonPropertyName("genero")]
        public int Genero { get; set; }

        [JsonPropertyName("sexo")]
        public int Sexo { get; set; }

        [JsonPropertyName("nacionalidade")]
        public string? Nacionalidade { get; set; }

        [JsonPropertyName("estadoCivil")]
        public int EstadoCivil { get; set; }

        [JsonPropertyName("raca")]
        public int Raca { get; set; }

        [JsonPropertyName("eMail")]
        public string? Email { get; set; }
    }
}
