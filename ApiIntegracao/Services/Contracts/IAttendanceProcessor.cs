// Services/Contracts/IAttendanceProcessor.cs
using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.Frequencia;
using ApiIntegracao.Infrastructure.FileProcessing;
using ApiIntegracao.Models;

namespace ApiIntegracao.Services.Contracts
{
    public interface IAttendanceProcessor
    {
        Task<AttendanceProcessingResult> ProcessAttendanceAsync(
            Guid turmaId,
            DateTime dataAula,
            List<ParticipanteArquivoDto> participantes,
            List<Matricula> matriculas,
            List<JustificativaDto>? justificativas = null);
    }

    public class AttendanceProcessingResult
    {
        public List<PresencaDto> Presencas { get; set; } = new();
        public List<string> EmailsNaoIdentificados { get; set; } = new();
        public List<string> ProfessoresIdentificados { get; set; } = new();
        public int TotalPresentes { get; set; }
        public int TotalAusentes { get; set; }
        public int TotalJustificados { get; set; }
    }

    public class PresencaDto
    {
        public Guid MatriculaId { get; set; }
        public string NomeAluno { get; set; } = string.Empty;
        public bool Presente { get; set; }
        public bool Justificada { get; set; }
        public string? Justificativa { get; set; }
    }
}