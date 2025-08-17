using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class ModalidadeDto
    {
        [JsonPropertyName("idModalidade")]
        public Guid IdModalidade { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("sigla")]
        public string Sigla { get; set; } = string.Empty;
    }
}
