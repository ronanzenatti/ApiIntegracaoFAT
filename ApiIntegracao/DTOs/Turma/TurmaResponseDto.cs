using ApiIntegracao.DTOs.Curso;

namespace ApiIntegracao.DTOs.Turma
{
    /// <summary>
    /// DTO de resposta básica para turma
    /// </summary>
    public class TurmaResponseDto
    {
        /// <summary>
        /// ID interno da turma
        /// </summary>
        public Guid IdTurma { get; set; }

        /// <summary>
        /// ID da turma na CETTPRO
        /// </summary>
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// Nome da turma
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Data de início da turma
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término da turma
        /// </summary>
        public DateTime? DataTermino { get; set; }

        /// <summary>
        /// Status da turma (código numérico)
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// ID do curso relacionado
        /// </summary>
        public Guid CursoId { get; set; }

        /// <summary>
        /// Dados básicos do curso
        /// </summary>
        public CursoBasicoDto Curso { get; set; } = new();

        /// <summary>
        /// ID da turma no Portal FAT
        /// </summary>
        public Guid? IdPortalFat { get; set; }

        /// <summary>
        /// ID da disciplina no Portal FAT
        /// </summary>
        public Guid? DisciplinaIdPortalFat { get; set; }

        /// <summary>
        /// Nome da disciplina no Portal FAT
        /// </summary>
        public string? DisciplinaNomePortalFat { get; set; }

        /// <summary>
        /// Data de criação
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
