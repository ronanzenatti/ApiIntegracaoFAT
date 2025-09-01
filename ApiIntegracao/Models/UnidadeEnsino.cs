using ApiIntegracao.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiIntegracao.Models
{
    /// <summary>
    /// Representa uma unidade de ensino
    /// </summary>
    [Table("UnidadesEnsino")]
    public class UnidadeEnsino : AuditableEntity
    {
        /// <summary>
        /// ID único da unidade de ensino na CETTPRO
        /// </summary>
        [Required]
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// Nome da unidade de ensino
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Nome fantasia
        /// </summary>
        [MaxLength(300)]
        public string? NomeFantasia { get; set; }

        /// <summary>
        /// CNPJ da unidade
        /// </summary>
        [MaxLength(20)]
        public string? Cnpj { get; set; }

        /// <summary>
        /// ID do município
        /// </summary>
        public Guid? MunicipioId { get; set; }

        /// <summary>
        /// Indica se a unidade está ativa
        /// </summary>
        [Required]
        public bool Ativo { get; set; } = true;

        // Navegação
        /// <summary>
        /// Turmas da unidade de ensino
        /// </summary>
        public virtual ICollection<Turma> Turmas { get; set; } = new HashSet<Turma>();
    }
}