using ApiIntegracao.DTOs.Curso;
using ApiIntegracao.DTOs.Matricula;

namespace ApiIntegracao.DTOs.Turma
{
    /// <summary>
    /// DTO de resposta com detalhes completos de uma turma
    /// </summary>
    public class TurmaDetalhadaResponseDto : TurmaResponseDto
    {
        /// <summary>
        /// Código da disciplina no Portal FAT
        /// </summary>
        public string? DisciplinaCodigoPortalFat { get; set; }

        /// <summary>
        /// Nome da disciplina no Portal FAT
        /// </summary>
        public string? DisciplinaNomePortalFat { get; set; }

        /// <summary>
        /// Curso da turma
        /// </summary>
        public CursoResponseDto? Curso { get; set; }

        /// <summary>
        /// Unidade de ensino da turma
        /// </summary>
        public UnidadeEnsinoResponseDto? UnidadeEnsino { get; set; }

        /// <summary>
        /// Matrículas da turma (opcional)
        /// </summary>
        public List<MatriculaResponseDto>? Matriculas { get; set; }
    }
}
