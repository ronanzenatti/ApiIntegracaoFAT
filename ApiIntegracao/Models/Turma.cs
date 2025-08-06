using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class Turma : AuditableEntity
    {
        public Guid IdCettpro { get; set; }
        public string Nome { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime? DataTermino { get; set; }
        public int Status { get; set; }
        public Guid CursoId { get; set; }
        public virtual Curso Curso { get; set; }
        public string? CodigoPortalFat { get; set; }
        public string? DisciplinaCodigoPortalFat { get; set; }
        public string? DisciplinaNomePortalFat { get; set; }
    }
}
