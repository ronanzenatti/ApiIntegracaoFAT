using ApiIntegracao.Data;
using ApiIntegracao.DTOs;
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
                _logger.LogInformation("Iniciando sincronização completa...");

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

                _logger.LogInformation(
                    "Sincronização completa finalizada: {TotalProcessed} processados, {Inserted} inseridos, {Updated} atualizados, {Deleted} deletados",
                    result.TotalProcessed, result.Inserted, result.Updated, result.Deleted);
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

            // O log individual de cada sync já é feito dentro dos métodos
            return result;
        }

        public async Task<SyncResult> SyncCursosAsync()
        {
            var result = new SyncResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Iniciando sincronização de cursos...");

                // CORREÇÃO: A API retorna uma lista de programas, cada um com uma lista de cursos.
                var programasFromApi = await _cettproClient.GetAsync<List<ProgramaDto>>("api/v1/RetornaCursos");

                // Extrai todos os cursos de todos os programas para uma lista única.
                var cursosFromApi = programasFromApi?.SelectMany(p => p.Cursos).ToList();

                if (cursosFromApi == null || !cursosFromApi.Any())
                {
                    result.Errors.Add("Nenhum curso retornado pela API");
                    result.EndTime = DateTime.UtcNow;
                    await LogSyncResult("Curso", result);
                    return result;
                }

                result.TotalProcessed = cursosFromApi.Count;
                _logger.LogInformation("Processando {Count} cursos da CETTPRO", cursosFromApi.Count);

                var idsFromApi = new HashSet<Guid>(cursosFromApi.Select(c => c.IdCurso));

                foreach (var cursoDto in cursosFromApi)
                {
                    try
                    {
                        idsFromApi.Add(cursoDto.IdCurso);

                        var cursoExistente = await _context.Cursos
                            .FirstOrDefaultAsync(c => c.IdCettpro == cursoDto.IdCurso);

                        if (cursoExistente == null)
                        {
                            var novoCurso = new Curso
                            {
                                IdCettpro = cursoDto.IdCurso,
                                NomeCurso = cursoDto.NomeCurso,
                                CargaHoraria = cursoDto.CargaHoraria, // Mapeamento direto de string?
                                Descricao = cursoDto.Descricao,
                                ModalidadeId = cursoDto.ModalidadeId ?? Guid.Empty,
                                Ativo = cursoDto.Ativo
                            };
                            _context.Cursos.Add(novoCurso);
                            result.Inserted++;
                        }
                        else
                        {
                            bool hasChanges = false;
                            if (cursoExistente.NomeCurso != cursoDto.NomeCurso) { cursoExistente.NomeCurso = cursoDto.NomeCurso; hasChanges = true; }
                            if (cursoExistente.CargaHoraria != cursoDto.CargaHoraria) { cursoExistente.CargaHoraria = cursoDto.CargaHoraria; hasChanges = true; }
                            if (cursoExistente.Descricao != cursoDto.Descricao) { cursoExistente.Descricao = cursoDto.Descricao; hasChanges = true; }
                            if (cursoExistente.ModalidadeId != (cursoDto.ModalidadeId ?? Guid.Empty)) { cursoExistente.ModalidadeId = cursoDto.ModalidadeId ?? Guid.Empty; hasChanges = true; }
                            if (cursoExistente.Ativo != cursoDto.Ativo) { cursoExistente.Ativo = cursoDto.Ativo; hasChanges = true; }
                            if (cursoExistente.DeletedAt != null) { cursoExistente.DeletedAt = null; hasChanges = true; }

                            if (hasChanges)
                            {
                                result.Updated++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar curso {Id}", cursoDto.IdCurso);
                        result.Errors.Add($"Erro no curso {cursoDto.NomeCurso}: {ex.Message}");
                    }
                }

                var cursosParaDesativar = await _context.Cursos
                    .Where(c => !idsFromApi.Contains(c.IdCettpro) && c.DeletedAt == null)
                    .ToListAsync();

                foreach (var curso in cursosParaDesativar)
                {
                    curso.DeletedAt = DateTime.UtcNow;
                    result.Deleted++;
                    _logger.LogWarning("Curso marcado como deletado: {Nome}", curso.NomeCurso);
                }

                await _context.SaveChangesAsync();
                result.Success = !result.Errors.Any();
                _logger.LogInformation(
                   "Sincronização de cursos concluída: {Inserted} inseridos, {Updated} atualizados, {Deleted} deletados",
                   result.Inserted, result.Updated, result.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização de cursos");
                result.Success = false;
                result.Errors.Add($"Erro: {ex.Message}");
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

                // CORREÇÃO: O endpoint de Turmas espera um POST com corpo JSON, mesmo que vazio.
                var turmasFromApi = await _cettproClient.GetAsync<List<TurmaDto>>("api/v1/Turma");

                if (turmasFromApi == null || !turmasFromApi.Any())
                {
                    result.Errors.Add("Nenhuma turma retornada pela API");
                    result.EndTime = DateTime.UtcNow;
                    await LogSyncResult("Turma", result);
                    return result;
                }

                result.TotalProcessed = turmasFromApi.Count;
                _logger.LogInformation("Processando {Count} turmas da CETTPRO", turmasFromApi.Count);

                var idsFromApi = new HashSet<Guid>();

                foreach (var turmaDto in turmasFromApi)
                {
                    try
                    {
                        idsFromApi.Add(turmaDto.IdTurma);

                        var turmaExistente = await _context.Turmas
                            .FirstOrDefaultAsync(t => t.IdCettpro == turmaDto.IdTurma);

                        Curso? curso = null;
                        if (turmaDto.CursoId.HasValue)
                        {
                            curso = await _context.Cursos
                                .FirstOrDefaultAsync(c => c.IdCettpro == turmaDto.CursoId.Value);
                        }

                        if (curso == null)
                        {
                            _logger.LogWarning("Curso com ID {CursoId} não encontrado para a turma {TurmaNome}. A turma será ignorada.", turmaDto.CursoId, turmaDto.Nome);
                            continue;
                        }

                        if (turmaExistente == null)
                        {
                            var novaTurma = new Turma
                            {
                                IdCettpro = turmaDto.IdTurma,
                                Nome = turmaDto.Nome,
                                DataInicio = turmaDto.DataInicio,
                                DataTermino = turmaDto.DataTermino,
                                Status = turmaDto.Status,
                                CursoId = curso.Id,
                                Curso = curso
                            };
                            _context.Turmas.Add(novaTurma);
                            result.Inserted++;
                        }
                        else
                        {
                            bool hasChanges = false;
                            if (turmaExistente.Nome != turmaDto.Nome) { turmaExistente.Nome = turmaDto.Nome; hasChanges = true; }
                            if (turmaExistente.DataInicio != turmaDto.DataInicio) { turmaExistente.DataInicio = turmaDto.DataInicio; hasChanges = true; }
                            if (turmaExistente.DataTermino != turmaDto.DataTermino) { turmaExistente.DataTermino = turmaDto.DataTermino; hasChanges = true; }
                            if (turmaExistente.Status != turmaDto.Status) { turmaExistente.Status = turmaDto.Status; hasChanges = true; }
                            if (turmaExistente.CursoId != curso.Id) { turmaExistente.CursoId = curso.Id; hasChanges = true; }
                            if (turmaExistente.DeletedAt.HasValue) { turmaExistente.DeletedAt = null; hasChanges = true; }

                            if (hasChanges) result.Updated++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar turma {Id}", turmaDto.IdTurma);
                        result.Errors.Add($"Erro na turma {turmaDto.Nome}: {ex.Message}");
                    }
                }

                var turmasParaDesativar = await _context.Turmas
                    .Where(t => !idsFromApi.Contains(t.IdCettpro) && t.DeletedAt == null)
                    .ToListAsync();

                foreach (var turma in turmasParaDesativar)
                {
                    turma.DeletedAt = DateTime.UtcNow;
                    result.Deleted++;
                }

                await _context.SaveChangesAsync();
                result.Success = !result.Errors.Any();
                _logger.LogInformation(
                    "Sincronização de turmas concluída: {Inserted} inseridas, {Updated} atualizadas, {Deleted} deletadas",
                    result.Inserted, result.Updated, result.Deleted);
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
            var result = new SyncResult { StartTime = DateTime.UtcNow };

            // CORREÇÃO: A API não tem um endpoint para buscar todas as matrículas/alunos.
            // A sincronização de alunos deve ser feita por turma, em um processo separado.
            _logger.LogInformation("Sincronização de Alunos: Etapa pulada. Não há endpoint global na API CETTPRO para buscar todos os alunos. A sincronização de alunos deve ocorrer em um contexto de turma específica.");

            result.Success = true;
            result.TotalProcessed = 0;
            result.EndTime = DateTime.UtcNow;

            await LogSyncResult("Aluno", result);
            return result;
        }

        private async Task LogSyncResult(string entityType, SyncResult result)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao salvar log de sincronização para {EntityType}", entityType);
            }
        }
    }
}