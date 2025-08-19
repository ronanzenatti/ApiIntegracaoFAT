using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiIntegracao.Controllers
{
    /// <summary>
    /// Controller para verificação de saúde e disponibilidade da API.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Verifica se a API está online e respondendo a requisições.
        /// </summary>
        /// <remarks>
        /// Este é um endpoint de "ping" básico. Para um relatório de saúde detalhado 
        /// (incluindo status do banco de dados e da API CETTPRO), acesse o endpoint `/health`.
        /// </remarks>
        /// <returns>Status de operação da API.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public IActionResult GetHealthStatus()
        {
            try
            {
                var response = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Message = "API Integração FAT está operacional."
                };

                _logger.LogInformation("Health check 'ping' executado com sucesso.");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ocorreu um erro crítico no endpoint de health check.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro Crítico de Saúde",
                    Detail = "A API encontrou um erro crítico e pode estar inoperante."
                });
            }
        }
    }
}