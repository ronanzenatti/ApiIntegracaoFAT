namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// Enum para os formatos de exportação de arquivos.
    /// </summary>
    public enum FormatoExportacao
    {
        PDF,
        XLSX,
        CSV
    }

    /// <summary>
    /// Resultado da validação de horários.
    /// </summary>
    public class ValidacaoHorariosResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> Erros { get; set; } = new();
    }

    /// <summary>
    /// Resultado do cálculo de horas-aula de um cronograma.
    /// </summary>
    public class CalculoHorasResult
    {
        public int TotalAulas { get; set; }
        public double TotalHoras { get; set; }
    }

    /// <summary>
    /// Resultado da exportação de um cronograma.
    /// </summary>
    public class ExportacaoCronogramaResult
    {
        public byte[] Arquivo { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = string.Empty;
        public string NomeArquivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado do processamento de geração de cronogramas em lote.
    /// </summary>
    public class CronogramaLoteResult
    {
        public int TotalProcessado { get; set; }
        public int TotalSucesso { get; set; }
        public int TotalFalhas { get; set; }
        public List<string> Erros { get; set; } = new();
    }

    /// <summary>
    /// Resultado da sincronização de cronogramas com o CETTPRO.
    /// </summary>
    public class SincronizacaoCronogramaResult
    {
        public bool Sucesso { get; set; }
        public int TotalSincronizado { get; set; }
        public string? Mensagem { get; set; }
    }
}