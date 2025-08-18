namespace ApiIntegracao.DTOs.Responses
{
    /// <summary>
    /// DTO padrão para respostas paginadas
    /// </summary>
    /// <typeparam name="T">Tipo do objeto de dados</typeparam>
    public class PaginatedResponseDto<T>
    {
        /// <summary>
        /// Array de dados da resposta
        /// </summary>
        public List<T> Data { get; set; } = new();

        /// <summary>
        /// Metadados da resposta
        /// </summary>
        public PaginationMetaDto Meta { get; set; } = new();
    }

}
