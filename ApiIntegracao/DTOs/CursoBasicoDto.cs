namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO básico para curso (usado em relacionamentos)
    /// </summary>
    public class CursoBasicoDto
    {
        /// <summary>
        /// ID do curso
        /// </summary>
        public Guid IdCurso { get; set; }

        /// <summary>
        /// Nome do curso
        /// </summary>
        public string NomeCurso { get; set; } = string.Empty;

        /// <summary>
        /// Carga horária do curso
        /// </summary>
        public string? CargaHoraria { get; set; }

        /// <summary>
        /// Código do curso no Portal FAT
        /// </summary>
        public Guid? IdPortalFat { get; set; }
    }
}
