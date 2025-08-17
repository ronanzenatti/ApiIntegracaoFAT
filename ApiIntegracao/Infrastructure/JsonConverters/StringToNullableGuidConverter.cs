using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiIntegracao.Infrastructure.JsonConverters
{
    /// <summary>
    /// Converte um valor string para um Guid anulável (Guid?).
    /// Se a string for nula, vazia ou inválida, retorna null.
    /// Necessário para lidar com o campo 'modalidadeId' da API da CETTPRO,
    /// que pode vir como uma string vazia em vez de null.
    /// </summary>
    public class StringToNullableGuidConverter : JsonConverter<Guid?>
    {
        public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? guidString = reader.GetString();
                if (Guid.TryParse(guidString, out Guid guid))
                {
                    return guid;
                }
            }
            // Se não for uma string ou se o TryParse falhar (incluindo strings vazias), retorna null.
            return null;
        }

        public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
        {
            // A escrita não é estritamente necessária para a desserialização, mas é uma boa prática implementá-la.
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("D"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}