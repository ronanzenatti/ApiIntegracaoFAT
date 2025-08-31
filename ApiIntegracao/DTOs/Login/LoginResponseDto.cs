namespace ApiIntegracao.DTOs.Login
{
    public class LoginResponseDto
    {
        public bool Authenticated { get; set; }
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
        public required string Message { get; set; }
    }
}
