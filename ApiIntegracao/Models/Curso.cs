using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class Curso : AuditableEntity
    {

        public Guid IdCettpro { get; set; }
        public required string NomeCurso { get; set; }
        public string? CargaHoraria { get; set; }
        public string? Descricao { get; set; }
        public Guid ModalidadeId { get; set; }
        public bool Ativo { get; set; }
        public Guid? IdPortalFat { get; set; }

        // Navegação
        public ICollection<Turma> Turmas { get; set; } = new List<Turma>();
    }
}
