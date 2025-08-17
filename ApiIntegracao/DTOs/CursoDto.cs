using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class CursoDto
    {
        [JsonPropertyName("idCurso")]
        public Guid IdCurso { get; set; }

        [JsonPropertyName("nomeCurso")]
        public string NomeCurso { get; set; } = string.Empty;

        // CORREÇÃO: Alterado de int para string? para corresponder à API
        [JsonPropertyName("cargaHoraria")]
        public string? CargaHoraria { get; set; }

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }

        [JsonPropertyName("modalidadeId")]
        public Guid? ModalidadeId { get; set; }
    }
}