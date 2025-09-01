using ApiIntegracao.DTOs.Turma;

namespace ApiIntegracao.DTOs.Curso
{
    public class CursoQualificacaoDto
    {
        public Guid IdCurso { get; set; }
        public string NomeCurso { get; set; }
        public bool Ativo { get; set; }
        public List<TurmaQualificacaoDto> Turmas { get; set; }
    }
}
