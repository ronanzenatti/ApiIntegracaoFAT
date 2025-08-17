using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class CursoSimplificadoDto
    {
        [JsonPropertyName("idCurso")]
        public Guid IdCurso { get; set; }

        [JsonPropertyName("nomeCurso")]
        public string NomeCurso { get; set; } = string.Empty;

        [JsonPropertyName("cargaHoraria")]
        public int CargaHoraria { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("modalidadeId")]
        public Guid? ModalidadeId { get; set; }

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }
    }
}
