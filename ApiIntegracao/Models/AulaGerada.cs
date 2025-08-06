using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class AulaGerada : AuditableEntity
    {
        public Guid TurmaId { get; set; }
        public DateTime DataAula { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public string Assunto { get; set; }
        public string Descricao { get; set; }
        public int DiaSemana { get; set; }

        public virtual Turma Turma { get; set; }
    }
}
