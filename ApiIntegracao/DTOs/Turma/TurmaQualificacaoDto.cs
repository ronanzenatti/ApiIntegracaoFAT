namespace ApiIntegracao.DTOs.Turma
{
    public class TurmaQualificacaoDto
    {
        public Guid IdTurma { get; set; }
        public string Nome { get; set; }
        public DateTime? DataInicio { get; set; }
        public DateTime? DataTermino { get; set; }
        public int Status { get; set; }
    }
}
