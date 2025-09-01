using ApiIntegracao.DTOs.Aluno;

namespace ApiIntegracao.DTOs.Matricula
{
    /// <summary>
    /// DTO de resposta para matrícula
    /// </summary>
    public class MatriculaResponseDto
    {
        /// <summary>
        /// ID único da matrícula
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID da matrícula na CETTPRO
        /// </summary>
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// Status da matrícula
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// Data da matrícula
        /// </summary>
        public DateTime? DataMatricula { get; set; }

        /// <summary>
        /// Dados do aluno matriculado
        /// </summary>
        public AlunoResponseDto Aluno { get; set; } = null!;
    }
}