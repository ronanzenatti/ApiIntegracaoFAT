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

                if (cursosFromApi == null || !cursosFromApi.Any())
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
                                CargaHoraria = cursoDto.CargaHoraria.ToString(), // CORREÇÃO: converter int para string
                                Descricao = cursoDto.Descricao,
                                ModalidadeId = cursoDto.ModalidadeId ?? Guid.Empty, // CORREÇÃO: tratar Guid nullable
                                Ativo = cursoDto.Ativo
                            };

                            _context.Cursos.Add(novoCurso);
                            result.Inserted++;
                            _logger.LogDebug("Novo curso adicionado: {Nome}", cursoDto.NomeCurso);
                        }
                        else
                        {
                            // Verificar se houve mudanças
                            bool hasChanges = false;

                            if (cursoExistente.NomeCurso != cursoDto.NomeCurso)
                            {
                                cursoExistente.NomeCurso = cursoDto.NomeCurso;
                                hasChanges = true;
                            }

                            if (cursoExistente.CargaHoraria != cursoDto.CargaHoraria.ToString()) // CORREÇÃO
                            {
                                cursoExistente.CargaHoraria = cursoDto.CargaHoraria.ToString(); // CORREÇÃO
                                hasChanges = true;
                            }

                            if (cursoExistente.Descricao != cursoDto.Descricao)
                            {
                                cursoExistente.Descricao = cursoDto.Descricao;
                                hasChanges = true;
                            }

                            if (cursoExistente.ModalidadeId != (cursoDto.ModalidadeId ?? Guid.Empty))
                            {
                                cursoExistente.ModalidadeId = cursoDto.ModalidadeId ?? Guid.Empty;
                                hasChanges = true;
                            }

                            if (cursoExistente.Ativo != cursoDto.Ativo)
                            {
                                cursoExistente.Ativo = cursoDto.Ativo;
                                hasChanges = true;
                            }

                            if (hasChanges)
                            {
                                cursoExistente.UpdatedAt = DateTime.UtcNow;
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

                await _context.SaveChangesAsync();

                result.Success = true;
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

                var turmasFromApi = await _cettproClient.GetAsync<List<TurmaDto>>("api/v1/Turma");

                if (turmasFromApi == null || !turmasFromApi.Any())
                {
                    result.Errors.Add("Nenhuma turma retornada pela API");
                    result.EndTime = DateTime.UtcNow;
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

                        // Buscar curso relacionado
                        Curso? curso = null;
                        if (turmaDto.CursoId.HasValue)
                        {
                            curso = await _context.Cursos
                                .FirstOrDefaultAsync(c => c.IdCettpro == turmaDto.CursoId.Value);
                        }

                        if (turmaExistente == null)
                        {
                            // Inserir nova turma
                            var novaTurma = new Turma
                            {
                                IdCettpro = turmaDto.IdTurma,
                                Nome = turmaDto.Nome,
                                DataInicio = turmaDto.DataInicio,
                                DataTermino = turmaDto.DataTermino,
                                Status = turmaDto.Status,
                                CursoId = curso?.Id ?? Guid.Empty,
                                Curso = curso!
                            };

                            _context.Turmas.Add(novaTurma);
                            result.Inserted++;

                            _logger.LogDebug("Nova turma adicionada: {Nome}", turmaDto.Nome);
                        }
                        else
                        {
                            // Atualizar turma existente
                            bool hasChanges = false;

                            if (turmaExistente.Nome != turmaDto.Nome)
                            {
                                turmaExistente.Nome = turmaDto.Nome;
                                hasChanges = true;
                            }

                            if (turmaExistente.DataInicio != turmaDto.DataInicio)
                            {
                                turmaExistente.DataInicio = turmaDto.DataInicio;
                                hasChanges = true;
                            }

                            if (turmaExistente.DataTermino != turmaDto.DataTermino)
                            {
                                turmaExistente.DataTermino = turmaDto.DataTermino;
                                hasChanges = true;
                            }

                            if (turmaExistente.Status != turmaDto.Status)
                            {
                                turmaExistente.Status = turmaDto.Status;
                                hasChanges = true;
                            }

                            if (curso != null && turmaExistente.CursoId != curso.Id)
                            {
                                turmaExistente.CursoId = curso.Id;
                                turmaExistente.Curso = curso;
                                hasChanges = true;
                            }

                            // Reativar se estava soft-deleted
                            if (turmaExistente.DeletedAt.HasValue)
                            {
                                turmaExistente.DeletedAt = null;
                                hasChanges = true;
                            }

                            if (hasChanges)
                            {
                                result.Updated++;
                                _logger.LogDebug("Turma atualizada: {Nome}", turmaDto.Nome);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar turma {Id}", turmaDto.IdTurma);
                        result.Errors.Add($"Erro na turma {turmaDto.Nome}: {ex.Message}");
                    }
                }

                // Soft delete das turmas que não vieram da API
                var turmasParaDesativar = await _context.Turmas
                    .Where(t => !idsFromApi.Contains(t.IdCettpro) && t.DeletedAt == null)
                    .ToListAsync();

                foreach (var turma in turmasParaDesativar)
                {
                    turma.DeletedAt = DateTime.UtcNow;
                    result.Deleted++;
                    _logger.LogWarning("Turma marcada como deletada: {Nome}", turma.Nome);
                }

                await _context.SaveChangesAsync();

                result.Success = true;
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

            try
            {
                _logger.LogInformation("Iniciando sincronização de alunos...");

                // Buscar matrículas da API (que contêm os alunos)
                var matriculasFromApi = await _cettproClient.GetAsync<List<MatriculaDto>>("api/v1/Matricula");

                if (matriculasFromApi == null || !matriculasFromApi.Any())
                {
                    result.Errors.Add("Nenhuma matrícula retornada pela API");
                    result.EndTime = DateTime.UtcNow;
                    return result;
                }

                // Extrair alunos únicos das matrículas
                var alunosFromApi = matriculasFromApi
                    .SelectMany(m => m.Alunos)
                    .GroupBy(a => a.IdAluno)
                    .Select(g => g.First())
                    .ToList();

                result.TotalProcessed = alunosFromApi.Count;
                _logger.LogInformation("Processando {Count} alunos da CETTPRO", alunosFromApi.Count);

                var idsFromApi = new HashSet<Guid>();

                foreach (var alunoDto in alunosFromApi)
                {
                    try
                    {
                        idsFromApi.Add(alunoDto.IdAluno);

                        var alunoExistente = await _context.Alunos
                            .FirstOrDefaultAsync(a => a.IdCettpro == alunoDto.IdAluno);

                        if (alunoExistente == null)
                        {
                            // Inserir novo aluno
                            var novoAluno = new Aluno
                            {
                                IdCettpro = alunoDto.IdAluno,
                                Nome = alunoDto.Nome,
                                NomeSocial = alunoDto.NomeSocial,
                                NomePai = alunoDto.NomePai,
                                NomeMae = alunoDto.NomeMae,
                                Cpf = alunoDto.Cpf,
                                Rg = alunoDto.Rg,
                                MunicipioId = alunoDto.MunicipioId,
                                DataNascimento = alunoDto.DataNascimento,
                                Genero = alunoDto.Genero,
                                Sexo = alunoDto.Sexo,
                                Nacionalidade = alunoDto.Nacionalidade,
                                EstadoCivil = alunoDto.EstadoCivil,
                                Raca = alunoDto.Raca,
                                Email = alunoDto.Email
                            };

                            _context.Alunos.Add(novoAluno);
                            result.Inserted++;

                            _logger.LogDebug("Novo aluno adicionado: {Nome}", alunoDto.Nome);
                        }
                        else
                        {
                            // Atualizar aluno existente (implementar conforme necessário)
                            // ... lógica de atualização similar aos cursos/turmas
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erro ao processar aluno {Id}", alunoDto.IdAluno);
                        result.Errors.Add($"Erro no aluno {alunoDto.Nome}: {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                _logger.LogInformation(
                    "Sincronização de alunos concluída: {Inserted} inseridos, {Updated} atualizados",
                    result.Inserted, result.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização de alunos");
                result.Success = false;
                result.Errors.Add($"Erro: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

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