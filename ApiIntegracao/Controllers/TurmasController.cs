using ApiIntegracao.Data;
using ApiIntegracao.DTOs.Aluno;
using ApiIntegracao.DTOs.Curso;
using ApiIntegracao.DTOs.Matricula;
using ApiIntegracao.DTOs.Responses;
using ApiIntegracao.DTOs.Turma;
using ApiIntegracao.DTOs.Unidade;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiIntegracao.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de turmas
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class TurmasController : ControllerBase
    {
        private readonly ApiIntegracaoDbContext _context;
        private readonly ISyncService _syncService;
        private readonly ILogger<TurmasController> _logger;

        public TurmasController(
            ApiIntegracaoDbContext context,
            ISyncService syncService,
            ILogger<TurmasController> logger)
        {
            _context = context;
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todas as turmas com paginação, busca e filtros
        /// </summary>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="perPage">Itens por página (padrão: 10, máximo: 100)</param>
        /// <param name="search">Termo para busca no nome da turma</param>
        /// <param name="idCurso">Filtro por ID do curso</param>
        /// <param name="status">Filtro por status da turma</param>
        /// <param name="ativo">Filtro por turmas ativas/inativas</param>
        /// <param name="forceSync">Força sincronização antes de retornar dados</param>
        /// <returns>Lista paginada de turmas</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponseDto<TurmaResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedResponseDto<TurmaResponseDto>>> GetTurmas(
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string? search = null,
            [FromQuery] Guid? idCurso = null,
            [FromQuery] int? status = null,
            [FromQuery] bool? ativo = null,
            [FromQuery] bool forceSync = false)
        {
            try
            {
                // Validações de entrada
                if (page < 1)
                {
                    ModelState.AddModelError(nameof(page), "O número da página deve ser maior que zero");
                    return ValidationProblem();
                }

                if (perPage < 1 || perPage > 100)
                {
                    ModelState.AddModelError(nameof(perPage), "Itens por página deve estar entre 1 e 100");
                    return ValidationProblem();
                }

                // Sincronização incremental (conforme especificação)
                if (forceSync)
                {
                    _logger.LogInformation("Executando sincronização forçada de turmas antes da consulta");
                    var syncResult = await _syncService.SyncTurmasAsync();
                    if (!syncResult.Success)
                    {
                        _logger.LogWarning("Sincronização falhou, mas continuando com dados locais: {Errors}",
                            string.Join(", ", syncResult.Errors));
                    }
                }

                // Query base com join do curso
                var query = _context.Turmas
                    .Include(t => t.Curso)
                    .Where(t => t.DeletedAt == null) // Filtrar soft deleted
                    .AsQueryable();

                // Aplicar filtros
                if (idCurso.HasValue)
                {
                    query = query.Where(t => t.CursoId == idCurso.Value);
                }

                if (status.HasValue)
                {
                    query = query.Where(t => t.Status == status.Value);
                }

                // Aplicar busca se fornecida
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    query = query.Where(t =>
                        t.Nome.ToLower().Contains(searchTerm) ||
                        t.Curso.NomeCurso.ToLower().Contains(searchTerm) ||
                        (t.DisciplinaNomePortalFat != null && t.DisciplinaNomePortalFat.ToLower().Contains(searchTerm)) ||
                        (t.IdPortalFat != null && t.IdPortalFat.ToString()!.Contains(searchTerm)));
                }

                // Contar total de itens
                var totalItems = await query.CountAsync();

                // Aplicar paginação e projeção
                var turmas = await query
                    .OrderByDescending(t => t.DataInicio)
                    .ThenBy(t => t.Nome)
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .Select(t => new TurmaResponseDto
                    {
                        IdTurma = t.Id,
                        IdCettpro = t.IdCettpro,
                        Nome = t.Nome,
                        DataInicio = t.DataInicio,
                        DataTermino = t.DataTermino,
                        Status = t.Status,
                        CursoId = t.CursoId,
                        Curso = new CursoBasicoDto
                        {
                            IdCurso = t.Curso.Id,
                            NomeCurso = t.Curso.NomeCurso,
                            IdPortalFat = t.Curso.IdPortalFat
                        },
                        IdPortalFat = t.IdPortalFat,
                        DisciplinaIdPortalFat = t.DisciplinaIdPortalFat,
                        DisciplinaNomePortalFat = t.DisciplinaNomePortalFat,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToListAsync();

                // Obter timestamp da última sincronização
                var lastSync = await _context.SyncLogs
                    .Where(s => s.TipoEntidade == "Turma" && s.Sucesso)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                // Construir resposta paginada
                var response = new PaginatedResponseDto<TurmaResponseDto>
                {
                    Data = turmas,
                    Meta = new PaginationMetaDto
                    {
                        Pagination = new PaginationDto
                        {
                            TotalItems = totalItems,
                            PerPage = perPage,
                            CurrentPage = page,
                            TotalPages = (int)Math.Ceiling((double)totalItems / perPage)
                        },
                        LastSyncTimestamp = lastSync
                    }
                };

                _logger.LogInformation(
                    "Consulta de turmas realizada: {TotalItems} itens, página {Page}/{TotalPages}, filtros: curso={IdCurso}, status={Status}, busca='{Search}'",
                    totalItems, page, response.Meta.Pagination.TotalPages, idCurso, status, search ?? "N/A");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar turmas");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar as turmas. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Obtém uma turma específica por ID
        /// </summary>
        /// <param name="id">ID da turma</param>
        /// <param name="forceSync">Força sincronização antes de retornar dados</param>
        /// <returns>Dados da turma</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(TurmaDetalhadaDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TurmaDetalhadaDto>> GetTurma(
            Guid id,
            [FromQuery] bool forceSync = false)
        {
            try
            {
                // Sincronização incremental se solicitada
                if (forceSync)
                {
                    _logger.LogInformation("Executando sincronização forçada antes de buscar turma {TurmaId}", id);
                    var syncResult = await _syncService.SyncTurmasAsync();
                    if (!syncResult.Success)
                    {
                        _logger.LogWarning("Sincronização falhou para busca da turma {TurmaId}: {Errors}",
                            id, string.Join(", ", syncResult.Errors));
                    }
                }

                // Buscar turma com dados relacionados
                var turma = await _context.Turmas
                    .Include(t => t.Curso)
                    .Where(t => t.Id == id && t.DeletedAt == null)
                    .Select(t => new TurmaDetalhadaDto
                    {
                        IdTurma = t.Id,
                        IdCettpro = t.IdCettpro,
                        Nome = t.Nome,
                        DataInicio = t.DataInicio,
                        DataTermino = t.DataTermino,
                        Status = t.Status,
                        StatusDescricao = GetStatusDescricao(t.Status),
                        CursoId = t.CursoId,
                        Curso = new CursoBasicoDto
                        {
                            IdCurso = t.Curso.Id,
                            NomeCurso = t.Curso.NomeCurso,
                            CargaHoraria = t.Curso.CargaHoraria,
                            IdPortalFat = t.Curso.IdPortalFat
                        },
                        IdPortalFat = t.IdPortalFat,
                        DisciplinaIdPortalFat = t.DisciplinaIdPortalFat,
                        DisciplinaNomePortalFat = t.DisciplinaNomePortalFat,
                        TotalMatriculas = _context.Matriculas.Count(m => m.TurmaId == t.Id && m.DeletedAt == null),
                        TotalAulasGeradas = _context.AulasGeradas.Count(a => a.TurmaId == t.Id && a.DeletedAt == null),
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (turma == null)
                {
                    _logger.LogWarning("Turma não encontrada: {TurmaId}", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Turma não encontrada",
                        Detail = $"Não foi encontrada uma turma com o ID: {id}",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                _logger.LogInformation("Turma consultada com sucesso: {TurmaId} - {NomeTurma}",
                    turma.IdTurma, turma.Nome);

                return Ok(turma);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar turma {TurmaId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar a turma. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Lista os alunos matriculados em uma turma específica
        /// </summary>
        /// <param name="id">ID da turma</param>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="perPage">Itens por página (padrão: 10, máximo: 100)</param>
        /// <param name="search">Termo para busca no nome do aluno</param>
        /// <param name="statusMatricula">Filtro por status da matrícula</param>
        /// <returns>Lista paginada de alunos da turma</returns>
        [HttpGet("{id:guid}/alunos")]
        [ProducesResponseType(typeof(PaginatedResponseDto<AlunoTurmaDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedResponseDto<AlunoTurmaDto>>> GetAlunosDaTurma(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string? search = null,
            [FromQuery] int? statusMatricula = null)
        {
            try
            {
                // Validações de entrada
                if (page < 1)
                {
                    ModelState.AddModelError(nameof(page), "O número da página deve ser maior que zero");
                    return ValidationProblem();
                }

                if (perPage < 1 || perPage > 100)
                {
                    ModelState.AddModelError(nameof(perPage), "Itens por página deve estar entre 1 e 100");
                    return ValidationProblem();
                }

                // Verificar se a turma existe
                var turmaExiste = await _context.Turmas
                    .AnyAsync(t => t.Id == id && t.DeletedAt == null);

                if (!turmaExiste)
                {
                    return NotFound(new ProblemDetails
                    {
                        Title = "Turma não encontrada",
                        Detail = $"Não foi encontrada uma turma com o ID: {id}",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                // Query base para alunos da turma
                var query = _context.Matriculas
                    .Include(m => m.Aluno)
                    .Where(m => m.TurmaId == id && m.DeletedAt == null)
                    .AsQueryable();

                // Aplicar filtro por status da matrícula
                if (statusMatricula.HasValue)
                {
                    query = query.Where(m => m.Status == statusMatricula.Value);
                }

                // Aplicar busca se fornecida
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    query = query.Where(m =>
                        m.Aluno.Nome.ToLower().Contains(searchTerm) ||
                        (m.Aluno.NomeSocial != null && m.Aluno.NomeSocial.ToLower().Contains(searchTerm)) ||
                        m.Aluno.Cpf.Contains(searchTerm) ||
                        (m.Aluno.EmailInstitucional != null && m.Aluno.EmailInstitucional.ToLower().Contains(searchTerm)));
                }

                // Contar total de itens
                var totalItems = await query.CountAsync();

                // Aplicar paginação e projeção
                var alunos = await query
                    .OrderBy(m => m.Aluno.Nome)
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .Select(m => new AlunoTurmaDto
                    {
                        IdMatricula = m.Id,
                        IdAluno = m.Aluno.Id,
                        IdCettpro = m.Aluno.IdCettpro,
                        Nome = m.Aluno.Nome,
                        NomeSocial = m.Aluno.NomeSocial,
                        Cpf = m.Aluno.Cpf,
                        Email = m.Aluno.Email,
                        EmailInstitucional = m.Aluno.EmailInstitucional,
                        StatusMatricula = m.Status,
                        StatusMatriculaDescricao = GetStatusMatriculaDescricao(m.Status),
                        DataMatricula = m.DataMatricula,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .ToListAsync();

                // Obter timestamp da última sincronização
                var lastSync = await _context.SyncLogs
                    .Where(s => s.TipoEntidade == "Aluno" && s.Sucesso)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                // Construir resposta paginada
                var response = new PaginatedResponseDto<AlunoTurmaDto>
                {
                    Data = alunos,
                    Meta = new PaginationMetaDto
                    {
                        Pagination = new PaginationDto
                        {
                            TotalItems = totalItems,
                            PerPage = perPage,
                            CurrentPage = page,
                            TotalPages = (int)Math.Ceiling((double)totalItems / perPage)
                        },
                        LastSyncTimestamp = lastSync
                    }
                };

                _logger.LogInformation(
                    "Consulta de alunos da turma {TurmaId} realizada: {TotalItems} alunos, página {Page}/{TotalPages}",
                    id, totalItems, page, response.Meta.Pagination.TotalPages);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar alunos da turma {TurmaId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar os alunos da turma. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Obtém estatísticas básicas das turmas
        /// </summary>
        /// <param name="idCurso">Filtro opcional por curso</param>
        /// <returns>Estatísticas das turmas</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(TurmaStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TurmaStatsDto>> GetTurmaStats([FromQuery] Guid? idCurso = null)
        {
            try
            {
                var query = _context.Turmas.Where(t => t.DeletedAt == null);

                if (idCurso.HasValue)
                {
                    query = query.Where(t => t.CursoId == idCurso.Value);
                }

                var stats = new TurmaStatsDto
                {
                    TotalTurmas = await query.CountAsync(),
                    TurmasAbertas = await query.CountAsync(t => t.Status == 731890001), // Aberta para Inscrições
                    TurmasEmExecucao = await query.CountAsync(t => t.Status == 731890004), // Em Execução
                    TurmasFinalizadas = await query.CountAsync(t => t.Status == 2), // Finalizada
                    TurmasCanceladas = await query.CountAsync(t => t.Status == 731890005), // Cancelada
                    TurmasComCodigoFat = await query.CountAsync(t => t.IdPortalFat != null),
                    TotalMatriculas = await _context.Matriculas
                        .Where(m => m.DeletedAt == null && (idCurso == null || m.Turma.CursoId == idCurso))
                        .CountAsync(),
                    TotalAulasGeradas = await _context.AulasGeradas
                        .Where(a => a.DeletedAt == null && (idCurso == null || a.Turma.CursoId == idCurso))
                        .CountAsync(),
                    UltimaAtualizacao = await query
                        .MaxAsync(t => (DateTime?)t.UpdatedAt) ?? DateTime.MinValue
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas de turmas");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao obter as estatísticas. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Sincroniza turmas ativas dos últimos N dias
        /// </summary>
        /// <param name="request">Parâmetros da sincronização</param>
        /// <returns>Resultado da sincronização</returns>
        [HttpPost("sync-ativas")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncTurmasAtivas(
            [FromBody] SyncTurmasAtivasRequest? request = null)
        {
            try
            {
                request ??= new SyncTurmasAtivasRequest();

                var operationName = $"Turmas Ativas (Últimos {request.Dias} dias)";
                _logger.LogInformation("Iniciando sincronização manual de '{OperationName}'...", operationName);

                var syncResult = await _syncService.SyncTurmasAtivasAsync();

                var response = new SyncResultResponseDto
                {
                    Success = syncResult.Success,
                    TotalProcessed = syncResult.TotalProcessed,
                    Inserted = syncResult.Inserted,
                    Updated = syncResult.Updated,
                    Deleted = syncResult.Deleted,
                    Errors = syncResult.Errors,
                    StartTime = syncResult.StartTime,
                    EndTime = syncResult.EndTime,
                    Duration = syncResult.Duration
                };

                if (syncResult.Success)
                {
                    _logger.LogInformation(
                        "Sincronização de '{OperationName}' concluída com sucesso: {Inserted} inseridas, {Updated} atualizadas",
                        operationName, syncResult.Inserted, syncResult.Updated);
                }
                else
                {
                    _logger.LogWarning(
                        "Sincronização de '{OperationName}' concluída com falhas: {Errors}",
                        operationName, string.Join("; ", syncResult.Errors));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante a sincronização de turmas ativas.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro Interno no Servidor",
                    Detail = "Ocorreu um erro inesperado ao executar a sincronização de turmas ativas. Consulte os logs para mais detalhes.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }
        /// <summary>
        /// Retorna detalhes de uma turma específica incluindo matrículas
        /// </summary>
        /// <param name="id">ID da turma</param>
        /// <param name="incluirMatriculas">Se deve incluir as matrículas na resposta</param>
        /// <returns>Detalhes da turma</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(TurmaDetalhadaResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TurmaDetalhadaResponseDto>> GetTurma(
            Guid id,
            [FromQuery] bool incluirMatriculas = false)
        {
            var turma = await _context.Turmas
                .Include(t => t.Curso)
                .Include(t => t.UnidadeEnsino)
                .Where(t => t.Id == id)
                .FirstOrDefaultAsync();

            if (turma == null)
            {
                return NotFound();
            }

            var response = new TurmaDetalhadaResponseDto
            {
                Id = turma.Id,
                IdCettpro = turma.IdCettpro,
                Nome = turma.Nome,
                DataInicio = turma.DataInicio,
                DataTermino = turma.DataTermino,
                Status = turma.Status,
                Ativo = turma.Ativo,
                CodigoPortalFat = turma.CodigoPortalFat,
                DisciplinaCodigoPortalFat = turma.DisciplinaCodigoPortalFat,
                DisciplinaNomePortalFat = turma.DisciplinaNomePortalFat,
                Curso = turma.Curso != null ? new CursoResponseDto
                {
                    Id = turma.Curso.Id,
                    IdCettpro = turma.Curso.IdCettpro,
                    NomeCurso = turma.Curso.NomeCurso,
                    CargaHoraria = turma.Curso.CargaHoraria,
                    Descricao = turma.Curso.Descricao,
                    Ativo = turma.Curso.Ativo
                } : null,
                UnidadeEnsino = turma.UnidadeEnsino != null ? new UnidadeEnsinoResponseDto
                {
                    Id = turma.UnidadeEnsino.Id,
                    IdCettpro = turma.UnidadeEnsino.IdCettpro,
                    Nome = turma.UnidadeEnsino.Nome,
                    NomeFantasia = turma.UnidadeEnsino.NomeFantasia,
                    Cnpj = turma.UnidadeEnsino.Cnpj,
                    Ativo = turma.UnidadeEnsino.Ativo
                } : null
            };

            if (incluirMatriculas)
            {
                var matriculas = await _context.Matriculas
                    .Include(m => m.Aluno)
                    .Where(m => m.TurmaId == turma.Id)
                    .ToListAsync();

                response.Matriculas = matriculas.Select(m => new MatriculaResponseDto
                {
                    Id = m.Id,
                    IdCettpro = m.IdCettpro,
                    Status = m.Status,
                    DataMatricula = m.DataMatricula,
                    Aluno = new AlunoResponseDto
                    {
                        Id = m.Aluno.Id,
                        IdCettpro = m.Aluno.IdCettpro,
                        Nome = m.Aluno.Nome,
                        NomeSocial = m.Aluno.NomeSocial,
                        Cpf = m.Aluno.Cpf,
                        Rg = m.Aluno.Rg,
                        Email = m.Aluno.Email,
                        EmailInstitucional = m.Aluno.EmailInstitucional,
                        DataNascimento = m.Aluno.DataNascimento,
                        Ativo = m.Aluno.Ativo
                    }
                }).ToList();
            }

            return Ok(response);
        }


        /// <summary>
        /// Força sincronização das turmas
        /// </summary>
        /// <returns>Resultado da sincronização</returns>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncTurmas()
        {
            try
            {
                _logger.LogInformation("Iniciando sincronização manual de turmas");

                var syncResult = await _syncService.SyncTurmasAsync();

                var response = new SyncResultResponseDto
                {
                    Success = syncResult.Success,
                    TotalProcessed = syncResult.TotalProcessed,
                    Inserted = syncResult.Inserted,
                    Updated = syncResult.Updated,
                    Deleted = syncResult.Deleted,
                    Errors = syncResult.Errors,
                    StartTime = syncResult.StartTime,
                    EndTime = syncResult.EndTime,
                    Duration = syncResult.Duration
                };

                if (syncResult.Success)
                {
                    _logger.LogInformation(
                        "Sincronização de turmas concluída com sucesso: {Inserted} inseridas, {Updated} atualizadas, {Deleted} removidas",
                        syncResult.Inserted, syncResult.Updated, syncResult.Deleted);
                }
                else
                {
                    _logger.LogWarning(
                        "Sincronização de turmas falhou: {Errors}",
                        string.Join(", ", syncResult.Errors));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar sincronização manual de turmas");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao sincronizar as turmas. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Executa a sincronização da entidade Turmas por um período específico.
        /// </summary>
        /// <param name="request">Objeto contendo a data inicial e final para o filtro.</param>
        /// <returns>Resultado detalhado da sincronização de turmas para o período.</returns>
        [HttpPost("turmas/periodo")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncTurmasPorPeriodo([FromBody] SyncTurmasPorPeriodoRequest request)
        {
            try
            {
                var operationName = $"Turmas por Período ({request.DataInicial:d} a {request.DataFinal:d})";
                _logger.LogInformation("Iniciando sincronização manual de '{OperationName}'...", operationName);

                var syncResult = await _syncService.SyncTurmasPorPeriodoAsync(request.DataInicial, request.DataFinal);

                var response = new SyncResultResponseDto
                {
                    Success = syncResult.Success,
                    TotalProcessed = syncResult.TotalProcessed,
                    Inserted = syncResult.Inserted,
                    Updated = syncResult.Updated,
                    Deleted = syncResult.Deleted,
                    Errors = syncResult.Errors,
                    StartTime = syncResult.StartTime,
                    EndTime = syncResult.EndTime,
                    Duration = syncResult.Duration
                };

                if (syncResult.Success)
                {
                    _logger.LogInformation(
                        "Sincronização de '{OperationName}' concluída com sucesso: {Inserted} inseridas, {Updated} atualizadas",
                        operationName, syncResult.Inserted, syncResult.Updated);
                }
                else
                {
                    _logger.LogWarning(
                        "Sincronização de '{OperationName}' concluída com falhas: {Errors}",
                        operationName, string.Join("; ", syncResult.Errors));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante a sincronização manual de turmas por período.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro Interno no Servidor",
                    Detail = "Ocorreu um erro inesperado ao executar a sincronização de turmas por período. Consulte os logs para mais detalhes."
                });
            }
        }

        #region Métodos Auxiliares

        private static string GetStatusDescricao(int status)
        {
            return status switch
            {
                731890001 => "Aberta para Inscrições",
                731890005 => "Cancelada",
                731890002 => "Em Construção",
                731890004 => "Em Execução",
                2 => "Finalizada",
                731890003 => "Pronta para Execução",
                _ => "Status Desconhecido"
            };
        }

        private static string GetStatusMatriculaDescricao(int status)
        {
            return status switch
            {
                1 => "Ativa",
                2 => "Concluída",
                3 => "Cancelada",
                4 => "Trancada",
                5 => "Transferida",
                _ => "Status Desconhecido"
            };
        }

        #endregion
    }
}