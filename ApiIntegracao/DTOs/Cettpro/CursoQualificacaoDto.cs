using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs.Cettpro
{
    /// <summary>
    /// DTO para retorno do endpoint CursoQualificacao da CETTPRO
    /// </summary>
    public class CursoQualificacaoDto
    {
        [JsonPropertyName("idCurso")]
        public Guid IdCurso { get; set; }

        [JsonPropertyName("nomeCurso")]
        public string NomeCurso { get; set; } = string.Empty;

        [JsonPropertyName("cargaHoraria")]
        public string CargaHoraria { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }

        [JsonPropertyName("modalidades")]
        public List<ModalidadeDto> Modalidades { get; set; } = new();

        [JsonPropertyName("arcos")]
        public List<ArcoDto> Arcos { get; set; } = new();

        [JsonPropertyName("turmas")]
        public List<TurmaQualificacaoDto> Turmas { get; set; } = new();
    }

    public class ModalidadeDto
    {
        [JsonPropertyName("idModalidade")]
        public Guid IdModalidade { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("sigla")]
        public string Sigla { get; set; } = string.Empty;
    }

    public class ArcoDto
    {
        [JsonPropertyName("idArco")]
        public Guid IdArco { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [JsonPropertyName("urlSlug")]
        public string UrlSlug { get; set; } = string.Empty;

        [JsonPropertyName("ativo")]
        public bool Ativo { get; set; }
    }

    public class TurmaQualificacaoDto
    {
        [JsonPropertyName("idTurma")]
        public Guid IdTurma { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("dataInicio")]
        public DateTime? DataInicio { get; set; }

        [JsonPropertyName("dataTermino")]
        public DateTime? DataTermino { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("unidadedeensino")]
        public List<UnidadeEnsinoDto> UnidadeEnsino { get; set; } = new();
    }

    public class UnidadeEnsinoDto
    {
        [JsonPropertyName("idUnidadeEnsino")]
        public Guid IdUnidadeEnsino { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("nomeFantasia")]
        public string NomeFantasia { get; set; } = string.Empty;

        [JsonPropertyName("cnpj")]
        public string Cnpj { get; set; } = string.Empty;
    }
}
