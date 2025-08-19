using ApiIntegracao.DTOs.Curso;
using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class ProgramaDto
    {
        [JsonPropertyName("nomePrograma")]
        public string NomePrograma { get; set; } = string.Empty;

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }

        [JsonPropertyName("cursos")]
        public List<CursoDto> Cursos { get; set; } = new();
    }
}