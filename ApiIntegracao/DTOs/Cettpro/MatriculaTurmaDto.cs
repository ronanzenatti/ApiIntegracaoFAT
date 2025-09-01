using System.Text.Json.Serialization;

namespace ApiIntegracao.DTOs.Cettpro
{
    /// <summary>
    /// DTO para retorno do endpoint Matricula/Turma da CETTPRO
    /// </summary>
    public class MatriculaTurmaDto
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

        [JsonPropertyName("matriculas")]
        public List<MatriculaDto> Matriculas { get; set; } = new();
    }

    public class MatriculaDto
    {
        [JsonPropertyName("idMatricula")]
        public Guid IdMatricula { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("alunos")]
        public List<AlunoMatriculaDto> Alunos { get; set; } = new();
    }

    public class AlunoMatriculaDto
    {
        [JsonPropertyName("idAluno")]
        public Guid IdAluno { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; } = string.Empty;

        [JsonPropertyName("nomeSocial")]
        public string NomeSocial { get; set; } = string.Empty;

        [JsonPropertyName("nomePai")]
        public string NomePai { get; set; } = string.Empty;

        [JsonPropertyName("nomeMae")]
        public string NomeMae { get; set; } = string.Empty;

        [JsonPropertyName("cnh")]
        public string Cnh { get; set; } = string.Empty;

        [JsonPropertyName("cpf")]
        public string Cpf { get; set; } = string.Empty;

        [JsonPropertyName("rg")]
        public string Rg { get; set; } = string.Empty;

        [JsonPropertyName("municipioId")]
        public Guid? MunicipioId { get; set; }

        [JsonPropertyName("tipoPNE")]
        public int TipoPNE { get; set; }

        [JsonPropertyName("dataNascimento")]
        public string DataNascimento { get; set; } = string.Empty;

        [JsonPropertyName("genero")]
        public int Genero { get; set; }

        [JsonPropertyName("sexo")]
        public int Sexo { get; set; }

        [JsonPropertyName("nacionalidade")]
        public string Nacionalidade { get; set; } = string.Empty;

        [JsonPropertyName("estadoCivil")]
        public int EstadoCivil { get; set; }

        [JsonPropertyName("raca")]
        public int Raca { get; set; }

        [JsonPropertyName("eMail")]
        public string Email { get; set; } = string.Empty;
    }
}
