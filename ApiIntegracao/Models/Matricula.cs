using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class Matricula : AuditableEntity
    {
        public Guid IdCettpro { get; set; }
        public Guid AlunoId { get; set; }
        public Guid TurmaId { get; set; }
        public int Status { get; set; }
        public DateTime? DataMatricula { get; set; }

        public virtual Aluno Aluno { get; set; } = null!;
        public virtual Turma Turma { get; set; } = null!;
    }
}
