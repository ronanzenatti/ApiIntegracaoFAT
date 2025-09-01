namespace ApiIntegracao.DTOs.Unidade
{
    /// <summary>
    /// DTO de resposta para unidade de ensino
    /// </summary>
    public class UnidadeEnsinoResponseDto
    {
        /// <summary>
        /// ID único da unidade de ensino
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID da unidade de ensino na CETTPRO
        /// </summary>
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// Nome da unidade de ensino
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Nome fantasia
        /// </summary>
        public string? NomeFantasia { get; set; }

        /// <summary>
        /// CNPJ da unidade
        /// </summary>
        public string? Cnpj { get; set; }

        /// <summary>
        /// Indica se a unidade está ativa
        /// </summary>
        public bool Ativo { get; set; }

        /// <summary>
        /// Data de criação
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime? UpdatedAt { get; set; }
    }
}
