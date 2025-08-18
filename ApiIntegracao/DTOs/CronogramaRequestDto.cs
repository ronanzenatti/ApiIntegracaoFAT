using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs
{
    public class CronogramaRequestDto
    {
        [Required]
        public string IdCursoFat { get; set; } = string.Empty;

        [Required]
        public string IdTurmaFat { get; set; } = string.Empty;

        [Required]
        public string IdDisciplinaFat { get; set; } = string.Empty;

        [Required]
        public string NomeDisciplinaFat { get; set; } = string.Empty;

        [Required]
        public DateTime DataInicio { get; set; }

        [Required]
        public DateTime DataTermino { get; set; }

        [Required]
        [MinLength(1)]
        public List<HorarioDto> Horarios { get; set; } = new();
    }
}
