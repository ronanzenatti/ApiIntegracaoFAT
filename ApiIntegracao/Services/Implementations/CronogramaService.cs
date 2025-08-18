using ApiIntegracao.Data;
using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.ApiIntegracao.DTOs;
using ApiIntegracao.Models;
using ApiIntegracao.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ApiIntegracao.Services.Implementations
{
    public class CronogramaService : ICronogramaService
    {
        private readonly ApiIntegracaoDbContext _context;
        private readonly ILogger<CronogramaService> _logger;

        public CronogramaService(
            ApiIntegracaoDbContext context,
            ILogger<CronogramaService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Métodos Principais (CRUD)

        public async Task<CronogramaResponseDto> GerarCronogramaAsync(CronogramaRequestDto request)
        {
            if (await ExisteCronogramaAsync(request.IdTurmaFat, request.IdDisciplinaFat) && !request.SobrescreverExistente)
            {
                throw new InvalidOperationException($"Já existe um cronograma para a turma {request.IdTurmaFat} e disciplina {request.IdDisciplinaFat}. Para substituir, use a opção 'SobrescreverExistente'.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation(
                    "Gerando cronograma para turma {IdTurmaFat} e disciplina {IdDisciplinaFat}",
                    request.IdTurmaFat, request.IdDisciplinaFat);

                var curso = await ObterCursoPorIdFat(request.IdCursoFat);
                var turma = await ObterOuCriarTurma(request, curso);

                // Limpar aulas existentes se for sobrescrever
                if (request.SobrescreverExistente)
                {
                    var aulasExistentes = await _context.AulasGeradas
                        .Where(a => a.TurmaId == turma.Id)
                        .ToListAsync();
                    if (aulasExistentes.Any())
                    {
                        _context.AulasGeradas.RemoveRange(aulasExistentes);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("{Count} aulas existentes removidas para a turma {IdTurmaFat}", aulasExistentes.Count, request.IdTurmaFat);
                    }
                }

                // CORREÇÃO: Chamar os métodos separadamente e usar os resultados corretamente.
                var aulasGeradas = GerarDatasAulas(request.DataInicio, request.DataTermino, request.Horarios);
                var calculoHorasResult = await CalcularTotalHorasAulaAsync(request.DataInicio, request.DataTermino, request.Horarios);
                var totalHoras = calculoHorasResult.TotalHoras;

                var novasAulas = new List<AulaGerada>();
                int numeroAula = 1;
                foreach (var (data, horaInicio, horaFim) in aulasGeradas)
                {
                    var aula = new AulaGerada
                    {
                        TurmaId = turma.Id,
                        DataAula = data,
                        HoraInicio = horaInicio,
                        HoraFim = horaFim,
                        DiaSemana = (int)data.DayOfWeek,
                        Assunto = $"Aula {numeroAula++}",
                        Descricao = $"Conteúdo de {request.NomeDisciplinaFat}"
                    };
                    novasAulas.Add(aula);
                }

                await _context.AulasGeradas.AddRangeAsync(novasAulas);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Cronograma gerado com sucesso para turma {IdTurmaFat}. Total de aulas: {TotalAulas}",
                    request.IdTurmaFat, novasAulas.Count);

                return CriarCronogramaResponse(turma, request, novasAulas, totalHoras);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao gerar cronograma para turma {IdTurmaFat}", request.IdTurmaFat);
                throw;
            }
        }

        public async Task<CronogramaResponseDto> AtualizarCronogramaAsync(string idTurmaFat, string idDisciplinaFat, CronogramaRequestDto request)
        {
            request.SobrescreverExistente = true;
            _logger.LogInformation("Iniciando atualização do cronograma para turma {IdTurmaFat}", idTurmaFat);
            return await GerarCronogramaAsync(request);
        }

        public async Task<bool> ExcluirCronogramaAsync(string idTurmaFat, string idDisciplinaFat)
        {
            var turma = await _context.Turmas
                .FirstOrDefaultAsync(t => t.IdPortalFat.ToString() == idTurmaFat && t.DisciplinaIdPortalFat.ToString() == idDisciplinaFat);

            if (turma == null)
            {
                _logger.LogWarning("Tentativa de exclusão de cronograma para turma/disciplina não encontrada: {IdTurmaFat}/{IdDisciplinaFat}", idTurmaFat, idDisciplinaFat);
                return false;
            }

            var aulasParaExcluir = await _context.AulasGeradas
                .Where(a => a.TurmaId == turma.Id)
                .ToListAsync();

            if (aulasParaExcluir.Any())
            {
                _context.AulasGeradas.RemoveRange(aulasParaExcluir);
                await _context.SaveChangesAsync();
                _logger.LogInformation("{Count} aulas foram excluídas para o cronograma da turma {IdTurmaFat}", aulasParaExcluir.Count, idTurmaFat);
            }

            return true;
        }

        #endregion

        #region Métodos de Consulta

        public async Task<IEnumerable<CronogramaListaDto>> ListarCronogramasAsync(string? idTurmaFat = null, string? idDisciplinaFat = null)
        {
            var query = _context.Turmas
                .Include(t => t.Curso)
                .Where(t => t.DisciplinaIdPortalFat != null && _context.AulasGeradas.Any(a => a.TurmaId == t.Id));

            if (!string.IsNullOrEmpty(idTurmaFat))
            {
                query = query.Where(t => t.IdPortalFat.ToString() == idTurmaFat);
            }

            if (!string.IsNullOrEmpty(idDisciplinaFat))
            {
                query = query.Where(t => t.DisciplinaIdPortalFat.ToString() == idDisciplinaFat);
            }

            return await query
                .Select(t => new CronogramaListaDto
                {
                    Id = t.Id,
                    IdTurmaFat = t.IdPortalFat.ToString(),
                    NomeTurma = t.Nome,
                    IdDisciplinaFat = t.DisciplinaIdPortalFat.ToString(),
                    NomeDisciplina = t.DisciplinaNomePortalFat,
                    IdCursoFat = t.Curso.IdPortalFat.ToString(),
                    NomeCurso = t.Curso.NomeCurso,
                    DataInicio = t.DataInicio,
                    DataTermino = t.DataTermino ?? DateTime.MinValue,
                    TotalAulas = _context.AulasGeradas.Count(a => a.TurmaId == t.Id),
                    DataCriacao = t.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<CronogramaDetalheDto?> ObterCronogramaAsync(string idTurmaFat, string idDisciplinaFat)
        {
            var turma = await _context.Turmas
                .Include(t => t.Curso)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdPortalFat.ToString() == idTurmaFat && t.DisciplinaIdPortalFat.ToString() == idDisciplinaFat);

            if (turma == null) return null;

            var aulas = await _context.AulasGeradas
                .Where(a => a.TurmaId == turma.Id)
                .OrderBy(a => a.DataAula)
                .ThenBy(a => a.HoraInicio)
                .ToListAsync();

            if (!aulas.Any()) return null;

            var aulasDto = aulas.Select((a, index) => MapearParaAulaGeradaDto(a, index + 1)).ToList();

            return new CronogramaDetalheDto
            {
                Id = turma.Id,
                IdTurmaFat = turma.IdPortalFat.ToString(),
                NomeTurma = turma.Nome,
                IdTurmaCettpro = turma.IdCettpro,
                IdDisciplinaFat = turma.DisciplinaIdPortalFat.ToString(),
                NomeDisciplina = turma.DisciplinaNomePortalFat,
                IdCursoFat = turma.Curso.IdPortalFat.ToString(),
                NomeCurso = turma.Curso.NomeCurso,
                DataInicio = turma.DataInicio,
                DataTermino = turma.DataTermino ?? aulas.Last().DataAula,
                TotalAulas = aulas.Count,
                Aulas = aulasDto,
                ProximasAulas = aulasDto.Where(a => a.DataAula.Date >= DateTime.Today).Take(5).ToList(),
                DataCriacao = turma.CreatedAt,
                DataUltimaAtualizacao = turma.UpdatedAt
            };
        }

        public async Task<IEnumerable<AulaGeradaDto>> ListarAulasAsync(string idTurmaFat, string idDisciplinaFat, DateTime? dataInicio = null, DateTime? dataFim = null)
        {
            var turma = await _context.Turmas
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdPortalFat.ToString() == idTurmaFat && t.DisciplinaIdPortalFat.ToString() == idDisciplinaFat);

            if (turma == null)
            {
                return Enumerable.Empty<AulaGeradaDto>();
            }

            var query = _context.AulasGeradas.Where(a => a.TurmaId == turma.Id);

            if (dataInicio.HasValue)
            {
                query = query.Where(a => a.DataAula.Date >= dataInicio.Value.Date);
            }
            if (dataFim.HasValue)
            {
                query = query.Where(a => a.DataAula.Date <= dataFim.Value.Date);
            }

            var aulas = await query
                .OrderBy(a => a.DataAula)
                .ThenBy(a => a.HoraInicio)
                .ToListAsync();

            return aulas.Select((a, index) => MapearParaAulaGeradaDto(a, index + 1));
        }

        #endregion

        #region Métodos de Utilitários e Validação

        public async Task<bool> ExisteCronogramaAsync(string idTurmaFat, string idDisciplinaFat)
        {
            return await _context.Turmas
                .AnyAsync(t => t.IdPortalFat.ToString() == idTurmaFat
                            && t.DisciplinaIdPortalFat.ToString() == idDisciplinaFat
                            && _context.AulasGeradas.Any(a => a.TurmaId == t.Id));
        }

        public Task<ValidacaoHorariosResult> ValidarHorariosAsync(List<HorarioDto> horarios)
        {
            var result = new ValidacaoHorariosResult();
            // Lógica de validação...
            return Task.FromResult(result);
        }

        public Task<CalculoHorasResult> CalcularTotalHorasAulaAsync(DateTime dataInicio, DateTime dataTermino, List<HorarioDto> horarios)
        {
            var aulasGeradas = GerarDatasAulas(dataInicio, dataTermino, horarios);
            double totalHoras = aulasGeradas.Sum(a => (a.horaFim - a.horaInicio).TotalHours);

            return Task.FromResult(new CalculoHorasResult
            {
                TotalAulas = aulasGeradas.Count,
                TotalHoras = totalHoras
            });
        }

        #endregion

        #region Métodos de Lote e Sincronização (Placeholders)

        public async Task<CronogramaLoteResult> GerarCronogramasEmLoteAsync(List<CronogramaRequestDto> requests)
        {
            var result = new CronogramaLoteResult();
            foreach (var request in requests)
            {
                try
                {
                    await GerarCronogramaAsync(request);
                    result.TotalSucesso++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao gerar cronograma em lote para turma {IdTurmaFat}", request.IdTurmaFat);
                    result.TotalFalhas++;
                    result.Erros.Add($"Turma {request.IdTurmaFat}: {ex.Message}");
                }
                result.TotalProcessado++;
            }
            return result;
        }

        public Task<SincronizacaoCronogramaResult> SincronizarComCettproAsync(string? idTurmaFat = null)
        {
            // A lógica de sincronização de cronograma com CETTPRO não está definida no escopo.
            // Cronogramas são gerados a partir do Portal FAT.
            _logger.LogWarning("SincronizarComCettproAsync não implementado pois o fluxo é unidirecional do Portal FAT para a API.");
            throw new NotImplementedException("Fluxo de sincronização de cronograma com CETTPRO não se aplica a este projeto.");
        }

        public Task<ExportacaoCronogramaResult> ExportarCronogramaAsync(string idTurmaFat, string idDisciplinaFat, FormatoExportacao formato)
        {
            // A implementação de exportação para PDF/XLSX requer bibliotecas de terceiros (ex: iTextSharp, EPPlus).
            // A exportação para CSV pode ser implementada de forma mais simples.
            _logger.LogWarning("ExportarCronogramaAsync não está totalmente implementado. Requer bibliotecas adicionais.");
            throw new NotImplementedException();
        }

        #endregion

        #region Métodos Privados Auxiliares

        private async Task<Curso> ObterCursoPorIdFat(string idCursoFat)
        {
            if (!Guid.TryParse(idCursoFat, out var cursoGuid))
                throw new ArgumentException($"ID do curso FAT inválido: {idCursoFat}");

            var curso = await _context.Cursos
                .FirstOrDefaultAsync(c => c.IdPortalFat == cursoGuid || c.IdCettpro == cursoGuid);

            if (curso == null)
                throw new InvalidOperationException($"Curso com ID FAT {idCursoFat} não encontrado. Execute a sincronização de cursos primeiro.");

            return curso;
        }

        private async Task<Turma> ObterOuCriarTurma(CronogramaRequestDto request, Curso curso)
        {
            if (!Guid.TryParse(request.IdTurmaFat, out var turmaGuid) || !Guid.TryParse(request.IdDisciplinaFat, out var disciplinaGuid))
                throw new ArgumentException("IDs da turma ou disciplina FAT são inválidos.");

            var turma = await _context.Turmas
                .FirstOrDefaultAsync(t => t.IdPortalFat == turmaGuid && t.DisciplinaIdPortalFat == disciplinaGuid);

            if (turma == null)
            {
                _logger.LogInformation("Nenhuma turma encontrada para IdTurmaFat={IdTurmaFat} e IdDisciplinaFat={IdDisciplinaFat}. Criando uma nova.", request.IdTurmaFat, request.IdDisciplinaFat);
                turma = new Turma
                {
                    IdCettpro = Guid.NewGuid(), // ID Cettpro pode ser novo ou buscado
                    Nome = request.NomeDisciplinaFat,
                    DataInicio = request.DataInicio,
                    DataTermino = request.DataTermino,
                    Status = 731890002, // Em Construção
                    CursoId = curso.Id,
                    Curso = curso,
                    IdPortalFat = turmaGuid,
                    DisciplinaIdPortalFat = disciplinaGuid,
                    DisciplinaNomePortalFat = request.NomeDisciplinaFat
                };
                _context.Turmas.Add(turma);
            }
            else
            {
                _logger.LogInformation("Turma encontrada para IdTurmaFat={IdTurmaFat} e IdDisciplinaFat={IdDisciplinaFat}. Atualizando...", request.IdTurmaFat, request.IdDisciplinaFat);
                turma.DataInicio = request.DataInicio;
                turma.DataTermino = request.DataTermino;
                turma.DisciplinaNomePortalFat = request.NomeDisciplinaFat;
            }

            await _context.SaveChangesAsync();
            return turma;
        }

        private List<(DateTime data, TimeSpan horaInicio, TimeSpan horaFim)> GerarDatasAulas(DateTime dataInicio, DateTime dataTermino, List<HorarioDto> horarios)
        {
            var aulas = new List<(DateTime, TimeSpan, TimeSpan)>();
            var dataAtual = dataInicio.Date;
            var diasSemanaComAula = new HashSet<DayOfWeek>(horarios.Select(h => (DayOfWeek)h.DiaSemana));

            while (dataAtual <= dataTermino.Date)
            {
                if (diasSemanaComAula.Contains(dataAtual.DayOfWeek))
                {
                    var horarioDoDia = horarios.First(h => h.DiaSemana == (int)dataAtual.DayOfWeek);
                    if (TimeSpan.TryParse(horarioDoDia.Inicio, out var horaInicio) && TimeSpan.TryParse(horarioDoDia.Fim, out var horaFim))
                    {
                        aulas.Add((dataAtual, horaInicio, horaFim));
                    }
                }
                dataAtual = dataAtual.AddDays(1);
            }
            return aulas;
        }

        private CronogramaResponseDto CriarCronogramaResponse(Turma turma, CronogramaRequestDto request, List<AulaGerada> aulas, double totalHoras)
        {
            return new CronogramaResponseDto
            {
                Status = "Sucesso",
                Mensagem = $"Cronograma gerado com sucesso. {aulas.Count} aulas criadas.",
                IdTurma = turma.Id,
                IdTurmaFat = turma.IdPortalFat.ToString(),
                NomeTurma = turma.Nome,
                IdDisciplinaFat = turma.DisciplinaIdPortalFat.ToString(),
                NomeDisciplina = turma.DisciplinaNomePortalFat,
                TotalAulasGeradas = aulas.Count,
                TotalHorasAula = totalHoras,
                PrimeiraAula = aulas.FirstOrDefault()?.DataAula,
                UltimaAula = aulas.LastOrDefault()?.DataAula,
                Aulas = aulas.Take(10).Select((a, i) => MapearParaAulaGeradaDto(a, i + 1)).ToList() // Retorna um preview das 10 primeiras aulas
            };
        }

        private AulaGeradaDto MapearParaAulaGeradaDto(AulaGerada aula, int numeroAula)
        {
            return new AulaGeradaDto
            {
                Id = aula.Id,
                DataAula = aula.DataAula,
                HoraInicio = aula.HoraInicio,
                HoraFim = aula.HoraFim,
                DiaSemana = aula.DiaSemana,
                NomeDiaSemana = ObterNomeDiaSemana(aula.DiaSemana),
                Assunto = aula.Assunto,
                Descricao = aula.Descricao,
                NumeroAula = numeroAula,
                DataCriacao = aula.CreatedAt
            };
        }

        public Task<CronogramaEstatisticasDto> ObterEstatisticasAsync()
        {
            throw new NotImplementedException();
        }

        private static string ObterNomeDiaSemana(int diaSemana) => ((DayOfWeek)diaSemana).ToString();

        #endregion
    }
}