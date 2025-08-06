using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class Curso : AuditableEntity
    {
        public Guid IdCettpro { get; set; }
        public string NomeCurso { get; set; }
        public string CargaHoraria { get; set; }
        public string Descricao { get; set; }
        public Guid ModalidadeId { get; set; }
        public bool Ativo { get; set; }
        public string? CodigoPortalFat { get; set; }
    }
}
