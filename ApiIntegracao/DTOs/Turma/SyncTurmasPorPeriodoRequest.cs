using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Turma
{
    public class SyncTurmasPorPeriodoRequest
    {
        [Required]
        public DateTime DataInicial { get; set; }
        [Required]
        public DateTime DataFinal { get; set; }
    }
}
