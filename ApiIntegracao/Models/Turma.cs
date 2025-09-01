using ApiIntegracao.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiIntegracao.Models
{
    public class Turma : AuditableEntity
    {
        public Guid IdCettpro { get; set; }
        public required string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataTermino { get; set; }
        public int Status { get; set; }
        public Guid CursoId { get; set; }
        public virtual required Curso Curso { get; set; }
        public Guid? IdPortalFat { get; set; }

        /// <summary>
        /// ID da unidade de ensino
        /// </summary>
        public Guid? UnidadeEnsinoId { get; set; }

        /// <summary>
        /// Código da disciplina no Portal FAT
        /// </summary>
        [MaxLength(50)]
        public Guid? DisciplinaIdPortalFat { get; set; }

        /// <summary>
        /// Nome da disciplina no Portal FAT
        /// </summary>
        [MaxLength(200)]
        public string? DisciplinaNomePortalFat { get; set; }

        // Navegação
        /// <summary>
        /// Unidade de ensino da turma
        /// </summary>
        [ForeignKey("UnidadeEnsinoId")]
        public virtual UnidadeEnsino? UnidadeEnsino { get; set; }

        /// <summary>
        /// Matrículas da turma
        /// </summary>
        public virtual ICollection<Matricula> Matriculas { get; set; } = new HashSet<Matricula>();
    }
}
