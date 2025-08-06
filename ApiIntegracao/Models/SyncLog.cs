using ApiIntegracao.Models.Base;

namespace ApiIntegracao.Models
{
    public class SyncLog : AuditableEntity
    {
        public string TipoEntidade { get; set; }
        public string Operacao { get; set; }
        public int QuantidadeProcessada { get; set; }
        public bool Sucesso { get; set; }
        public string? ErroDetalhes { get; set; }
        public DateTime InicioProcessamento { get; set; }
        public DateTime? FimProcessamento { get; set; }
    }
}
