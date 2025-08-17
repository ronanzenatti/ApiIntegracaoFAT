namespace ApiIntegracao.DTOs
{
    public class TurmaResponseDto
    {
        public Guid IdTurma { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataInicio { get; set; }
        public DateTime? DataTermino { get; set; }
        public int Status { get; set; }
        public string StatusDescricao { get; set; } = string.Empty;
        public Guid CursoId { get; set; }
        public string NomeCurso { get; set; } = string.Empty;
    }
}
