using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    // Modelo para registro de frequência processada
    public class FrequenciaProcessada : AuditableEntity
    {
        public Guid TurmaId { get; set; }
        public DateTime DataAula { get; set; }
        public int TotalPresentes { get; set; }
        public int TotalAusentes { get; set; }
        public int TotalJustificados { get; set; }
        public DateTime ProcessadoEm { get; set; }
        public string? EmailsNaoIdentificados { get; set; }
        public bool Sucesso { get; set; }

        public virtual Turma Turma { get; set; } = null!;
    }
}
