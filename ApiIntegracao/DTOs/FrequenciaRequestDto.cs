using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs
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