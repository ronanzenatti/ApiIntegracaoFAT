namespace ApiIntegracao.DTOs.Responses
{
    /// <summary>
    /// DTO de resposta para resultado de sincronização
    /// </summary>
    public class SyncResultResponseDto
    {
        /// <summary>
        /// Indica se a sincronização foi bem-sucedida
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Total de registros processados
        /// </summary>
        public int TotalProcessed { get; set; }

        /// <summary>
        /// Número de registros inseridos
        /// </summary>
        public int Inserted { get; set; }

        /// <summary>
        /// Número de registros atualizados
        /// </summary>
        public int Updated { get; set; }

        /// <summary>
        /// Número de registros removidos (soft delete)
        /// </summary>
        public int Deleted { get; set; }

        /// <summary>
        /// Lista de erros encontrados
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Hora de início da sincronização
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Hora de fim da sincronização
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Duração da sincronização
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
