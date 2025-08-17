// Em ApiIntegracao/Infrastructure/FileProcessing/FileParser.cs
using System.Text.Json;

namespace ApiIntegracao.Infrastructure.FileProcessing
{
    public class FileParser : IFileParser
    {
        private readonly ILogger<FileParser> _logger;

        public FileParser(ILogger<FileParser> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<T>> ParseAsync<T>(Stream fileStream, string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".csv" => await ParseCsvAsync<T>(fileStream),
                ".xlsx" or ".xls" => await ParseExcelAsync<T>(fileStream),
                ".json" => await ParseJsonAsync<T>(fileStream),
                _ => throw new NotSupportedException($"Formato de arquivo {extension} não suportado")
            };
        }

        private async Task<IEnumerable<T>> ParseCsvAsync<T>(Stream stream)
        {
            // Implementação do parser CSV
            throw new NotImplementedException("Implementar parser CSV");
        }

        private async Task<IEnumerable<T>> ParseExcelAsync<T>(Stream stream)
        {
            // Implementação do parser Excel
            throw new NotImplementedException("Implementar parser Excel");
        }

        private async Task<IEnumerable<T>> ParseJsonAsync<T>(Stream stream)
        {
            // Implementação do parser JSON
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            return JsonSerializer.Deserialize<IEnumerable<T>>(json) ?? new List<T>();
        }
    }
}