namespace ApiIntegracao.Configuration
{
    public class CettproApiSettings
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
