using ApiIntegracao.DTOs.Frequencia;
using ApiIntegracao.Exceptions;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ApiIntegracao.Controllers
{
    /// <summary>
    /// Controller responsável pelo processamento de frequência das turmas.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class FrequenciaController : ControllerBase
    {
        private readonly IFrequenciaService _frequenciaService;
        private readonly ILogger<FrequenciaController> _logger;

        public FrequenciaController(
            IFrequenciaService frequenciaService,
            ILogger<FrequenciaController> logger)
        {
            _frequenciaService = frequenciaService;
            _logger = logger;
        }

        /// <summary>
        /// Processa o arquivo de frequência de uma aula.
        /// </summary>
        /// <remarks>
        /// Este endpoint realiza um processo transacional que inclui:
        /// 1.  **Atualização de Alunos:** Atualiza os e-mails institucionais dos alunos fornecidos.
        /// 2.  **Processamento do Arquivo:** Lê e interpreta o arquivo de presença (CSV ou XLSX).
        /// 3.  **Lógica de Frequência:** Mapeia a presença, ausência e justificativas dos alunos.
        /// 4.  **Envio para CETTPRO:** Envia os dados de frequência para a API da CETTPRO.
        /// A requisição deve ser do tipo `multipart/form-data`, contendo os dados da requisição em um campo 'dados' (JSON) e o arquivo de presença em um campo 'arquivoFrequencia'.
        /// </remarks>
        /// <returns>Resultado do processamento da frequência.</returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(FrequenciaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<FrequenciaResponseDto>> ProcessarFrequencia()
        {
            try
            {
                var form = await Request.ReadFormAsync();
                var dadosJson = form["dados"].FirstOrDefault();
                var arquivoFrequencia = form.Files.GetFile("arquivoFrequencia");

                if (string.IsNullOrWhiteSpace(dadosJson))
                {
                    _logger.LogWarning("O campo 'dados' da requisição de frequência está vazio.");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Dados da requisição ausentes",
                        Detail = "O campo 'dados' contendo o JSON da requisição é obrigatório."
                    });
                }

                if (arquivoFrequencia == null || arquivoFrequencia.Length == 0)
                {
                    _logger.LogWarning("O arquivo de frequência não foi enviado.");
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Arquivo de frequência ausente",
                        Detail = "O campo 'arquivoFrequencia' é obrigatório."
                    });
                }

                FrequenciaRequestDto? request;
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    request = JsonSerializer.Deserialize<FrequenciaRequestDto>(dadosJson, options);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Erro ao deserializar o JSON do campo 'dados'. JSON: {Json}", dadosJson);
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Formato JSON inválido",
                        Detail = "O conteúdo do campo 'dados' não é um JSON válido."
                    });
                }

                if (request == null)
                {
                    return BadRequest(new ProblemDetails { Title = "Dados da requisição inválidos" });
                }

                // Força a validação do modelo deserializado
                if (!TryValidateModel(request))
                {
                    return ValidationProblem(ModelState);
                }

                _logger.LogInformation("Iniciando processamento de frequência para a turma {TurmaId} na data {DataAula}", request.IdTurma, request.DataAula);

                var resultado = await _frequenciaService.ProcessarFrequenciaAsync(request, arquivoFrequencia);

                _logger.LogInformation("Processamento de frequência para a turma {TurmaId} concluído com status: {Status}", request.IdTurma, resultado.Status);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Argumento inválido ao processar frequência.");
                return BadRequest(new ProblemDetails { Title = "Dados inválidos", Detail = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erro de operação ao processar frequência.");
                return Conflict(new ProblemDetails { Title = "Erro de processamento", Detail = ex.Message });
            }
            catch (CettproApiException ex)
            {
                _logger.LogError(ex, "Erro na comunicação com a API CETTPRO durante o processamento de frequência.");
                return StatusCode(StatusCodes.Status502BadGateway, new ProblemDetails
                {
                    Title = "Erro de comunicação com o serviço parceiro",
                    Detail = $"Não foi possível enviar os dados de frequência para a CETTPRO: {ex.Message}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao processar frequência.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro inesperado. Consulte os logs para mais detalhes."
                });
            }
        }
    }
}