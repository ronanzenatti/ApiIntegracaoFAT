using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs
{
    // DTO genérico para encapsular respostas da API que vêm num formato de objeto
    public class ApiResponseDto<T>
    {
        // Assumimos que o array de dados está numa propriedade chamada "data"
        // Se o nome for diferente na API real, altere o "data" abaixo.
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();

        // Pode adicionar outras propriedades do envelope de resposta aqui, se existirem
        // Ex: [JsonPropertyName("success")]
        //     public bool Success { get; set; }
    }
}