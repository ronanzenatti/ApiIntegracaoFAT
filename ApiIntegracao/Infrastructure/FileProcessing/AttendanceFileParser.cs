// Infrastructure/FileProcessing/AttendanceFileParser.cs
using ApiIntegracao.DTOs;
using CsvHelper;
using CsvHelper.Configuration;
using ExcelDataReader;
using System.Data;
using System.Globalization;
using System.Text;

namespace ApiIntegracao.Infrastructure.FileProcessing
{
    public class AttendanceFileParser : IFileParser
    {
        private readonly ILogger<AttendanceFileParser> _logger;
        private readonly IConfiguration _config;

        public AttendanceFileParser(ILogger<AttendanceFileParser> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;

            // Registrar encoding para Excel
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public bool IsValidFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("Arquivo vazio ou nulo");
                return false;
            }

            // Verificar tamanho máximo
            var maxSizeMB = _config.GetValue<int>("FileProcessing:MaxFileSizeMB", 10);
            var maxSizeBytes = maxSizeMB * 1024 * 1024;

            if (file.Length > maxSizeBytes)
            {
                _logger.LogWarning("Arquivo excede o tamanho máximo de {MaxSize}MB", maxSizeMB);
                return false;
            }

            // Verificar extensão
            var allowedExtensions = _config.GetSection("FileProcessing:AllowedExtensions")
                .Get<string[]>() ?? new[] { ".csv", ".xlsx" };

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
            {
                _logger.LogWarning("Extensão {Extension} não permitida", fileExtension);
                return false;
            }

            return true;
        }

        public async Task<List<ParticipanteArquivoDto>> ParseAttendanceFileAsync(IFormFile file)
        {
            if (!IsValidFile(file))
            {
                throw new ArgumentException("Arquivo inválido");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            _logger.LogInformation("Processando arquivo {FileName} ({Size} bytes)",
                file.FileName, file.Length);

            return extension switch
            {
                ".csv" => await ParseCsvFileAsync(file),
                ".xlsx" => await ParseExcelFileAsync(file),
                _ => throw new NotSupportedException($"Extensão {extension} não suportada")
            };
        }

        private async Task<List<ParticipanteArquivoDto>> ParseCsvFileAsync(IFormFile file)
        {
            var participantes = new List<ParticipanteArquivoDto>();

            using var reader = new StreamReader(file.OpenReadStream());
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                BadDataFound = null, // Ignorar linhas com problemas
                MissingFieldFound = null,
                HeaderValidated = null
            });

            // Registrar mapeamento para diferentes formatos (Teams/Meet)
            csv.Context.RegisterClassMap<TeamsParticipantMap>();

            try
            {
                await foreach (var record in csv.GetRecordsAsync<ParticipanteArquivoDto>())
                {
                    if (!string.IsNullOrWhiteSpace(record.Email))
                    {
                        participantes.Add(record);
                        _logger.LogDebug("Participante encontrado: {Email}", record.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar CSV");

                // Tentar formato alternativo
                participantes = await TryAlternativeCsvFormat(file);
            }

            _logger.LogInformation("Total de {Count} participantes extraídos do CSV", participantes.Count);
            return participantes;
        }

        private Task<List<ParticipanteArquivoDto>> ParseExcelFileAsync(IFormFile file)
        {
            var participantes = new List<ParticipanteArquivoDto>();

            using var stream = file.OpenReadStream();
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
            {
                ConfigureDataTable = _ => new ExcelDataTableConfiguration
                {
                    UseHeaderRow = true
                }
            });

            if (dataSet.Tables.Count == 0)
            {
                throw new InvalidOperationException("Arquivo Excel vazio");
            }

            var table = dataSet.Tables[0];

            // Identificar colunas por diferentes nomes possíveis
            var emailColumnNames = new[] { "Email", "E-mail", "Participante", "User Email", "Email Address" };
            var nameColumnNames = new[] { "Nome", "Name", "Display Name", "Nome Completo", "Full Name" };

            int? emailColumnIndex = null;
            int? nameColumnIndex = null;

            // Encontrar índices das colunas
            for (int i = 0; i < table.Columns.Count; i++)
            {
                var columnName = table.Columns[i].ColumnName;

                if (emailColumnIndex == null && emailColumnNames.Any(n =>
                    columnName.Contains(n, StringComparison.OrdinalIgnoreCase)))
                {
                    emailColumnIndex = i;
                }

                if (nameColumnIndex == null && nameColumnNames.Any(n =>
                    columnName.Contains(n, StringComparison.OrdinalIgnoreCase)))
                {
                    nameColumnIndex = i;
                }
            }

            if (emailColumnIndex == null)
            {
                throw new InvalidOperationException("Coluna de e-mail não encontrada no Excel");
            }

            // Processar linhas
            foreach (DataRow row in table.Rows)
            {
                var email = row[emailColumnIndex.Value]?.ToString()?.Trim();

                if (!string.IsNullOrWhiteSpace(email) && email.Contains('@'))
                {
                    var participante = new ParticipanteArquivoDto
                    {
                        Email = email,
                        Nome = nameColumnIndex.HasValue ?
                            row[nameColumnIndex.Value]?.ToString()?.Trim() ?? "" : ""
                    };

                    participantes.Add(participante);
                }
            }

            _logger.LogInformation("Total de {Count} participantes extraídos do Excel", participantes.Count);
            return Task.FromResult(participantes);
        }

        private async Task<List<ParticipanteArquivoDto>> TryAlternativeCsvFormat(IFormFile file)
        {
            // Implementar tentativa com formato alternativo
            // Por exemplo, separador diferente ou encoding diferente

            var participantes = new List<ParticipanteArquivoDto>();

            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            var lines = new List<string>();
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.Add(line);
            }

            // Processar manualmente se necessário
            // ...

            return participantes;
        }
    }

    // Mapeamento para CSV do Teams
    public class TeamsParticipantMap : ClassMap<ParticipanteArquivoDto>
    {
        public TeamsParticipantMap()
        {
            Map(m => m.Nome).Name("Full Name", "Nome", "Display Name");
            Map(m => m.Email).Name("User Email", "Email", "E-mail", "Participante");
            Map(m => m.EntradaAula).Name("Join Time", "Hora de Entrada").Optional();
            Map(m => m.SaidaAula).Name("Leave Time", "Hora de Saída").Optional();
        }
    }
}