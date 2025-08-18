using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs
{
    public class JustificativaDto
    {
        [Required]
        public string CpfAluno { get; set; } = string.Empty;

        [Required]
        public string TextoJustificativa { get; set; } = string.Empty;
    }
}