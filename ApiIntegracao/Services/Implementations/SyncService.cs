using ApiIntegracao.Data;
using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.Cettpro;
using ApiIntegracao.DTOs.Matricula;
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
    /// <summary>
    /// Serviço de sincronização entre a API da CETTPRO e o banco de dados local.
    /// </summary>
    public class SyncService : ISyncService
    {
        private readonly ICettproApiClient _cettproClient;
        private readonly ApiIntegracaoDbContext _context;
        private readonly ILogger<SyncService> _logger;

        /// <summary>
        /// Inicializa o serviço de sincronização.
        /// </summary>
        /// <param name="cettproClient"></param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        public SyncService(
            ICettproApiClient cettproClient,
            ApiIntegracaoDbContext context,
            ILogger<SyncService> logger)
        {
            _cettproClient = cettproClient;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Realiza a sincronização completa de cursos, turmas e alunos
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Sincroniza os cursos da CETTPRO com o banco de dados local.
        /// Obtém os dados da API, realiza a desserialização e atualiza/inclui/exclui cursos conforme necessário.
        /// </summary>
        /// <returns>Retorna um <see cref="SyncResult"/> contendo o resultado da sincronização.</returns>
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

        /// <inheritdoc/>
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

        /// <summary>
        /// Sincroniza turmas ativas dos últimos 30 dias usando o endpoint CursoQualificacao
        /// </summary>
        /// <returns>Resultado da sincronização</returns>
        public async Task<SyncResult> SyncTurmasAtivasAsync()
        {
            var result = new SyncResult { StartTime = DateTime.UtcNow };
            var dataInicial = DateTime.Now.AddDays(-30);
            var dataFinal = DateTime.Now;

            try
            {
                _logger.LogInformation("Iniciando sincronização de turmas ativas dos últimos 30 dias...");

                // 1. Buscar turmas ativas através do endpoint CursoQualificacao
                var cursosQualificacao = await BuscarTurmasAtivasPorPeriodo(dataInicial, dataFinal);

                if (cursosQualificacao == null || !cursosQualificacao.Any())
                {
                    _logger.LogWarning("Nenhuma turma ativa encontrada nos últimos 30 dias");
                    result.Success = true;
                    result.EndTime = DateTime.UtcNow;
                    return result;
                }

                var todasTurmas = cursosQualificacao.SelectMany(c => c.Turmas).ToList();
                result.TotalProcessed = todasTurmas.Count;

                _logger.LogInformation("Processando {Count} turmas ativas encontradas", todasTurmas.Count);

                // 2. Processar cada turma individualmente
                foreach (var turmaQualificacao in todasTurmas)
                {
                    await ProcessarTurmaCompleta(turmaQualificacao, result);
                }

                await _context.SaveChangesAsync();
                result.Success = !result.Errors.Any();

                _logger.LogInformation(
                    "Sincronização de turmas ativas concluída: {Inserted} inseridas, {Updated} atualizadas",
                    result.Inserted, result.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização de turmas ativas");
                result.Success = false;
                result.Errors.Add($"Erro: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            await LogSyncResult("TurmasAtivas", result);
            return result;
        }

        /// <summary>
        /// Versão atualizada do SyncTurmasPorPeriodoAsync usando CursoQualificacao
        /// </summary>
        public async Task<SyncResult> SyncTurmasPorPeriodoAsync(DateTime dataInicial, DateTime dataFinal)
        {
            var result = new SyncResult { StartTime = DateTime.UtcNow };

            try
            {
                _logger.LogInformation("Iniciando sincronização de turmas por período de {DataInicial} a {DataFinal}",
                    dataInicial, dataFinal);

                // 1. Buscar turmas através do endpoint CursoQualificacao
                var cursosQualificacao = await BuscarTurmasAtivasPorPeriodo(dataInicial, dataFinal);

                if (cursosQualificacao == null || !cursosQualificacao.Any())
                {
                    _logger.LogWarning("Nenhuma turma encontrada no período especificado");
                    result.Success = true;
                    result.EndTime = DateTime.UtcNow;
                    return result;
                }

                var todasTurmas = cursosQualificacao.SelectMany(c => c.Turmas).ToList();
                result.TotalProcessed = todasTurmas.Count;

                _logger.LogInformation("Processando {Count} turmas encontradas no período", todasTurmas.Count);

                // 2. Processar cada turma individualmente
                foreach (var turmaQualificacao in todasTurmas)
                {
                    await ProcessarTurmaCompleta(turmaQualificacao, result);
                }

                await _context.SaveChangesAsync();
                result.Success = !result.Errors.Any();

                _logger.LogInformation(
                    "Sincronização de turmas por período concluída: {Inserted} inseridas, {Updated} atualizadas",
                    result.Inserted, result.Updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante sincronização de turmas por período");
                result.Success = false;
                result.Errors.Add($"Erro: {ex.Message}");
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
            }

            await LogSyncResult("TurmasPorPeriodo", result);
            return result;
        }

        /// <summary>
        /// Busca turmas ativas através do endpoint CursoQualificacao
        /// </summary>
        private async Task<List<CursoQualificacaoDto>> BuscarTurmasAtivasPorPeriodo(DateTime dataInicial, DateTime dataFinal)
        {
            try
            {
                // Parametros vazios para buscar todas as turmas ativas
                var requestBody = new { };

                var cursosQualificacao = await _cettproClient.PostAsync<List<CursoQualificacaoDto>>(
                    "api/v1/CursoQualificacao", requestBody);

                if (cursosQualificacao == null)
                {
                    _logger.LogWarning("Resposta nula do endpoint CursoQualificacao");
                    return new List<CursoQualificacaoDto>();
                }

                // Filtrar turmas por período (se necessário)
                var cursosComTurmasFiltradas = cursosQualificacao
                    .Where(c => c.Turmas.Any())
                    .Select(c => new CursoQualificacaoDto
                    {
                        IdCurso = c.IdCurso,
                        NomeCurso = c.NomeCurso,
                        CargaHoraria = c.CargaHoraria,
                        Descricao = c.Descricao,
                        Ativo = c.Ativo,
                        Modalidades = c.Modalidades,
                        Arcos = c.Arcos,
                        Turmas = c.Turmas.Where(t =>
                            t.DataInicio.HasValue &&
                            t.DataInicio.Value.Date >= dataInicial.Date &&
                            t.DataInicio.Value.Date <= dataFinal.Date).ToList()
                    })
                    .Where(c => c.Turmas.Any())
                    .ToList();

                return cursosComTurmasFiltradas;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar turmas através do endpoint CursoQualificacao");
                throw;
            }
        }

        /// <summary>
        /// Processa uma turma completa: atualiza dados e sincroniza alunos
        /// </summary>
        private async Task ProcessarTurmaCompleta(TurmaQualificacaoDto turmaQualificacao, SyncResult result)
        {
            try
            {
                // 1. Atualizar/inserir dados da turma
                var turmaExistente = await _context.Turmas
                    .FirstOrDefaultAsync(t => t.IdCettpro == turmaQualificacao.IdTurma);

                if (turmaExistente == null)
                {
                    // Inserir nova turma
                    var novaTurma = new Turma
                    {
                        IdCettpro = turmaQualificacao.IdTurma,
                        Nome = turmaQualificacao.Nome,
                        DataInicio = turmaQualificacao.DataInicio,
                        DataTermino = turmaQualificacao.DataTermino,
                        Status = turmaQualificacao.Status,
                        Ativo = true,
                        CursoId = await ObterCursoId(turmaQualificacao),
                        UnidadeEnsinoId = await ObterUnidadeEnsinoId(turmaQualificacao)
                    };

                    _context.Turmas.Add(novaTurma);
                    result.Inserted++;
                }
                else
                {
                    // Atualizar turma existente
                    turmaExistente.Nome = turmaQualificacao.Nome;
                    turmaExistente.DataInicio = turmaQualificacao.DataInicio;
                    turmaExistente.DataTermino = turmaQualificacao.DataTermino;
                    turmaExistente.Status = turmaQualificacao.Status;
                    turmaExistente.DeletedAt = null; // Reativar se estava soft-deleted
                    turmaExistente.UpdatedAt = DateTime.UtcNow;

                    result.Updated++;
                }

                // 2. Buscar e atualizar dados completos da turma com matrículas
                await AtualizarMatriculasDaTurma(turmaQualificacao.IdTurma, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar turma {TurmaId}: {Erro}",
                    turmaQualificacao.IdTurma, ex.Message);
                result.Errors.Add($"Erro ao processar turma {turmaQualificacao.Nome}: {ex.Message}");
            }
        }

        /// <summary>
        /// Atualiza as matrículas de uma turma específica usando o endpoint Matricula/Turma
        /// </summary>
        private async Task AtualizarMatriculasDaTurma(Guid turmaId, SyncResult result)
        {
            try
            {
                _logger.LogDebug("Atualizando matrículas da turma {TurmaId}", turmaId);

                // Buscar dados completos da turma com matrículas
                var matriculaTurma = await _cettproClient.GetAsync<MatriculaTurmaDto>(
                    $"api/v1/Matricula/Turma?idTurma={turmaId}");

                if (matriculaTurma?.Matriculas == null || !matriculaTurma.Matriculas.Any())
                {
                    _logger.LogDebug("Nenhuma matrícula encontrada para a turma {TurmaId}", turmaId);
                    return;
                }

                _logger.LogDebug("Processando {Count} matrículas da turma {TurmaId}",
                    matriculaTurma.Matriculas.Count, turmaId);

                // Processar cada matrícula
                foreach (var matriculaDto in matriculaTurma.Matriculas)
                {
                    await ProcessarMatricula(matriculaDto, turmaId, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar matrículas da turma {TurmaId}", turmaId);
                result.Errors.Add($"Erro ao atualizar matrículas da turma {turmaId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa uma matrícula individual e seus alunos
        /// </summary>
        private async Task ProcessarMatricula(MatriculaDto matriculaDto, Guid turmaId, SyncResult result)
        {
            try
            {
                var turmaLocal = await _context.Turmas
                    .FirstOrDefaultAsync(t => t.IdCettpro == turmaId);

                if (turmaLocal == null)
                {
                    _logger.LogWarning("Turma local não encontrada para ID {TurmaId}", turmaId);
                    return;
                }

                // Processar cada aluno da matrícula
                foreach (var alunoDto in matriculaDto.Alunos)
                {
                    await ProcessarAluno(alunoDto, matriculaDto, turmaLocal, result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar matrícula {MatriculaId}", matriculaDto.IdMatricula);
                result.Errors.Add($"Erro ao processar matrícula {matriculaDto.IdMatricula}: {ex.Message}");
            }
        }

        /// <summary>
        /// Processa um aluno individual e sua matrícula
        /// </summary>
        private async Task ProcessarAluno(AlunoMatriculaDto alunoDto, MatriculaDto matriculaDto,
            Turma turmaLocal, SyncResult result)
        {
            try
            {
                // 1. Verificar/inserir/atualizar aluno
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
                        Cnh = alunoDto.Cnh,
                        Cpf = alunoDto.Cpf,
                        Rg = alunoDto.Rg,
                        MunicipioId = alunoDto.MunicipioId,
                        TipoPNE = alunoDto.TipoPNE,
                        DataNascimento = ParseDataNascimento(alunoDto.DataNascimento),
                        Genero = alunoDto.Genero,
                        Sexo = alunoDto.Sexo,
                        Nacionalidade = alunoDto.Nacionalidade,
                        EstadoCivil = alunoDto.EstadoCivil,
                        Raca = alunoDto.Raca,
                        Email = alunoDto.Email,
                        Ativo = true
                    };

                    _context.Alunos.Add(novoAluno);
                    alunoExistente = novoAluno;
                }
                else
                {
                    // Atualizar aluno existente
                    alunoExistente.Nome = alunoDto.Nome;
                    alunoExistente.NomeSocial = alunoDto.NomeSocial;
                    alunoExistente.Email = alunoDto.Email;
                    alunoExistente.DeletedAt = null;
                    alunoExistente.UpdatedAt = DateTime.UtcNow;
                }

                // 2. Verificar/inserir/atualizar matrícula
                var matriculaExistente = await _context.Matriculas
                    .FirstOrDefaultAsync(m => m.IdCettpro == matriculaDto.IdMatricula);

                if (matriculaExistente == null)
                {
                    // Inserir nova matrícula
                    var novaMatricula = new Matricula
                    {
                        IdCettpro = matriculaDto.IdMatricula,
                        AlunoId = alunoExistente.Id,
                        TurmaId = turmaLocal.Id,
                        Status = matriculaDto.Status,
                        DataMatricula = DateTime.UtcNow
                    };

                    _context.Matriculas.Add(novaMatricula);
                }
                else
                {
                    // Atualizar matrícula existente
                    matriculaExistente.Status = matriculaDto.Status;
                    matriculaExistente.UpdatedAt = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar aluno {AlunoId}", alunoDto.IdAluno);
                result.Errors.Add($"Erro ao processar aluno {alunoDto.Nome}: {ex.Message}");
            }
        }

        /// <summary>
        /// Métodos auxiliares
        /// </summary>
        private async Task<Guid?> ObterCursoId(TurmaQualificacaoDto turmaDto)
        {
            // Lógica para encontrar o curso baseado no nome da turma ou outros critérios
            // Por enquanto retorna null, mas pode ser implementado conforme necessário
            return null;
        }

        private async Task<Guid?> ObterUnidadeEnsinoId(TurmaQualificacaoDto turmaDto)
        {
            if (turmaDto.UnidadeEnsino?.FirstOrDefault() != null)
            {
                var unidade = turmaDto.UnidadeEnsino.First();
                var unidadeLocal = await _context.UnidadesEnsino
                    .FirstOrDefaultAsync(u => u.IdCettpro == unidade.IdUnidadeEnsino);

                return unidadeLocal?.Id;
            }

            return null;
        }

        private DateTime? ParseDataNascimento(string dataNascimento)
        {
            if (DateTime.TryParse(dataNascimento, out var data))
            {
                return data;
            }

            return null;
        }

        /// <summary>
        /// Sincroniza os alunos da CETTPRO com o banco de dados local.
        /// Observação: Não há endpoint global na API CETTPRO para buscar todos os alunos.
        /// A sincronização de alunos deve ocorrer em um contexto de turma específica.
        /// </summary>
        /// <returns>Retorna um <see cref="SyncResult"/> contendo o resultado da sincronização.</returns>
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