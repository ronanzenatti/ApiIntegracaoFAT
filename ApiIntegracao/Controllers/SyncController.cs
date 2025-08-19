using ApiIntegracao.DTOs.Responses;
using ApiIntegracao.Services.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiIntegracao.Controllers
{
    /// <summary>
    /// Controller para acionamento manual das rotinas de sincronização com a API CETTPRO.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly ISyncService _syncService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(ISyncService syncService, ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Executa a sincronização completa de todas as entidades (Cursos, Turmas e Alunos).
        /// </summary>
        /// <returns>Resultado detalhado da sincronização completa.</returns>
        [HttpPost("all")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncAll()
        {
            return await ExecuteSyncOperation(
                "Completa",
                () => _syncService.SyncAllAsync()
            );
        }

        /// <summary>
        /// Executa a sincronização apenas da entidade Cursos.
        /// </summary>
        /// <returns>Resultado detalhado da sincronização de cursos.</returns>
        [HttpPost("cursos")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncCursos()
        {
            return await ExecuteSyncOperation(
                "Cursos",
                () => _syncService.SyncCursosAsync()
            );
        }

        /// <summary>
        /// Executa a sincronização apenas da entidade Turmas.
        /// </summary>
        /// <returns>Resultado detalhado da sincronização de turmas.</returns>
        [HttpPost("turmas")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncTurmas()
        {
            return await ExecuteSyncOperation(
                "Turmas",
                () => _syncService.SyncTurmasAsync()
            );
        }

        /// <summary>
        /// Executa a sincronização apenas da entidade Alunos (e suas matrículas).
        /// </summary>
        /// <returns>Resultado detalhado da sincronização de alunos.</returns>
        [HttpPost("alunos")]
        [ProducesResponseType(typeof(SyncResultResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<SyncResultResponseDto>> SyncAlunos()
        {
            return await ExecuteSyncOperation(
                "Alunos",
                () => _syncService.SyncAlunosAsync()
            );
        }

        /// <summary>
        /// Método auxiliar para executar e padronizar a resposta das operações de sincronização.
        /// </summary>
        private async Task<ActionResult<SyncResultResponseDto>> ExecuteSyncOperation(
            string operationName,
            Func<Task<SyncResult>> syncAction)
        {
            try
            {
                _logger.LogInformation("Iniciando sincronização manual de '{OperationName}'...", operationName);

                var syncResult = await syncAction();

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
                    _logger.LogInformation("Sincronização manual de '{OperationName}' concluída com sucesso.", operationName);
                }
                else
                {
                    _logger.LogWarning("Sincronização manual de '{OperationName}' concluída com falhas: {Errors}",
                        operationName, string.Join("; ", syncResult.Errors));
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante a sincronização manual de '{OperationName}'.", operationName);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Erro Interno no Servidor",
                    Detail = $"Ocorreu um erro inesperado ao executar a sincronização de {operationName}. Consulte os logs para mais detalhes."
                });
            }
        }
    }
}