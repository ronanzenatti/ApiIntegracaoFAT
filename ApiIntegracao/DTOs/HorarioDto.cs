using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs
{
    public class HorarioDto
    {
        [Range(0, 6)] // 0=Domingo, 6=Sábado
        public int DiaSemana { get; set; }

        [Required]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        public string Inicio { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$")]
        public string Fim { get; set; } = string.Empty;
    }
}
