namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO de resposta para dados de um Aluno
    /// </summary>
    public class AlunoResponseDto
    {
        /// <summary>
        /// ID interno do aluno
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID do aluno na CETTPRO
        /// </summary>
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// Nome completo do aluno
        /// </summary>
        public required string Nome { get; set; }

        /// <summary>
        /// Nome social do aluno
        /// </summary>
        public string? NomeSocial { get; set; }

        /// <summary>
        /// CPF do aluno
        /// </summary>
        public required string Cpf { get; set; }

        /// <summary>
        /// RG do aluno
        /// </summary>
        public string? Rg { get; set; }

        /// <summary>
        /// Data de nascimento do aluno
        /// </summary>
        public DateTime DataNascimento { get; set; }

        /// <summary>
        /// Email pessoal do aluno
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// Email institucional do aluno
        /// </summary>
        public string? EmailInstitucional { get; set; }

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