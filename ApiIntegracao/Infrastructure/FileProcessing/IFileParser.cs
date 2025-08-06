// Infrastructure/FileProcessing/IFileParser.cs
namespace ApiIntegracao.Infrastructure.FileProcessing
{
    public interface IFileParser
    {
        Task<List<ParticipanteArquivoDto>> ParseAttendanceFileAsync(IFormFile file);
        bool IsValidFile(IFormFile file);
    }

    public class ParticipanteArquivoDto
    {
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? EntradaAula { get; set; }
        public DateTime? SaidaAula { get; set; }
        public int DuracaoMinutos { get; set; }
    }
}