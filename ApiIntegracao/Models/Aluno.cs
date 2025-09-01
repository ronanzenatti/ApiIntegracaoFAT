using ApiIntegracao.Models.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiIntegracao.Models
{
    [Table("Alunos")]
    public partial class Aluno : AuditableEntity
    {
        public Guid IdCettpro { get; set; }
        public required string Nome { get; set; }
        public required string Cpf { get; set; }
        public string? Rg { get; set; }
        public DateTime DataNascimento { get; set; }
        public string? Email { get; set; }

        /// <summary>
        /// Nome social do aluno
        /// </summary>
        [MaxLength(200)]
        public string? NomeSocial { get; set; }

        /// <summary>
        /// Nome do pai
        /// </summary>
        [MaxLength(200)]
        public string? NomePai { get; set; }

        /// <summary>
        /// Nome da mãe
        /// </summary>
        [MaxLength(200)]
        public string? NomeMae { get; set; }

        /// <summary>
        /// Número da CNH
        /// </summary>
        [MaxLength(20)]
        public string? Cnh { get; set; }

        /// <summary>
        /// ID do município (referência CETTPRO)
        /// </summary>
        public Guid? MunicipioId { get; set; }

        /// <summary>
        /// Tipo de PNE (Pessoa com Necessidades Especiais)
        /// </summary>
        public int TipoPNE { get; set; }

        /// <summary>
        /// Data de nascimento
        /// </summary>
        public DateTime? DataNascimento { get; set; }

        /// <summary>
        /// Gênero
        /// 100000000: Masculino
        /// 100000001: Feminino
        /// </summary>
        public int Genero { get; set; }

        /// <summary>
        /// Sexo biológico
        /// 1: Masculino
        /// 2: Feminino
        /// </summary>
        public int Sexo { get; set; }

        /// <summary>
        /// Nacionalidade
        /// </summary>
        [MaxLength(50)]
        public string? Nacionalidade { get; set; }

        /// <summary>
        /// Estado civil
        /// 1: Solteiro
        /// 2: Casado
        /// etc.
        /// </summary>
        public int EstadoCivil { get; set; }

        /// <summary>
        /// Raça/Cor
        /// 731890000: Branca
        /// 731890001: Preta
        /// 731890002: Parda
        /// etc.
        /// </summary>
        public int Raca { get; set; }

        /// <summary>
        /// Email institucional (usado para frequência)
        /// </summary>
        [MaxLength(255)]
        public string? EmailInstitucional { get; set; }

        // Navegação
        /// <summary>
        /// Matrículas do aluno
        /// </summary>
        public virtual ICollection<Matricula> Matriculas { get; set; } = new HashSet<Matricula>();
    }

}
