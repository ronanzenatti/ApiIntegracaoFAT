namespace ApiIntegracao.DTOs.Curso
{
    /// <summary>
    /// DTO de resposta para curso
    /// </summary>
    public class CursoResponseDto
    {
        /// <summary>
        /// ID interno do curso
        /// </summary>
        public Guid IdCurso { get; set; }

        /// <summary>
        /// ID do curso na CETTPRO
        /// </summary>
        public Guid IdCettpro { get; set; }

        /// <summary>
        /// Nome do curso
        /// </summary>
        public string NomeCurso { get; set; } = string.Empty;

        /// <summary>
        /// Carga horária do curso
        /// </summary>
        public string? CargaHoraria { get; set; }

        /// <summary>
        /// Descrição do curso
        /// </summary>
        public string? Descricao { get; set; }

        /// <summary>
        /// ID da modalidade
        /// </summary>
        public Guid? ModalidadeId { get; set; }

        /// <summary>
        /// Indica se o curso está ativo
        /// </summary>
        public bool Ativo { get; set; }

        /// <summary>
        /// Código do curso no Portal FAT
        /// </summary>
        public Guid? IdPortalFat { get; set; }

        /// <summary>
        /// Data de criação
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}