namespace ApiIntegracao.DTOs.Login
{
    public class LoginResponseDto
    {
        public bool Authenticated { get; set; }
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public string Message { get; set; }
    }
}
