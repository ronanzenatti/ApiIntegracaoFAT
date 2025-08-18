using ApiIntegracao.DTOs;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ApiIntegracao.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class CronogramasController : ControllerBase
    {
        private readonly ICronogramaService _cronogramaService;
        private readonly ILogger<CronogramasController> _logger;

        public CronogramasController(
            ICronogramaService cronogramaService,
            ILogger<CronogramasController> logger)
        {
            _cronogramaService = cronogramaService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(typeof(CronogramaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CronogramaResponseDto>> GerarCronograma(
            [FromBody] CronogramaRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return ValidationProblem();
                }

                // Validações adicionais
                if (request.DataInicio >= request.DataTermino)
                {
                    ModelState.AddModelError("DataTermino", "Data de término deve ser posterior à data de início");
                    return ValidationProblem();
                }

                if (request.Horarios == null || !request.Horarios.Any())
                {
                    ModelState.AddModelError("Horarios", "Pelo menos um horário deve ser informado");
                    return ValidationProblem();
                }

                var resultado = await _cronogramaService.GerarCronogramaAsync(request);

                if (resultado.Status == "Erro")
                {
                    return BadRequest(new ProblemDetails
                    {
                        Title = "Erro ao gerar cronograma",
                        Detail = resultado.Mensagem,
                        Status = StatusCodes.Status400BadRequest
                    });
                }

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao gerar cronograma");
                return BadRequest(new ProblemDetails
                {
                    Title = "Dados inválidos",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erro de operação ao gerar cronograma");
                return BadRequest(new ProblemDetails
                {
                    Title = "Erro ao processar cronograma",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado ao gerar cronograma para turma {IdTurmaFat}", request?.IdTurmaFat);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao gerar o cronograma. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }
    }
}