// Services/Implementations/FrequenciaService.cs
using ApiIntegracao.Data;
using ApiIntegracao.DTOs.Frequencia;
using ApiIntegracao.Exceptions;
using ApiIntegracao.Infrastructure.FileProcessing;
using ApiIntegracao.Infrastructure.HttpClients;
using ApiIntegracao.Models;
using ApiIntegracao.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ApiIntegracao.Services.Implementations
{
    public class FrequenciaService : IFrequenciaService
    {
        private readonly ApiIntegracaoDbContext _context;
        private readonly ICettproApiClient _cettproClient;
        private readonly IFileParser _fileParser;
        private readonly IEmailUpdater _emailUpdater;
        private readonly IAttendanceProcessor _attendanceProcessor;
        private readonly ILogger<FrequenciaService> _logger;

        public FrequenciaService(
            ApiIntegracaoDbContext context,
            ICettproApiClient cettproClient,
            IFileParser fileParser,
            IEmailUpdater emailUpdater,
            IAttendanceProcessor attendanceProcessor,
            ILogger<FrequenciaService> logger)
        {
            _context = context;
            _cettproClient = cettproClient;
            _fileParser = fileParser;
            _emailUpdater = emailUpdater;
            _attendanceProcessor = attendanceProcessor;
            _logger = logger;
        }

        public async Task<FrequenciaResponseDto> ProcessarFrequenciaAsync(
            FrequenciaRequestDto request,
            IFormFile arquivo)
        {
            // Validações iniciais
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (arquivo == null || arquivo.Length == 0)
                throw new ArgumentException("Arquivo de frequência é obrigatório");

            // Iniciar transação para garantir consistência
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation(
                    "Iniciando processamento de frequência para turma {TurmaId} em {Data}",
                    request.IdTurma, request.DataAula);

                // Etapa 1: Atualizar e-mails institucionais
                var emailUpdateResult = await _emailUpdater.UpdateInstitutionalEmailsAsync(
                    request.AlunosParaAtualizar);

                _logger.LogInformation(
                    "E-mails atualizados: {Atualizados} de {Total}",
                    emailUpdateResult.Atualizados, emailUpdateResult.TotalProcessados);

                // Etapa 2: Processar arquivo de frequência
                var participantes = await _fileParser.ParseAttendanceFileAsync(arquivo);

                _logger.LogInformation(
                    "{Count} participantes extraídos do arquivo",
                    participantes.Count);

                // Etapa 3: Buscar matrículas da turma
                var matriculas = await _context.Matriculas
                    .Include(m => m.Aluno)
                    .Where(m => m.TurmaId == request.IdTurma)
                    .ToListAsync();

                if (!matriculas.Any())
                {
                    throw new InvalidOperationException(
                        $"Nenhuma matrícula encontrada para a turma {request.IdTurma}");
                }

                _logger.LogInformation(
                    "{Count} matrículas encontradas para a turma",
                    matriculas.Count);

                // Etapa 4: Processar presenças
                var attendanceResult = await _attendanceProcessor.ProcessAttendanceAsync(
                    request.IdTurma,
                    request.DataAula,
                    participantes,
                    matriculas,
                    request.Justificativas);

                // Etapa 5: Preparar dados para CETTPRO
                var frequenciaParaCettpro = PrepararFrequenciaParaCettpro(
                    request,
                    attendanceResult,
                    matriculas);

                // Etapa 6: Enviar para CETTPRO
                await EnviarFrequenciaParaCettpro(frequenciaParaCettpro);

                // Etapa 7: Registrar processamento no banco
                await RegistrarProcessamentoFrequencia(request, attendanceResult);

                // Confirmar transação
                await transaction.CommitAsync();

                // Preparar resposta
                var response = new FrequenciaResponseDto
                {
                    Status = "Sucesso",
                    Mensagem = $"Frequência para a aula de {request.DataAula:yyyy-MM-dd} processada e enviada.",
                    Inconsistencias = new List<string>()
                };

                // Adicionar inconsistências se houver
                if (attendanceResult.EmailsNaoIdentificados.Any())
                {
                    response.Inconsistencias.AddRange(attendanceResult.EmailsNaoIdentificados
                        .Select(e => $"E-mail não identificado: {e}"));
                }

                if (emailUpdateResult.CpfsNaoEncontrados.Any())
                {
                    response.Inconsistencias.AddRange(emailUpdateResult.CpfsNaoEncontrados
                        .Select(cpf => $"CPF não encontrado para atualização: {cpf}"));
                }

                _logger.LogInformation(
                    "Processamento de frequência concluído com sucesso. " +
                    "{Presentes} presentes, {Ausentes} ausentes, {Justificados} justificados",
                    attendanceResult.TotalPresentes,
                    attendanceResult.TotalAusentes,
                    attendanceResult.TotalJustificados);

                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Erro ao processar frequência");
                throw;
            }
        }

        private object PrepararFrequenciaParaCettpro(
            FrequenciaRequestDto request,
            AttendanceProcessingResult attendanceResult,
            List<Matricula> matriculas)
        {
            var matriculasDict = matriculas.ToDictionary(m => m.Id);

            var presencas = attendanceResult.Presencas.Select(p =>
            {
                var matricula = matriculasDict[p.MatriculaId];
                return new
                {
                    externalId = matricula.IdCettpro,
                    presente = p.Presente,
                    justificada = p.Justificada,
                    justificativa = p.Justificativa
                };
            }).ToList();

            return new
            {
                idTurma = request.IdTurma,
                dataPresenca = request.DataAula,
                nrAula = CalcularNumeroAula(request.IdTurma, request.DataAula),
                presencas = presencas
            };
        }

        private int CalcularNumeroAula(Guid turmaId, DateTime dataAula)
        {
            // Buscar número da aula baseado na sequência de aulas geradas
            var numeroAula = _context.AulasGeradas
                .Where(a => a.TurmaId == turmaId && a.DataAula <= dataAula)
                .OrderBy(a => a.DataAula)
                .Count();

            return numeroAula > 0 ? numeroAula : 1;
        }

        private async Task EnviarFrequenciaParaCettpro(object frequenciaData)
        {
            try
            {
                _logger.LogInformation("Enviando frequência para CETTPRO...");

                var response = await _cettproClient.PostAsync<object>(
                    "api/v1/Frequencia",
                    frequenciaData);

                _logger.LogInformation("Frequência enviada com sucesso para CETTPRO");
            }
            catch (CettproApiException ex)
            {
                _logger.LogError(ex, "Erro ao enviar frequência para CETTPRO");
                throw new InvalidOperationException(
                    "Erro ao enviar frequência para sistema CETTPRO", ex);
            }
        }

        private async Task RegistrarProcessamentoFrequencia(
            FrequenciaRequestDto request,
            AttendanceProcessingResult result)
        {
            var registro = new FrequenciaProcessada
            {
                TurmaId = request.IdTurma,
                DataAula = request.DataAula,
                TotalPresentes = result.TotalPresentes,
                TotalAusentes = result.TotalAusentes,
                TotalJustificados = result.TotalJustificados,
                ProcessadoEm = DateTime.UtcNow,
                EmailsNaoIdentificados = string.Join(";", result.EmailsNaoIdentificados),
                Sucesso = true
            };

            _context.FrequenciasProcessadas.Add(registro);
            await _context.SaveChangesAsync();
        }
    }
}