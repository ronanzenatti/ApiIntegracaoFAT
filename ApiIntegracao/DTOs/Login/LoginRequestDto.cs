using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Login
{
    public class LoginRequestDto
    {
        [Required]
        public required string ClientId { get; set; }

        [Required]
        public required string ClientSecret { get; set; }
    }
}
