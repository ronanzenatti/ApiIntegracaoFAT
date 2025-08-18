using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class UnidadeEnsinoDto
    {
        [JsonPropertyName("idUnidadeEnsino")]
        public Guid IdUnidadeEnsino { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("cnpj")]
        public string Cnpj { get; set; } = string.Empty;

        [JsonPropertyName("nomeFantasia")]
        public string NomeFantasia { get; set; } = string.Empty;
    }
}
