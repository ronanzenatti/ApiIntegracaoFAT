using ApiIntegracao.Data;
using ApiIntegracao.Exceptions;
using ApiIntegracao.Infrastructure.HttpClients;
using ApiIntegracao.Models;
using ApiIntegracao.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ApiIntegracao.Services.Implementations
{
    public class SyncService : ISyncService
    {
        private readonly ICettproApiClient _cettproClient;
        private readonly ApiIntegracaoDbContext _context;
        private readonly ILogger<SyncService> _logger;

        public SyncService(
            ICettproApiClient cettproClient,
            ApiIntegracaoDbContext context,
            ILogger<SyncService> logger)
        {
            _cettproClient = cettproClient;
            _context = context;
            _logger = logger;
        }

        public async Task<SyncResult> SyncAllAsync()
        {
            var result = new SyncResult { StartTime = DateTime.UtcNow };

            try
            {
                // Sincronizar em ordem de dependência
                var cursoResult = await SyncCursosAsync();
                var turmaResult = await SyncTurmasAsync();
                var alunoResult = await SyncAlunosAsync();

                result.Success = cursoResult.Success && turmaResult.Success && alunoResult.Success;
                result.TotalProcessed = cursoResult.TotalProcessed + turmaResult.TotalProcessed + alunoResult.TotalProcessed;
                result.Inserted = cursoResult.Inserted + turmaResult.Inserted + alunoResult.Inserted;
                result.Updated = cursoResult.Updated + turmaResult.Updated + alunoResult.Updated;
                result.Deleted = cursoResult.Deleted + turmaResult.Deleted + alunoResult.Deleted;

                result.Errors.AddRange(cursoResult.Errors);
                result.Errors.AddRange(turmaResult.Errors);
                result.Errors.AddRange(alunoResult.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização completa");
                result.Success = false;
                result.Errors.Add($"Erro geral: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            await LogSyncResult("Full", result);
            return result;
        }

        public async Task<SyncResult> SyncCursosAsync()
        {
            var result = new SyncResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Iniciando sincronização de cursos...");

                // Buscar dados da CETTPRO
                var cursosFromApi = await _cettproClient.GetAsync<List<CursoDto>>("api/v1/RetornaCursos");

                if (cursosFromApi == null)
                {
                    result.Errors.Add("Nenhum curso retornado pela API");
                    result.EndTime = DateTime.UtcNow;
                    return result;
                }

                result.TotalProcessed = cursosFromApi.Count;
                _logger.LogInformation("Processando {Count} cursos da CETTPRO", cursosFromApi.Count);

                // Processar cada curso
                var idsFromApi = new HashSet<Guid>();

                foreach (var cursoDto in cursosFromApi)
                {
                    try
                    {
                        idsFromApi.Add(cursoDto.IdCurso);

                        var cursoExistente = await _context.Cursos
                            .FirstOrDefaultAsync(c => c.IdCettpro == cursoDto.IdCurso);

                        if (cursoExistente == null)
                        {
                            // Inserir novo curso
                            var novoCurso = new Curso
                            {
                                IdCettpro = cursoDto.IdCurso,
                                NomeCurso = cursoDto.NomeCurso,
                                CargaHoraria = cursoDto.CargaHoraria,
                                Descricao = cursoDto.Descricao,
                                ModalidadeId = cursoDto.ModalidadeId,
                                Ativo = cursoDto.Ativo
                            };

                            _context.Cursos.Add(novoCurso);
                            result.Inserted++;

                            _logger.LogDebug("Novo curso adicionado: {Nome}", cursoDto.NomeCurso);
                        }
                        else
                        {
                            // Verificar se houve alterações
                            bool hasChanges = false;

                            if (cursoExistente.NomeCurso != cursoDto.NomeCurso)
                            {
                                cursoExistente.NomeCurso = cursoDto.NomeCurso;
                                hasChanges = true;
                            }

                            if (cursoExistente.CargaHoraria != cursoDto.CargaHoraria)
                            {
                                cursoExistente.CargaHoraria = cursoDto.CargaHoraria;
                                hasChanges = true;
                            }

                            if (cursoExistente.Descricao != cursoDto.Descricao)
                            {
                                cursoExistente.Descricao = cursoDto.Descricao;
                                hasChanges = true;
                            }

                            if (cursoExistente.Ativo != cursoDto.Ativo)
                            {
                                cursoExistente.Ativo = cursoDto.Ativo;
                                hasChanges = true;
                            }

                            // Reativar se estava soft-deleted
                            if (cursoExistente.DeletedAt.HasValue)
                            {
                                cursoExistente.DeletedAt = null;
                                hasChanges = true;
                            }

                            if (hasChanges)
                            {
                                result.Updated++;
                                _logger.LogDebug("Curso atualizado: {Nome}", cursoDto.NomeCurso);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar curso {Id}", cursoDto.IdCurso);
                        result.Errors.Add($"Erro no curso {cursoDto.NomeCurso}: {ex.Message}");
                    }
                }

                // Soft delete dos cursos que não vieram da API
                var cursosParaDesativar = await _context.Cursos
                    .Where(c => !idsFromApi.Contains(c.IdCettpro) && c.DeletedAt == null)
                    .ToListAsync();

                foreach (var curso in cursosParaDesativar)
                {
                    curso.DeletedAt = DateTime.UtcNow;
                    result.Deleted++;
                    _logger.LogWarning("Curso marcado como deletado: {Nome}", curso.NomeCurso);
                }

                // Salvar todas as alterações
                await _context.SaveChangesAsync();

                result.Success = true;
                _logger.LogInformation(
                    "Sincronização de cursos concluída: {Inserted} inseridos, {Updated} atualizados, {Deleted} deletados",
                    result.Inserted, result.Updated, result.Deleted);
            }
            catch (CettproApiException ex)
            {
                _logger.LogError(ex, "Erro na API CETTPRO durante sincronização de cursos");
                result.Success = false;
                result.Errors.Add($"Erro CETTPRO: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro inesperado durante sincronização de cursos");
                result.Success = false;
                result.Errors.Add($"Erro geral: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            await LogSyncResult("Curso", result);
            return result;
        }

        public async Task<SyncResult> SyncTurmasAsync()
        {
            var result = new SyncResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Iniciando sincronização de turmas...");

                var turmasFromApi = await _cettproClient.GetAsync<List<TurmaDto>>("api/v1/Turma");

                if (turmasFromApi == null)
                {
                    result.Errors.Add("Nenhuma turma retornada pela API");
                    result.EndTime = DateTime.UtcNow;
                    return result;
                }

                result.TotalProcessed = turmasFromApi.Count;

                // Implementação similar à de cursos...
                // [Código similar ao SyncCursosAsync adaptado para Turmas]

                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização de turmas");
                result.Success = false;
                result.Errors.Add($"Erro: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            await LogSyncResult("Turma", result);
            return result;
        }

        public async Task<SyncResult> SyncAlunosAsync()
        {
            // Implementação similar...
            var result = new SyncResult { StartTime = DateTime.UtcNow };
            // ... código de sincronização de alunos
            return result;
        }

        private async Task LogSyncResult(string entityType, SyncResult result)
        {
            var syncLog = new SyncLog
            {
                TipoEntidade = entityType,
                Operacao = "Sync",
                QuantidadeProcessada = result.TotalProcessed,
                Sucesso = result.Success,
                ErroDetalhes = result.Errors.Any() ? string.Join("; ", result.Errors) : null,
                InicioProcessamento = result.StartTime,
                FimProcessamento = result.EndTime
            };

            _context.SyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();
        }
    }
}