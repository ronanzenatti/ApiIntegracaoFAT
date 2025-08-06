using ApiIntegracao.Models.Base;

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
        public Guid? DisciplinaIdPortalFat { get; set; }
        public string? DisciplinaNomePortalFat { get; set; }
    }
}
