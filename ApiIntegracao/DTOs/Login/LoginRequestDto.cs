using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Login
{
    public class LoginRequestDto
    {
        [Required]
        public string ClientId { get; set; }

        [Required]
        public string ClientSecret { get; set; }
    }
}
