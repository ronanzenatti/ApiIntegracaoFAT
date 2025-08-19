using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Aluno
{
    public class AlunoEmailDto
    {
        [Required]
        public string Cpf { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string EmailInstitucional { get; set; } = string.Empty;
    }
}
