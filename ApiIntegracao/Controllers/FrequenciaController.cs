// Controllers/FrequenciaController.cs
using ApiIntegracao.DTOs;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

[ApiController]
[Route("api/v1/[controller]")]
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

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FrequenciaResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FrequenciaResponseDto>> ProcessarFrequencia()
    {
        try
        {
            // Ler os dados diretamente do formulário da requisição
            var form = await Request.ReadFormAsync();
            var dados = form["dados"].FirstOrDefault();
            var arquivoFrequencia = form.Files.GetFile("arquivoFrequencia");

            // Validações básicas
            if (string.IsNullOrWhiteSpace(dados))
            {
                ModelState.AddModelError("dados", "Dados da frequência são obrigatórios");
                return ValidationProblem();
            }

            if (arquivoFrequencia == null || arquivoFrequencia.Length == 0)
            {
                ModelState.AddModelError("arquivoFrequencia", "Arquivo de frequência é obrigatório");
                return ValidationProblem();
            }

            // Deserializar dados
            FrequenciaRequestDto? request;
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                request = JsonSerializer.Deserialize<FrequenciaRequestDto>(dados, options);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Erro ao deserializar dados de frequência");
                ModelState.AddModelError("dados", "Formato JSON inválido");
                return ValidationProblem();
            }

            if (request == null)
            {
                ModelState.AddModelError("dados", "Dados inválidos");
                return ValidationProblem();
            }

            // Validar modelo
            if (!TryValidateModel(request))
            {
                return ValidationProblem();
            }

            // Processar frequência
            var resultado = await _frequenciaService.ProcessarFrequenciaAsync(
                request,
                arquivoFrequencia);

            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erro de operação ao processar frequência");
            return BadRequest(new ProblemDetails
            {
                Title = "Erro ao processar frequência",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao processar frequência");
            return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
            {
                Title = "Erro interno do servidor",
                Detail = "Ocorreu um erro ao processar a frequência. Tente novamente mais tarde.",
                Status = StatusCodes.Status500InternalServerError
            });
        }
    }
}