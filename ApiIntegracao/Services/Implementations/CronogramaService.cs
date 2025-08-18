using ApiIntegracao.Data;
using ApiIntegracao.DTOs;
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

        public async Task<CronogramaResponseDto> GerarCronogramaAsync(CronogramaRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation(
                    "Gerando cronograma para turma {IdTurmaFat} do curso {IdCursoFat}",
                    request.IdTurmaFat,
                    request.IdCursoFat);

                // 1. Buscar ou criar curso baseado no código FAT
                var curso = await ObterOuCriarCurso(request.IdCursoFat);

                // 2. Buscar ou criar turma
                var turma = await ObterOuCriarTurma(request, curso);

                // 3. Gerar datas das aulas baseado nos horários
                var aulasGeradas = GerarDatasAulas(request.DataInicio, request.DataTermino, request.Horarios);

                // 4. Limpar aulas existentes da turma
                var aulasExistentes = await _context.AulasGeradas
                    .Where(a => a.TurmaId == turma.Id)
                    .ToListAsync();

                _context.AulasGeradas.RemoveRange(aulasExistentes);

                // 5. Criar novas aulas
                var novasAulas = new List<AulaGerada>();
                foreach (var (data, horaInicio, horaFim) in aulasGeradas)
                {
                    var aula = new AulaGerada
                    {
                        TurmaId = turma.Id,
                        DataAula = data,
                        HoraInicio = horaInicio,
                        HoraFim = horaFim,
                        DiaSemana = (int)data.DayOfWeek,
                        Assunto = request.NomeDisciplinaFat,
                        Descricao = $"Aula de {request.NomeDisciplinaFat}"
                    };
                    novasAulas.Add(aula);
                }

                await _context.AulasGeradas.AddRangeAsync(novasAulas);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 6. Preparar resposta
                var aulasDto = novasAulas.Select(a => new AulaGeradaDto
                {
                    DataAula = a.DataAula,
                    HoraInicio = a.HoraInicio,
                    HoraFim = a.HoraFim,
                    DiaSemana = a.DiaSemana,
                    NomeDiaSemana = ObterNomeDiaSemana(a.DiaSemana)
                }).ToList();

                _logger.LogInformation(
                    "Cronograma gerado com sucesso para turma {IdTurmaFat}. Total de aulas: {TotalAulas}",
                    request.IdTurmaFat,
                    novasAulas.Count);

                return new CronogramaResponseDto
                {
                    Status = "Sucesso",
                    Mensagem = $"Cronograma gerado com sucesso. {novasAulas.Count} aulas criadas.",
                    IdTurma = turma.Id,
                    TotalAulasGeradas = novasAulas.Count,
                    AulasGeradas = aulasDto
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao gerar cronograma para turma {IdTurmaFat}", request.IdTurmaFat);

                return new CronogramaResponseDto
                {
                    Status = "Erro",
                    Mensagem = $"Erro ao gerar cronograma: {ex.Message}",
                    IdTurma = Guid.Empty,
                    TotalAulasGeradas = 0,
                    AulasGeradas = new List<AulaGeradaDto>()
                };
            }
        }

        private async Task<Curso> ObterOuCriarCurso(string idCursoFat)
        {
            // Tentar converter o ID para Guid
            if (!Guid.TryParse(idCursoFat, out var cursoGuid))
            {
                throw new ArgumentException($"ID do curso FAT inválido: {idCursoFat}");
            }

            var curso = await _context.Cursos
                .FirstOrDefaultAsync(c => c.IdCettpro == cursoGuid);

            if (curso == null)
            {
                throw new InvalidOperationException($"Curso com ID {idCursoFat} não encontrado. Execute a sincronização primeiro.");
            }

            return curso;
        }

        private async Task<Turma> ObterOuCriarTurma(CronogramaRequestDto request, Curso curso)
        {
            // Tentar converter o ID para Guid
            if (!Guid.TryParse(request.IdTurmaFat, out var turmaGuid))
            {
                throw new ArgumentException($"ID da turma FAT inválido: {request.IdTurmaFat}");
            }

            var turma = await _context.Turmas
                .FirstOrDefaultAsync(t => t.IdCettpro == turmaGuid);

            if (turma == null)
            {
                // Criar turma se não existir
                turma = new Turma
                {
                    IdCettpro = turmaGuid,
                    Nome = $"Turma {request.IdTurmaFat}",
                    DataInicio = request.DataInicio,
                    DataTermino = request.DataTermino,
                    Status = 73189002, // Em Construção
                    CursoId = curso.Id,
                    Curso = curso,
                    IdPortalFat = turmaGuid,
                    DisciplinaIdPortalFat = Guid.TryParse(request.IdDisciplinaFat, out var disciplinaGuid) ? disciplinaGuid : null,
                    DisciplinaNomePortalFat = request.NomeDisciplinaFat
                };

                _context.Turmas.Add(turma);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Nova turma criada: {IdTurma} - {Nome}", turma.IdCettpro, turma.Nome);
            }
            else
            {
                // Atualizar informações da turma
                turma.DataInicio = request.DataInicio;
                turma.DataTermino = request.DataTermino;
                turma.DisciplinaIdPortalFat = Guid.TryParse(request.IdDisciplinaFat, out var disciplinaGuid) ? disciplinaGuid : null;
                turma.DisciplinaNomePortalFat = request.NomeDisciplinaFat;

                await _context.SaveChangesAsync();
            }

            return turma;
        }

        private List<(DateTime data, TimeSpan horaInicio, TimeSpan horaFim)> GerarDatasAulas(
            DateTime dataInicio,
            DateTime dataTermino,
            List<HorarioDto> horarios)
        {
            var aulas = new List<(DateTime, TimeSpan, TimeSpan)>();
            var dataAtual = dataInicio.Date;

            while (dataAtual <= dataTermino.Date)
            {
                var diaSemana = (int)dataAtual.DayOfWeek;
                var horarioDoDia = horarios.FirstOrDefault(h => h.DiaSemana == diaSemana);

                if (horarioDoDia != null)
                {
                    if (TimeSpan.TryParse(horarioDoDia.Inicio, out var horaInicio) &&
                        TimeSpan.TryParse(horarioDoDia.Fim, out var horaFim))
                    {
                        aulas.Add((dataAtual, horaInicio, horaFim));
                    }
                }

                dataAtual = dataAtual.AddDays(1);
            }

            return aulas;
        }

        private static string ObterNomeDiaSemana(int diaSemana)
        {
            return diaSemana switch
            {
                0 => "Domingo",
                1 => "Segunda-feira",
                2 => "Terça-feira",
                3 => "Quarta-feira",
                4 => "Quinta-feira",
                5 => "Sexta-feira",
                6 => "Sábado",
                _ => "Desconhecido"
            };
        }
    }
}