using ApiIntegracao.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiIntegracao.Models
{
    /// <summary>
    /// Representa uma matrícula de aluno em uma turma
    /// </summary>
    [Table("Matriculas")]
    public class Matricula : AuditableEntity
    {
        /// <summary>
        /// ID único da matrícula na CETTPRO
        /// </summary>
        [Required]
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// ID do aluno matriculado
        /// </summary>
        [Required]
        public Guid AlunoId { get; set; }

        /// <summary>
        /// ID da turma
        /// </summary>
        [Required]
        public Guid TurmaId { get; set; }

        /// <summary>
        /// Status da matrícula
        /// 731890001: Aberta para Inscrições
        /// 731890002: Em Construção
        /// 731890004: Em Execução
        /// 2: Finalizada
        /// </summary>
        [Required]
        public int Status { get; set; }

        /// <summary>
        /// Data da matrícula
        /// </summary>
        public DateTime? DataMatricula { get; set; }

        /// <summary>
        /// Observações sobre a matrícula
        /// </summary>
        [MaxLength(500)]
        public string? Observacoes { get; set; }

        // Navegação
        /// <summary>
        /// Aluno matriculado
        /// </summary>
        [ForeignKey("AlunoId")]
        public virtual Aluno Aluno { get; set; } = null!;

        /// <summary>
        /// Turma da matrícula
        /// </summary>
        [ForeignKey("TurmaId")]
        public virtual Turma Turma { get; set; } = null!;
    }
}
