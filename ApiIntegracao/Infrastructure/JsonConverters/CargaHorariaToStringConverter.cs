using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiIntegracao.Infrastructure.JsonConverters
{
    /// <summary>
    /// Converte um valor JSON (seja número ou string) para uma string.
    /// Isto é necessário para lidar com a inconsistência da API da CETTPRO no campo 'cargaHoraria'.
    /// </summary>
    public class CargaHorariaToStringConverter : JsonConverter<string?>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Se o token JSON for um número, leia-o e converta para string.
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out int intValue))
                {
                    return intValue.ToString();
                }
                if (reader.TryGetDouble(out double doubleValue))
                {
                    return doubleValue.ToString();
                }
            }

            // Se o token JSON já for uma string, retorne-o diretamente.
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            // Se for nulo ou outro tipo, retorne uma string vazia como fallback seguro.
            return string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
        {
            // A escrita não é necessária para a desserialização, mas é bom implementá-la.
            writer.WriteStringValue(value);
        }
    }
}