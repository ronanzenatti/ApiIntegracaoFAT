using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Frequencia
{
    public class JustificativaDto
    {
        [Required]
        public string CpfAluno { get; set; } = string.Empty;

        [Required]
        public string TextoJustificativa { get; set; } = string.Empty;
    }
}