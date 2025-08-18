// Infrastructure/FileProcessing/IFileParser.cs
using ApiIntegracao.DTOs;

namespace ApiIntegracao.Infrastructure.FileProcessing
{
    public interface IFileParser
    {
        Task<List<ParticipanteArquivoDto>> ParseAttendanceFileAsync(IFormFile file);
        bool IsValidFile(IFormFile file);
    }

}