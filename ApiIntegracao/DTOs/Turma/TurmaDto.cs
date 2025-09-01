using ApiIntegracao.DTOs.Curso;
using ApiIntegracao.DTOs.Unidade;
using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs.Turma
{
    public class TurmaDto
    {
        [JsonPropertyName("idTurma")]
        public Guid IdTurma { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("dataInicio")]
        public DateTime DataInicio { get; set; }

        [JsonPropertyName("dataTermino")]
        public DateTime? DataTermino { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("cursos")]
        public List<CursoSimplificadoDto> Cursos { get; set; } = new();

        [JsonPropertyName("unidadedeensino")]
        public List<UnidadeEnsinoDto> UnidadeDeEnsino { get; set; } = new();

        // Propriedades auxiliares para compatibilidade
        public Guid? CursoId => Cursos?.FirstOrDefault()?.IdCurso;
        public string? NomeCurso => Cursos?.FirstOrDefault()?.NomeCurso;
    }
}
