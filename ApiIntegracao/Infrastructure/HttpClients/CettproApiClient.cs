// Infrastructure/HttpClients/CettproApiClient.cs
using ApiIntegracao.Exceptions;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ApiIntegracao.Infrastructure.HttpClients
{
    public interface ICettproApiClient
    {
        Task<string> AuthenticateAsync();
        Task<T?> GetAsync<T>(string endpoint, string? token = null) where T : class;
        Task<T?> SendAsync<T>(HttpMethod method, string endpoint, object data, string? token = null) where T : class;
        Task InvalidateTokenCache();
    }

    public class CettproApiClient : ICettproApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<CettproApiClient> _logger;

        // Cache do token
        private string? _currentToken;
        private DateTime _tokenExpiry;
        private readonly SemaphoreSlim _authSemaphore = new(1, 1);

        public CettproApiClient(
            HttpClient httpClient,
            IConfiguration config,
            ILogger<CettproApiClient> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;

            // Configurar base address
            _httpClient.BaseAddress = new Uri(_config["CettproApi:BaseUrl"]!);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<string> AuthenticateAsync()
        {
            // Usar semáforo para evitar múltiplas autenticações simultâneas
            await _authSemaphore.WaitAsync();
            try
            {
                // Verificar se token ainda é válido
                if (!string.IsNullOrEmpty(_currentToken) && DateTime.UtcNow < _tokenExpiry)
                {
                    _logger.LogDebug("Usando token em cache, expira em {Expiry}", _tokenExpiry);
                    return _currentToken;
                }

                _logger.LogInformation("Autenticando com CETTPRO...");

                var authData = new
                {
                    Email = _config["CettproApi:Email"],
                    Senha = _config["CettproApi:Password"]
                };

                // Fazer chamada de autenticação
                var response = await _httpClient.GetAsync(
                    $"Autenticar?Email={authData.Email}&Senha={authData.Senha}");

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError(
                        "Falha na autenticação. Status: {Status}, Conteúdo: {Content}",
                        response.StatusCode, content);

                    throw new CettproAuthenticationException(
                        $"Falha na autenticação com CETTPRO: {response.StatusCode}");
                }

                var json = await response.Content.ReadAsStringAsync();
                var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (authResponse == null || string.IsNullOrEmpty(authResponse.Access_token))
                {
                    throw new CettproAuthenticationException("Token não retornado pela CETTPRO");
                }

                // Armazenar token com buffer de segurança
                var bufferSeconds = _config.GetValue<int>("CettproApi:TokenExpirationBuffer", 300);
                _currentToken = authResponse.Access_token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(authResponse.Expires_in - bufferSeconds);

                _logger.LogInformation(
                    "Autenticação bem-sucedida. Token expira em {Expiry}",
                    _tokenExpiry);

                return _currentToken;
            }
            finally
            {
                _authSemaphore.Release();
            }
        }

        public async Task<T?> GetAsync<T>(string endpoint, string? token = null) where T : class
        {
            // Se token não foi fornecido, obter um novo
            token ??= await AuthenticateAsync();

            _logger.LogDebug("GET {Endpoint}", endpoint);

            // Configurar autorização
            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            ;
            var response = await _httpClient.SendAsync(request);

            return await ProcessResponse<T>(response, endpoint);
        }

        public async Task<T?> SendAsync<T>(HttpMethod method, string endpoint, object? data, string? token = null) where T : class
        {
            token ??= await AuthenticateAsync();

            _logger.LogDebug("{Method} {Endpoint}", method.Method, endpoint);

            using var request = new HttpRequestMessage(method, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(data);

            if (data != null)
            {
                request.Content = JsonContent.Create(data);
            }

            var response = await _httpClient.SendAsync(request);

            return await ProcessResponse<T>(response, endpoint);
        }

        private async Task<T?> ProcessResponse<T>(HttpResponseMessage response, string endpoint) where T : class
        {
            var content = await response.Content.ReadAsStringAsync();

            // Log da resposta para debug
            _logger.LogDebug(
                "Resposta de {Endpoint}: Status={Status}, Tamanho={Size}",
                endpoint, response.StatusCode, content?.Length ?? 0);

            // Tratamento específico por código de status
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    return DeserializeContent<T>(content);

                case HttpStatusCode.NoContent:
                    _logger.LogInformation("Endpoint {Endpoint} retornou 204 No Content", endpoint);
                    return default;

                case HttpStatusCode.NotFound:
                    _logger.LogWarning("Recurso não encontrado: {Endpoint}", endpoint);
                    // Para 404, podemos retornar null ou lançar exceção dependendo do contexto
                    if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
                    {
                        // Se esperamos uma lista, retornar lista vazia
                        return Activator.CreateInstance<T>();
                    }
                    throw new CettproResourceNotFoundException(endpoint, typeof(T).Name);

                case HttpStatusCode.Unauthorized:
                    _logger.LogError("Token inválido ou expirado para {Endpoint}", endpoint);
                    // Limpar cache do token
                    await InvalidateTokenCache();
                    throw new CettproAuthenticationException("Token inválido ou expirado");

                case HttpStatusCode.Forbidden:
                    _logger.LogError("Acesso negado ao endpoint {Endpoint}", endpoint);
                    throw new CettproApiException(403, $"Acesso negado ao recurso: {endpoint}", content);

                case HttpStatusCode.BadRequest:
                    _logger.LogError("Requisição inválida para {Endpoint}: {Content}", endpoint, content);
                    throw new CettproApiException(400, $"Requisição inválida: {content}", content);

                case HttpStatusCode.TooManyRequests:
                    // Extrair Retry-After header se disponível
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMinutes(1);
                    _logger.LogWarning("Rate limit atingido. Retry após {Seconds}s", retryAfter.TotalSeconds);
                    throw new CettproRateLimitException(retryAfter);

                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.BadGateway:
                case HttpStatusCode.ServiceUnavailable:
                case HttpStatusCode.GatewayTimeout:
                    _logger.LogError(
                        "Erro no servidor CETTPRO: {Status} - {Content}",
                        response.StatusCode, content);
                    throw new CettproApiException(
                        (int)response.StatusCode,
                        $"Erro no servidor CETTPRO: {response.StatusCode}",
                        content);

                default:
                    _logger.LogWarning(
                        "Status não tratado: {Status} para {Endpoint}",
                        response.StatusCode, endpoint);
                    throw new CettproApiException(
                        (int)response.StatusCode,
                        $"Status não esperado: {response.StatusCode}",
                        content);
            }
        }

        private T? DeserializeContent<T>(string? content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
                return default;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                return JsonSerializer.Deserialize<T>(content, options);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Erro ao deserializar resposta: {Content}", content);
                throw new CettproApiException(
                    500,
                    $"Erro ao processar resposta da CETTPRO: {ex.Message}",
                    content);
            }
        }

        public async Task InvalidateTokenCache()
        {
            await _authSemaphore.WaitAsync();
            try
            {
                _currentToken = null;
                _tokenExpiry = DateTime.MinValue;
                _logger.LogInformation("Cache de token invalidado");
            }
            finally
            {
                _authSemaphore.Release();
            }
        }
    }

    // DTO para resposta de autenticação
    public class AuthResponseDto
    {
        public string Token_type { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int Expires_in { get; set; }
        public string Access_token { get; set; } = string.Empty;
    }
}