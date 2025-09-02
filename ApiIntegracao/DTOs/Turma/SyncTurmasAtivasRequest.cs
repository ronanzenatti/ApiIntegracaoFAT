using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Turma
{
    /// <summary>
    /// Representa a requisição para sincronizar turmas ativas com base em um período em dias.
    /// </summary>
    public class SyncTurmasAtivasRequest
    {
        /// <summary>
        /// O número de dias no passado a serem considerados para a sincronização.
        /// O valor padrão é 30.
        /// </summary>
        [Range(1, 365, ErrorMessage = "O valor de dias deve estar entre 1 e 365.")]
        public int Dias { get; set; } = 30;
    }
}