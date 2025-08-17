using ApiIntegracao.Infrastructure.JsonConverters;
using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class CursoDto
    {
        [JsonPropertyName("idCurso")]
        public Guid IdCurso { get; set; }

        [JsonPropertyName("nomeCurso")]
        public string NomeCurso { get; set; } = string.Empty;

        [JsonPropertyName("cargaHoraria")]
        [JsonConverter(typeof(CargaHorariaToStringConverter))]
        public string? CargaHoraria { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }

        [JsonPropertyName("modalidadeId")]
        [JsonConverter(typeof(StringToNullableGuidConverter))]
        public Guid? ModalidadeId { get; set; }
    }
}