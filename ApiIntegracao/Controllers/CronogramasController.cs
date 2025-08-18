using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.ApiIntegracao.DTOs;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace ApiIntegracao.Controllers
{
    /// <summary>
    /// Controller responsável pela geração e gerenciamento de cronogramas de aulas
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class CronogramasController : ControllerBase
    {
        private readonly ICronogramaService _cronogramaService;
        private readonly ILogger<CronogramasController> _logger;

        /// <summary>
        /// Construtor do controller de cronogramas
        /// </summary>
        /// <param name="cronogramaService">Serviço de cronograma</param>
        /// <param name="logger">Logger para registro de eventos</param>
        public CronogramasController(
            ICronogramaService cronogramaService,
            ILogger<CronogramasController> logger)
        {
            _cronogramaService = cronogramaService ?? throw new ArgumentNullException(nameof(cronogramaService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gera um novo cronograma de aulas para uma turma
        /// </summary>
        /// <param name="request">Dados para geração do cronograma</param>
        /// <returns>Cronograma gerado com as datas das aulas</returns>
        /// <response code="200">Cronograma gerado com sucesso</response>
        /// <response code="400">Dados inválidos ou erro na validação</response>
        /// <response code="409">Conflito - Cronograma já existe para a turma/disciplina</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpPost]
        [ProducesResponseType(typeof(CronogramaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CronogramaResponseDto>> GerarCronograma(
            [FromBody] CronogramaRequestDto request)
        {
            try
            {
                // Log da requisição
                _logger.LogInformation(
                    "Iniciando geração de cronograma para Turma: {IdTurmaFat}, Disciplina: {NomeDisciplinaFat}",
                    request?.IdTurmaFat,
                    request?.NomeDisciplinaFat);

                // Validação do ModelState
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Requisição inválida para geração de cronograma. ModelState inválido.");
                    return ValidationProblem(ModelState);
                }

                // Validações adicionais de negócio
                var validationResult = ValidateBusinessRules(request);
                if (validationResult != null)
                {
                    return validationResult;
                }

                // Chamar o serviço para gerar o cronograma
                var resultado = await _cronogramaService.GerarCronogramaAsync(request);

                // Verificar se houve erro no processamento
                if (resultado.Status == "Erro")
                {
                    _logger.LogWarning(
                        "Erro ao gerar cronograma: {Mensagem}",
                        resultado.Mensagem);

                    return BadRequest(new ProblemDetails
                    {
                        Title = "Erro ao gerar cronograma",
                        Detail = resultado.Mensagem,
                        Status = StatusCodes.Status400BadRequest,
                        Instance = HttpContext.Request.Path
                    });
                }

                // Log de sucesso
                _logger.LogInformation(
                    "Cronograma gerado com sucesso. Total de aulas: {TotalAulas}",
                    resultado.Aulas?.Count ?? 0);

                return Ok(resultado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Erro de validação ao gerar cronograma");
                return BadRequest(new ProblemDetails
                {
                    Title = "Dados inválidos",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Erro de operação ao gerar cronograma");

                // Verificar se é um conflito (cronograma já existe)
                if (ex.Message.Contains("já existe", StringComparison.OrdinalIgnoreCase))
                {
                    return Conflict(new ProblemDetails
                    {
                        Title = "Conflito de cronograma",
                        Detail = ex.Message,
                        Status = StatusCodes.Status409Conflict,
                        Instance = HttpContext.Request.Path
                    });
                }

                return BadRequest(new ProblemDetails
                {
                    Title = "Erro ao processar cronograma",
                    Detail = ex.Message,
                    Status = StatusCodes.Status400BadRequest,
                    Instance = HttpContext.Request.Path
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro inesperado ao gerar cronograma para turma {IdTurmaFat}",
                    request?.IdTurmaFat);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro inesperado ao processar sua solicitação. Por favor, tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        /// <summary>
        /// Lista todos os cronogramas gerados
        /// </summary>
        /// <param name="idTurmaFat">Filtrar por ID da turma FAT</param>
        /// <param name="idDisciplinaFat">Filtrar por ID da disciplina FAT</param>
        /// <returns>Lista de cronogramas</returns>
        /// <response code="200">Lista de cronogramas retornada com sucesso</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CronogramaListaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CronogramaListaDto>>> ListarCronogramas(
            [FromQuery] string? idTurmaFat = null,
            [FromQuery] string? idDisciplinaFat = null)
        {
            try
            {
                _logger.LogInformation(
                    "Listando cronogramas. Filtros - Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                    idTurmaFat,
                    idDisciplinaFat);

                var cronogramas = await _cronogramaService.ListarCronogramasAsync(idTurmaFat, idDisciplinaFat);

                _logger.LogInformation(
                    "Encontrados {Count} cronogramas",
                    cronogramas?.Count() ?? 0);

                return Ok(cronogramas);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar cronogramas");

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao listar os cronogramas.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        /// <summary>
        /// Obtém um cronograma específico pelo ID da turma e disciplina
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <returns>Detalhes do cronograma</returns>
        /// <response code="200">Cronograma encontrado</response>
        /// <response code="404">Cronograma não encontrado</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpGet("{idTurmaFat}/{idDisciplinaFat}")]
        [ProducesResponseType(typeof(CronogramaDetalheDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CronogramaDetalheDto>> ObterCronograma(
            string idTurmaFat,
            string idDisciplinaFat)
        {
            try
            {
                _logger.LogInformation(
                    "Buscando cronograma para Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                    idTurmaFat,
                    idDisciplinaFat);

                var cronograma = await _cronogramaService.ObterCronogramaAsync(idTurmaFat, idDisciplinaFat);

                if (cronograma == null)
                {
                    _logger.LogWarning(
                        "Cronograma não encontrado para Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                        idTurmaFat,
                        idDisciplinaFat);

                    return NotFound(new ProblemDetails
                    {
                        Title = "Cronograma não encontrado",
                        Detail = $"Não foi encontrado um cronograma para a turma {idTurmaFat} e disciplina {idDisciplinaFat}",
                        Status = StatusCodes.Status404NotFound,
                        Instance = HttpContext.Request.Path
                    });
                }

                return Ok(cronograma);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao obter cronograma para Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                    idTurmaFat,
                    idDisciplinaFat);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar o cronograma.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        /// <summary>
        /// Exclui um cronograma existente
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <returns>Resultado da exclusão</returns>
        /// <response code="204">Cronograma excluído com sucesso</response>
        /// <response code="404">Cronograma não encontrado</response>
        /// <response code="500">Erro interno do servidor</response>
        [HttpDelete("{idTurmaFat}/{idDisciplinaFat}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ExcluirCronograma(
            string idTurmaFat,
            string idDisciplinaFat)
        {
            try
            {
                _logger.LogInformation(
                    "Excluindo cronograma para Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                    idTurmaFat,
                    idDisciplinaFat);

                var resultado = await _cronogramaService.ExcluirCronogramaAsync(idTurmaFat, idDisciplinaFat);

                if (!resultado)
                {
                    _logger.LogWarning(
                        "Cronograma não encontrado para exclusão. Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                        idTurmaFat,
                        idDisciplinaFat);

                    return NotFound(new ProblemDetails
                    {
                        Title = "Cronograma não encontrado",
                        Detail = $"Não foi encontrado um cronograma para excluir com a turma {idTurmaFat} e disciplina {idDisciplinaFat}",
                        Status = StatusCodes.Status404NotFound,
                        Instance = HttpContext.Request.Path
                    });
                }

                _logger.LogInformation(
                    "Cronograma excluído com sucesso. Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                    idTurmaFat,
                    idDisciplinaFat);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Erro ao excluir cronograma para Turma: {IdTurmaFat}, Disciplina: {IdDisciplinaFat}",
                    idTurmaFat,
                    idDisciplinaFat);

                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao excluir o cronograma.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        /// <summary>
        /// Valida as regras de negócio para geração de cronograma
        /// </summary>
        /// <param name="request">Dados da requisição</param>
        /// <returns>ActionResult com erro ou null se válido</returns>
        private ActionResult? ValidateBusinessRules(CronogramaRequestDto request)
        {
            // Validar datas
            if (request.DataInicio >= request.DataTermino)
            {
                _logger.LogWarning(
                    "Data de término ({DataTermino}) deve ser posterior à data de início ({DataInicio})",
                    request.DataTermino,
                    request.DataInicio);

                ModelState.AddModelError("DataTermino", "Data de término deve ser posterior à data de início");
                return ValidationProblem(ModelState);
            }

            // Validar se a data de início não está no passado
            if (request.DataInicio.Date < DateTime.Today)
            {
                _logger.LogWarning(
                    "Data de início ({DataInicio}) não pode ser anterior à data atual",
                    request.DataInicio);

                ModelState.AddModelError("DataInicio", "Data de início não pode ser anterior à data atual");
                return ValidationProblem(ModelState);
            }

            // Validar período máximo (ex: não pode ser maior que 1 ano)
            var diasTotal = (request.DataTermino - request.DataInicio).TotalDays;
            if (diasTotal > 365)
            {
                _logger.LogWarning(
                    "Período do cronograma ({Dias} dias) excede o máximo permitido de 365 dias",
                    diasTotal);

                ModelState.AddModelError("DataTermino", "O período do cronograma não pode exceder 365 dias");
                return ValidationProblem(ModelState);
            }

            // Validar horários
            if (request.Horarios == null || !request.Horarios.Any())
            {
                _logger.LogWarning("Nenhum horário informado para o cronograma");
                ModelState.AddModelError("Horarios", "Pelo menos um horário deve ser informado");
                return ValidationProblem(ModelState);
            }

            // Validar horários duplicados para o mesmo dia
            var diasDuplicados = request.Horarios
                .GroupBy(h => h.DiaSemana)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (diasDuplicados.Any())
            {
                var diasNomes = string.Join(", ", diasDuplicados.Select(d => ObterNomeDiaSemana(d)));
                _logger.LogWarning(
                    "Horários duplicados para os dias: {Dias}",
                    diasNomes);

                ModelState.AddModelError("Horarios", $"Horários duplicados para os dias: {diasNomes}");
                return ValidationProblem(ModelState);
            }

            // Validar formato dos horários
            foreach (var horario in request.Horarios)
            {
                // Validar hora de início e fim
                if (!TimeSpan.TryParse(horario.Inicio, out var horaInicio))
                {
                    _logger.LogWarning(
                        "Formato inválido para hora de início: {HoraInicio}",
                        horario.Inicio);

                    ModelState.AddModelError("Horarios", $"Formato inválido para hora de início: {horario.Inicio}");
                    return ValidationProblem(ModelState);
                }

                if (!TimeSpan.TryParse(horario.Fim, out var horaFim))
                {
                    _logger.LogWarning(
                        "Formato inválido para hora de fim: {HoraFim}",
                        horario.Fim);

                    ModelState.AddModelError("Horarios", $"Formato inválido para hora de fim: {horario.Fim}");
                    return ValidationProblem(ModelState);
                }

                // Validar se hora de fim é maior que hora de início
                if (horaFim <= horaInicio)
                {
                    _logger.LogWarning(
                        "Hora de fim ({HoraFim}) deve ser maior que hora de início ({HoraInicio}) para o dia {DiaSemana}",
                        horaFim,
                        horaInicio,
                        ObterNomeDiaSemana(horario.DiaSemana));

                    ModelState.AddModelError("Horarios",
                        $"Hora de fim deve ser maior que hora de início para {ObterNomeDiaSemana(horario.DiaSemana)}");
                    return ValidationProblem(ModelState);
                }

                // Validar duração mínima da aula (ex: 30 minutos)
                var duracao = horaFim - horaInicio;
                if (duracao.TotalMinutes < 30)
                {
                    _logger.LogWarning(
                        "Duração da aula ({Duracao} minutos) menor que o mínimo permitido para o dia {DiaSemana}",
                        duracao.TotalMinutes,
                        ObterNomeDiaSemana(horario.DiaSemana));

                    ModelState.AddModelError("Horarios",
                        $"A duração mínima da aula deve ser de 30 minutos para {ObterNomeDiaSemana(horario.DiaSemana)}");
                    return ValidationProblem(ModelState);
                }

                // Validar duração máxima da aula (ex: 8 horas)
                if (duracao.TotalHours > 8)
                {
                    _logger.LogWarning(
                        "Duração da aula ({Duracao} horas) maior que o máximo permitido para o dia {DiaSemana}",
                        duracao.TotalHours,
                        ObterNomeDiaSemana(horario.DiaSemana));

                    ModelState.AddModelError("Horarios",
                        $"A duração máxima da aula deve ser de 8 horas para {ObterNomeDiaSemana(horario.DiaSemana)}");
                    return ValidationProblem(ModelState);
                }
            }

            return null;
        }

        /// <summary>
        /// Obtém o nome do dia da semana
        /// </summary>
        /// <param name="diaSemana">Número do dia (0=Domingo, 6=Sábado)</param>
        /// <returns>Nome do dia da semana</returns>
        private static string ObterNomeDiaSemana(int diaSemana)
        {
            return diaSemana switch
            {
                0 => "Domingo",
                1 => "Segunda-feira",
                2 => "Terça-feira",
                3 => "Quarta-feira",
                4 => "Quinta-feira",
                5 => "Sexta-feira",
                6 => "Sábado",
                _ => $"Dia {diaSemana}"
            };
        }
    }
}