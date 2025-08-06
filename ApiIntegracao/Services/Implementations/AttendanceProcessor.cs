// Services/Implementations/AttendanceProcessor.cs
using ApiIntegracao.Infrastructure.FileProcessing;
using ApiIntegracao.Models;
using ApiIntegracao.Services.Contracts;

namespace ApiIntegracao.Services.Implementations
{
    public class AttendanceProcessor : IAttendanceProcessor
    {
        private readonly ILogger<AttendanceProcessor> _logger;
        private readonly IConfiguration _config;

        public AttendanceProcessor(ILogger<AttendanceProcessor> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async Task<AttendanceProcessingResult> ProcessAttendanceAsync(
            Guid turmaId,
            DateTime dataAula,
            List<ParticipanteArquivoDto> participantes,
            List<Matricula> matriculas,
            List<JustificativaDto>? justificativas = null)
        {
            var result = new AttendanceProcessingResult();

            _logger.LogInformation(
                "Processando presença para turma {TurmaId} em {Data} com {Participantes} participantes e {Matriculas} matrículas",
                turmaId, dataAula.ToShortDateString(), participantes.Count, matriculas.Count);

            // Normalizar e-mails para comparação
            var emailsNoArquivo = participantes
                .Where(p => !string.IsNullOrWhiteSpace(p.Email))
                .Select(p => p.Email.Trim().ToLowerInvariant())
                .ToHashSet();

            // Criar dicionário de justificativas por CPF
            var justificativasPorCpf = justificativas?
                .ToDictionary(j => j.CpfAluno, j => j.TextoJustificativa)
                ?? new Dictionary<string, string>();

            // Processar cada matrícula
            foreach (var matricula in matriculas)
            {
                var aluno = matricula.Aluno;
                var presenca = new PresencaDto
                {
                    MatriculaId = matricula.Id,
                    NomeAluno = aluno.Nome
                };

                // Verificar presença pelo e-mail institucional
                var emailAluno = aluno.EmailInstitucional?.Trim().ToLowerInvariant();

                if (!string.IsNullOrWhiteSpace(emailAluno))
                {
                    presenca.Presente = emailsNoArquivo.Contains(emailAluno);

                    if (presenca.Presente)
                    {
                        result.TotalPresentes++;
                        _logger.LogDebug("Aluno {Nome} marcado como presente", aluno.Nome);
                    }
                    else
                    {
                        // Verificar se não é professor
                        if (!await IsProfessorAsync(emailAluno))
                        {
                            result.TotalAusentes++;

                            // Verificar justificativa
                            if (justificativasPorCpf.TryGetValue(aluno.Cpf, out var justificativa))
                            {
                                presenca.Justificada = true;
                                presenca.Justificativa = justificativa;
                                result.TotalJustificados++;
                                _logger.LogDebug("Aluno {Nome} tem falta justificada", aluno.Nome);
                            }
                            else
                            {
                                _logger.LogDebug("Aluno {Nome} marcado como ausente", aluno.Nome);
                            }
                        }
                    }
                }
                else
                {
                    // Aluno sem e-mail institucional é marcado como ausente
                    presenca.Presente = false;
                    result.TotalAusentes++;

                    _logger.LogWarning(
                        "Aluno {Nome} sem e-mail institucional cadastrado, marcado como ausente",
                        aluno.Nome);
                }

                result.Presencas.Add(presenca);
            }

            // Identificar e-mails não reconhecidos no arquivo
            var emailsAlunos = matriculas
                .Select(m => m.Aluno.EmailInstitucional?.Trim().ToLowerInvariant())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .ToHashSet();

            foreach (var participante in participantes)
            {
                var emailParticipante = participante.Email?.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(emailParticipante))
                    continue;

                if (!emailsAlunos!.Contains(emailParticipante))
                {
                    if (await IsProfessorAsync(emailParticipante))
                    {
                        result.ProfessoresIdentificados.Add(participante.Email);
                        _logger.LogDebug("Professor identificado: {Email}", participante.Email);
                    }
                    else
                    {
                        result.EmailsNaoIdentificados.Add(participante.Email);
                        _logger.LogWarning("E-mail não identificado no arquivo: {Email}", participante.Email);
                    }
                }
            }

            _logger.LogInformation(
                "Processamento concluído: {Presentes} presentes, {Ausentes} ausentes, {Justificados} justificados",
                result.TotalPresentes, result.TotalAusentes, result.TotalJustificados);

            return result;
        }

        private async Task<bool> IsProfessorAsync(string email)
        {
            // Implementar lógica para identificar professores
            // Pode ser por domínio, lista em banco, ou configuração

            var dominiosProfessores = _config.GetSection("Attendance:TeacherDomains")
                .Get<string[]>() ?? new[] { "@professor.", "@docente.", "@teacher." };

            return await Task.FromResult(
                dominiosProfessores.Any(d => email.Contains(d, StringComparison.OrdinalIgnoreCase)));
        }
    }
}