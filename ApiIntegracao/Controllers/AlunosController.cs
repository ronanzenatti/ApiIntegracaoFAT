using ApiIntegracao.Data;
using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.Responses;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApiIntegracao.Controllers
{
    /// <summary>
    /// Controller para gerenciamento de alunos
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    public class AlunosController : ControllerBase
    {
        private readonly ApiIntegracaoDbContext _context;
        private readonly ISyncService _syncService;
        private readonly ILogger<AlunosController> _logger;

        public AlunosController(
            ApiIntegracaoDbContext context,
            ISyncService syncService,
            ILogger<AlunosController> logger)
        {
            _context = context;
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos os alunos com paginação e busca
        /// </summary>
        /// <param name="page">Número da página (padrão: 1)</param>
        /// <param name="perPage">Itens por página (padrão: 10, máximo: 100)</param>
        /// <param name="search">Termo para busca no nome, CPF ou e-mail do aluno</param>
        /// <param name="forceSync">Força sincronização antes de retornar dados</param>
        /// <returns>Lista paginada de alunos</returns>
        [HttpGet]
        [ProducesResponseType(typeof(PaginatedResponseDto<AlunoResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PaginatedResponseDto<AlunoResponseDto>>> GetAlunos(
            [FromQuery] int page = 1,
            [FromQuery] int perPage = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool forceSync = false)
        {
            try
            {
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

                if (forceSync)
                {
                    _logger.LogInformation("Executando sincronização forçada de alunos antes da consulta.");
                    var syncResult = await _syncService.SyncAlunosAsync();
                    if (!syncResult.Success)
                    {
                        _logger.LogWarning("Sincronização de alunos falhou, mas continuando com dados locais: {Errors}", string.Join(", ", syncResult.Errors));
                    }
                }

                var query = _context.Alunos.AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchTerm = search.Trim().ToLower();
                    query = query.Where(a =>
                        a.Nome.ToLower().Contains(searchTerm) ||
                        (a.NomeSocial != null && a.NomeSocial.ToLower().Contains(searchTerm)) ||
                        a.Cpf.Contains(searchTerm) ||
                        (a.Email != null && a.Email.ToLower().Contains(searchTerm)) ||
                        (a.EmailInstitucional != null && a.EmailInstitucional.ToLower().Contains(searchTerm))
                    );
                }

                var totalItems = await query.CountAsync();
                var alunos = await query
                    .OrderBy(a => a.Nome)
                    .Skip((page - 1) * perPage)
                    .Take(perPage)
                    .Select(a => new AlunoResponseDto
                    {
                        Id = a.Id,
                        IdCettpro = a.IdCettpro,
                        Nome = a.Nome,
                        NomeSocial = a.NomeSocial,
                        Cpf = a.Cpf,
                        Rg = a.Rg,
                        DataNascimento = a.DataNascimento,
                        Email = a.Email,
                        EmailInstitucional = a.EmailInstitucional,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt
                    })
                    .ToListAsync();

                var response = new PaginatedResponseDto<AlunoResponseDto>
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
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar alunos.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar os alunos. Tente novamente mais tarde."
                });
            }
        }

        /// <summary>
        /// Obtém um aluno específico por ID
        /// </summary>
        /// <param name="id">ID do aluno</param>
        /// <returns>Dados do aluno</returns>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(AlunoResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AlunoResponseDto>> GetAluno(Guid id)
        {
            try
            {
                var aluno = await _context.Alunos
                    .Where(a => a.Id == id)
                    .Select(a => new AlunoResponseDto
                    {
                        Id = a.Id,
                        IdCettpro = a.IdCettpro,
                        Nome = a.Nome,
                        NomeSocial = a.NomeSocial,
                        Cpf = a.Cpf,
                        Rg = a.Rg,
                        DataNascimento = a.DataNascimento,
                        Email = a.Email,
                        EmailInstitucional = a.EmailInstitucional,
                        CreatedAt = a.CreatedAt,
                        UpdatedAt = a.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (aluno == null)
                {
                    _logger.LogWarning("Aluno com ID {AlunoId} não encontrado.", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Aluno não encontrado",
                        Detail = $"Não foi possível encontrar um aluno com o ID: {id}"
                    });
                }

                return Ok(aluno);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar aluno com ID {AlunoId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro interno do servidor",
                    Detail = "Ocorreu um erro ao buscar o aluno. Tente novamente mais tarde."
                });
            }
        }
    }
}