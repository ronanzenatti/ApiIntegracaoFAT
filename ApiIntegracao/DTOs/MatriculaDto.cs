using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    public class MatriculaDto
    {
        [JsonPropertyName("idMatricula")]
        public Guid IdMatricula { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("alunos")]
        public List<AlunoDto> Alunos { get; set; } = new();

        // Propriedades auxiliares
        public Guid? AlunoId => Alunos?.FirstOrDefault()?.IdAluno;
        public string? NomeAluno => Alunos?.FirstOrDefault()?.Nome;
        public string? CpfAluno => Alunos?.FirstOrDefault()?.Cpf;
    }
}
