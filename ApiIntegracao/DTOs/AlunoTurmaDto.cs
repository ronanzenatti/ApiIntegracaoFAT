namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO para aluno vinculado a uma turma
    /// </summary>
    public class AlunoTurmaDto
    {
        /// <summary>
        /// ID da matrícula
        /// </summary>
        public Guid IdMatricula { get; set; }

        /// <summary>
        /// ID do aluno
        /// </summary>
        public Guid IdAluno { get; set; }

        /// <summary>
        /// ID do aluno na CETTPRO
        /// </summary>
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// Nome completo do aluno
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Nome social do aluno
        /// </summary>
        public string? NomeSocial { get; set; }

        /// <summary>
        /// CPF do aluno
        /// </summary>
        public string Cpf { get; set; } = string.Empty;

        /// <summary>
        /// Email pessoal do aluno
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Email institucional do aluno
        /// </summary>
        public string? EmailInstitucional { get; set; }

        /// <summary>
        /// Status da matrícula (código numérico)
        /// </summary>
        public int StatusMatricula { get; set; }

        /// <summary>
        /// Descrição do status da matrícula
        /// </summary>
        public string StatusMatriculaDescricao { get; set; } = string.Empty;

        /// <summary>
        /// Data da matrícula
        /// </summary>
        public DateTime? DataMatricula { get; set; }

        /// <summary>
        /// Data de criação do registro
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
