// Services/Implementations/EmailUpdater.cs
using ApiIntegracao.Data;
using ApiIntegracao.DTOs.Aluno;
using ApiIntegracao.Services.Contracts;
using Microsoft.EntityFrameworkCore;

namespace ApiIntegracao.Services.Implementations
{
    public class EmailUpdater : IEmailUpdater
    {
        private readonly ApiIntegracaoDbContext _context;
        private readonly ILogger<EmailUpdater> _logger;

        public EmailUpdater(ApiIntegracaoDbContext context, ILogger<EmailUpdater> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<EmailUpdateResult> UpdateInstitutionalEmailsAsync(
            List<AlunoEmailDto> alunosParaAtualizar)
        {
            var result = new EmailUpdateResult
            {
                TotalProcessados = alunosParaAtualizar.Count
            };

            if (!alunosParaAtualizar.Any())
            {
                _logger.LogInformation("Nenhum e-mail para atualizar");
                return result;
            }

            _logger.LogInformation("Atualizando {Count} e-mails institucionais", alunosParaAtualizar.Count);

            // Agrupar por CPF para evitar duplicatas
            var alunosPorCpf = alunosParaAtualizar
                .GroupBy(a => a.Cpf.Trim())
                .ToDictionary(g => g.Key, g => g.First().EmailInstitucional);

            // Buscar todos os alunos de uma vez para melhor performance
            var cpfs = alunosPorCpf.Keys.ToList();
            var alunos = await _context.Alunos
                .Where(a => cpfs.Contains(a.Cpf))
                .ToListAsync();

            var alunosDictionary = alunos.ToDictionary(a => a.Cpf);

            foreach (var (cpf, emailInstitucional) in alunosPorCpf)
            {
                if (alunosDictionary.TryGetValue(cpf, out var aluno))
                {
                    var emailAnterior = aluno.EmailInstitucional;

                    if (aluno.EmailInstitucional != emailInstitucional)
                    {
                        aluno.EmailInstitucional = emailInstitucional;
                        result.Atualizados++;

                        _logger.LogDebug(
                            "E-mail institucional atualizado para aluno {Nome}: {EmailAnterior} -> {EmailNovo}",
                            aluno.Nome, emailAnterior, emailInstitucional);
                    }
                }
                else
                {
                    result.NaoEncontrados++;
                    result.CpfsNaoEncontrados.Add(cpf);

                    _logger.LogWarning("Aluno com CPF {Cpf} não encontrado no banco", cpf);
                }
            }

            if (result.Atualizados > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation(
                    "Atualização de e-mails concluída: {Atualizados} atualizados, {NaoEncontrados} não encontrados",
                    result.Atualizados, result.NaoEncontrados);
            }

            return result;
        }
    }
}