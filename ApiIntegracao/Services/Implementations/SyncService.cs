using ApiIntegracao.Data;
using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.Turma;
using ApiIntegracao.Exceptions;
using ApiIntegracao.Infrastructure.HttpClients;
using ApiIntegracao.Infrastructure.JsonConverters;
using ApiIntegracao.Models;
using ApiIntegracao.Services.Contracts;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
            List<ProgramaDto>? programasFromApi = null;

            try
            {
                _logger.LogInformation("Iniciando sincronização de cursos...");

                // ETAPA 1: Obter a resposta como um documento JSON genérico em vez de uma lista.
                // Isto evita o erro de desserialização imediato.
                var jsonDocument = await _cettproClient.GetAsync<JsonDocument>("api/v1/RetornaCursos");

                if (jsonDocument == null)
                {
                    _logger.LogWarning("A API da CETTPRO não retornou dados (resposta nula).");
                    result.Success = true; // Não é um erro, apenas não há dados.
                    result.EndTime = DateTime.UtcNow;
                    await LogSyncResult("Curso", result);
                    return result;
                }

                // ETAPA 2: Desserialização manual e segura.
                // Convertemos o JsonDocument para uma string e tentamos desserializá-la agora.
                // Isto permite um controlo muito maior e um diagnóstico preciso do erro.
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        // Adicione aqui os seus conversores personalizados, se eles forem necessários
                        // para os campos internos, como 'cargaHoraria' e 'modalidadeId'.
                        Converters = { new CargaHorariaToStringConverter(), new StringToNullableGuidConverter() }
                    };

                    // IMPORTANTE: A API DEVOLVE O JSON COMO STRING, TEM QUE PEGAR ASSIM ANTES DE CONVERTER EM JSON.
                    string rawJson = jsonDocument.RootElement.GetString() ?? "[]";
                    programasFromApi = JsonSerializer.Deserialize<List<ProgramaDto>>(rawJson, options);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Erro final ao tentar desserializar o JSON recebido da CETTPRO. O formato dos dados é inválido.");
                    // Logar o início do JSON para ajudar a diagnosticar
                    _logger.LogDebug("Início do JSON recebido: {json}", jsonDocument.RootElement.ToString().Substring(0, 200));
                    throw new CettproApiException(500, "O formato do JSON retornado pela API de cursos é inválido e não pôde ser processado.", jsonDocument.RootElement.ToString());
                }


                // A partir daqui, a lógica original continua, agora com a certeza de que os dados foram desserializados corretamente.
                var cursosFromApi = programasFromApi?.SelectMany(p => p.Cursos).ToList();

                if (cursosFromApi == null || !cursosFromApi.Any())
                {
                    result.Errors.Add("Nenhum curso encontrado nos programas retornados pela API.");
                    result.Success = true;
                    _logger.LogWarning("Nenhum curso foi encontrado nos programas para sincronização.");
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
                        var cursoExistente = await _context.Cursos
                            .FirstOrDefaultAsync(c => c.IdCettpro == cursoDto.IdCurso);

                        if (cursoExistente == null)
                        {
                            var novoCurso = new Curso
                            {
                                IdCettpro = cursoDto.IdCurso,
                                NomeCurso = cursoDto.NomeCurso,
                                CargaHoraria = cursoDto.CargaHoraria?.ToString(),
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
                            if (cursoExistente.CargaHoraria != cursoDto.CargaHoraria?.ToString()) { cursoExistente.CargaHoraria = cursoDto.CargaHoraria?.ToString(); hasChanges = true; }
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
                        _logger.LogError(ex, "Erro ao processar o curso com ID CETTPRO {Id}", cursoDto.IdCurso);
                        result.Errors.Add($"Erro no curso '{cursoDto.NomeCurso}': {ex.Message}");
                    }
                }

                var cursosParaDesativar = await _context.Cursos
                    .Where(c => !idsFromApi.Contains(c.IdCettpro) && c.DeletedAt == null)
                    .ToListAsync();

                foreach (var curso in cursosParaDesativar)
                {
                    curso.DeletedAt = DateTime.UtcNow;
                    result.Deleted++;
                }

                await _context.SaveChangesAsync();
                result.Success = !result.Errors.Any();
                _logger.LogInformation(
                   "Sincronização de cursos concluída: {Inserted} inseridos, {Updated} atualizados, {Deleted} deletados.",
                   result.Inserted, result.Updated, result.Deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha crítica durante a sincronização de cursos.");
                result.Success = false;
                result.Errors.Add($"Erro geral na sincronização: {ex.Message}");
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