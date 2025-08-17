using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class CursoDto
    {
        [JsonPropertyName("idCurso")]
        public Guid IdCurso { get; set; }

        [JsonPropertyName("nomeCurso")]
        public string NomeCurso { get; set; } = string.Empty;

        [JsonPropertyName("carga_horaria")]
        public int CargaHoraria { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }

        [JsonPropertyName("modalidades")]
        public List<ModalidadeDto> Modalidades { get; set; } = new();

        // Propriedade auxiliar para pegar o primeiro ModalidadeId (compatibilidade)
        public Guid? ModalidadeId => Modalidades?.FirstOrDefault()?.IdModalidade;
    }
}
