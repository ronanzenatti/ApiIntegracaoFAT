using ApiIntegracao.DTOs.Aluno;
using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Frequencia
{
    public class FrequenciaRequestDto
    {
        [Required]
        public Guid IdTurma { get; set; }

        [Required]
        public DateTime DataAula { get; set; }

        public List<AlunoEmailDto> AlunosParaAtualizar { get; set; } = new();
        public List<JustificativaDto> Justificativas { get; set; } = new();
    }
}