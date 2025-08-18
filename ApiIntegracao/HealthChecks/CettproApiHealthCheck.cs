using ApiIntegracao.Infrastructure.HttpClients;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ApiIntegracao.HealthChecks
{
    /// <summary>
    /// Health check para verificar a disponibilidade da API CETTPRO
    /// </summary>
    public class CettproApiHealthCheck : IHealthCheck
    {
        private readonly ICettproApiClient _cettproClient;
        private readonly ILogger<CettproApiHealthCheck> _logger;
        private readonly IConfiguration _configuration;

        public CettproApiHealthCheck(
            ICettproApiClient cettproClient,
            ILogger<CettproApiHealthCheck> logger,
            IConfiguration configuration)
        {
            _cettproClient = cettproClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var baseUrl = _configuration["CettproApi:BaseUrl"] ?? "API CETTPRO";

            try
            {
                // Tentar autenticar na API CETTPRO como teste de saúde
                var token = await _cettproClient.AuthenticateAsync();

                if (!string.IsNullOrEmpty(token))
                {
                    _logger.LogDebug("CETTPRO API health check passed - authentication successful");

                    // Opcionalmente, fazer uma chamada simples para verificar se a API está respondendo
                    try
                    {
                        // Tentar buscar cursos como teste adicional (limitar a 1 resultado)
                        var testResult = await _cettproClient.GetAsync<object>("Cursos?page=1&perPage=1", token);

                        return HealthCheckResult.Healthy(
                            "CETTPRO API is responsive and authenticated",
                            new Dictionary<string, object>
                            {
                                ["endpoint"] = baseUrl,
                                ["authenticated"] = true,
                                ["timestamp"] = DateTime.UtcNow
                            });
                    }
                    catch (Exception testEx)
                    {
                        // Autenticação funcionou, mas chamada de teste falhou
                        _logger.LogWarning(testEx, "CETTPRO API authenticated but test call failed");

                        return HealthCheckResult.Degraded(
                            "CETTPRO API authenticated but may have issues",
                            data: new Dictionary<string, object>
                            {
                                ["endpoint"] = baseUrl,
                                ["authenticated"] = true,
                                ["test_failed"] = true,
                                ["timestamp"] = DateTime.UtcNow
                            });
                    }
                }

                _logger.LogWarning("CETTPRO API health check failed - authentication returned empty token");
                return HealthCheckResult.Unhealthy(
                    "CETTPRO API authentication failed",
                    data: new Dictionary<string, object>
                    {
                        ["endpoint"] = baseUrl,
                        ["authenticated"] = false,
                        ["timestamp"] = DateTime.UtcNow
                    });
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Network error during CETTPRO API health check");
                return HealthCheckResult.Unhealthy(
                    $"Network error connecting to CETTPRO API: {httpEx.Message}",
                    exception: httpEx,
                    data: new Dictionary<string, object>
                    {
                        ["endpoint"] = baseUrl,
                        ["error_type"] = "network",
                        ["timestamp"] = DateTime.UtcNow
                    });
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("CETTPRO API health check timed out");
                return HealthCheckResult.Unhealthy(
                    "CETTPRO API request timed out",
                    data: new Dictionary<string, object>
                    {
                        ["endpoint"] = baseUrl,
                        ["error_type"] = "timeout",
                        ["timestamp"] = DateTime.UtcNow
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during CETTPRO API health check");
                return HealthCheckResult.Unhealthy(
                    $"Unexpected error checking CETTPRO API: {ex.Message}",
                    exception: ex,
                    data: new Dictionary<string, object>
                    {
                        ["endpoint"] = baseUrl,
                        ["error_type"] = "unexpected",
                        ["error_message"] = ex.Message,
                        ["timestamp"] = DateTime.UtcNow
                    });
            }
        }
    }
}