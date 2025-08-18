using ApiIntegracao.Data;
using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.Responses;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiIntegracao.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de cursos
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class CursosController : ControllerBase
    {
        private readonly ApiIntegracaoDbContext _context;
        private readonly ISyncService _syncService;
        private readonly ILogger<CursosController> _logger;

        public CursosController(
            ApiIntegracaoDbContext context,
            ISyncService syncService,
            ILogger<CursosController> logger)
        {
            _context = context;
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos os cursos com paginação e busca
        /// </summary>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="perPage">Itens por página (padrão: 10, máximo: 100)</param>
        /// <param name="search">Termo para busca no nome do curso</param>
        /// <param name="forceSync">Força sincronização antes de retornar dados</param>
        /// <returns>Lista paginada de cursos</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponseDto<CursoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedResponseDto<CursoResponseDto>>> GetCursos(
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string? search = null,
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
                    _logger.LogInformation("Executando sincronização forçada de cursos antes da consulta");
                    var syncResult = await _syncService.SyncCursosAsync();
                    if (!syncResult.Success)
                    {
                        _logger.LogWarning("Sincronização falhou, mas continuando com dados locais: {Errors}",
                            string.Join(", ", syncResult.Errors));
                    }
                }

                // Query base
                var query = _context.Cursos
                    .Where(c => c.DeletedAt == null) // Filtrar soft deleted
                    .AsQueryable();

                // Aplicar busca se fornecida
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    query = query.Where(c =>
                        c.NomeCurso.ToLower().Contains(searchTerm) ||
                        (c.Descricao != null && c.Descricao.ToLower().Contains(searchTerm)) ||
                        (c.IdPortalFat != null && c.IdPortalFat.ToString().ToLower().Contains(searchTerm)));
                }

                // Contar total de itens
                var totalItems = await query.CountAsync();

                // Aplicar paginação
                var cursos = await query
                    .OrderBy(c => c.NomeCurso)
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .Select(c => new CursoResponseDto
                    {
                        IdCurso = c.Id,
                        IdCettpro = c.IdCettpro,
                        NomeCurso = c.NomeCurso,
                        CargaHoraria = c.CargaHoraria,
                        Descricao = c.Descricao,
                        ModalidadeId = c.ModalidadeId,
                        Ativo = c.Ativo,
                        IdPortalFat = c.IdPortalFat,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                // Obter timestamp da última sincronização
                var lastSync = await _context.SyncLogs
                    .Where(s => s.TipoEntidade == "Curso" && s.Sucesso)
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => s.CreatedAt)
                    .FirstOrDefaultAsync();

                // Construir resposta paginada
                var response = new PaginatedResponseDto<CursoResponseDto>
                {
                    Data = cursos,
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
                    "Consulta de cursos realizada: {TotalItems} itens, página {Page}/{TotalPages}, busca: '{Search}'",
                    totalItems, page, response.Meta.Pagination.TotalPages, search ?? "N/A");

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar cursos");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar os cursos. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Obtém um curso específico por ID
        /// </summary>
        /// <param name="id">ID do curso</param>
        /// <param name="forceSync">Força sincronização antes de retornar dados</param>
        /// <returns>Dados do curso</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(CursoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CursoResponseDto>> GetCurso(
            Guid id,
            [FromQuery] bool forceSync = false)
        {
            try
            {
                // Sincronização incremental se solicitada
                if (forceSync)
                {
                    _logger.LogInformation("Executando sincronização forçada antes de buscar curso {CursoId}", id);
                    var syncResult = await _syncService.SyncCursosAsync();
                    if (!syncResult.Success)
                    {
                        _logger.LogWarning("Sincronização falhou para busca do curso {CursoId}: {Errors}",
                            id, string.Join(", ", syncResult.Errors));
                    }
                }

                // Buscar curso
                var curso = await _context.Cursos
                    .Where(c => c.Id == id && c.DeletedAt == null)
                    .Select(c => new CursoResponseDto
                    {
                        IdCurso = c.Id,
                        IdCettpro = c.IdCettpro,
                        NomeCurso = c.NomeCurso,
                        CargaHoraria = c.CargaHoraria,
                        Descricao = c.Descricao,
                        ModalidadeId = c.ModalidadeId,
                        Ativo = c.Ativo,
                        IdPortalFat = c.IdPortalFat,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (curso == null)
                {
                    _logger.LogWarning("Curso não encontrado: {CursoId}", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Curso não encontrado",
                        Detail = $"Não foi encontrado um curso com o ID: {id}",
                        Status = StatusCodes.Status404NotFound
                    });
                }

                _logger.LogInformation("Curso consultado com sucesso: {CursoId} - {NomeCurso}",
                    curso.IdCurso, curso.NomeCurso);

                return Ok(curso);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar curso {CursoId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar o curso. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Obtém estatísticas básicas dos cursos
        /// </summary>
        /// <returns>Estatísticas dos cursos</returns>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(CursoStatsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CursoStatsDto>> GetCursoStats()
        {
            try
            {
                var stats = new CursoStatsDto
                {
                    TotalCursos = await _context.Cursos.CountAsync(c => c.DeletedAt == null),
                    CursosAtivos = await _context.Cursos.CountAsync(c => c.DeletedAt == null && c.Ativo),
                    CursosInativos = await _context.Cursos.CountAsync(c => c.DeletedAt == null && !c.Ativo),
                    CursosComCodigoFat = await _context.Cursos.CountAsync(c => c.DeletedAt == null && !string.IsNullOrEmpty(c.IdPortalFat.ToString())),
                    TotalTurmas = await _context.Turmas.CountAsync(t => t.DeletedAt == null),
                    UltimaAtualizacao = await _context.Cursos
                        .Where(c => c.DeletedAt == null)
                        .MaxAsync(c => (DateTime?)c.UpdatedAt) ?? DateTime.MinValue
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao obter estatísticas de cursos");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao obter as estatísticas. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }

        /// <summary>
        /// Força sincronização dos cursos
        /// </summary>
        /// <returns>Resultado da sincronização</returns>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncCursos()
        {
            try
            {
                _logger.LogInformation("Iniciando sincronização manual de cursos");

                var syncResult = await _syncService.SyncCursosAsync();

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
                        "Sincronização de cursos concluída com sucesso: {Inserted} inseridos, {Updated} atualizados, {Deleted} removidos",
                        syncResult.Inserted, syncResult.Updated, syncResult.Deleted);
                }
                else
                {
                    _logger.LogWarning(
                        "Sincronização de cursos falhou: {Errors}",
                        string.Join(", ", syncResult.Errors));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao executar sincronização manual de cursos");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao sincronizar os cursos. Tente novamente mais tarde.",
                    Status = StatusCodes.Status500InternalServerError
                });
            }
        }
    }
}