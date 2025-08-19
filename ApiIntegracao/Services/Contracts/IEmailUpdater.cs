// Services/Contracts/IEmailUpdater.cs
using ApiIntegracao.DTOs.Aluno;

namespace ApiIntegracao.Services.Contracts
{
    public interface IEmailUpdater
    {
        Task<EmailUpdateResult> UpdateInstitutionalEmailsAsync(List<AlunoEmailDto> alunosParaAtualizar);
    }

    public class EmailUpdateResult
    {
        public int TotalProcessados { get; set; }
        public int Atualizados { get; set; }
        public int NaoEncontrados { get; set; }
        public List<string> CpfsNaoEncontrados { get; set; } = new();
    }
}