namespace ApiIntegracao.DTOs
{
    public class AulaGeradaDto
    {
        public DateTime DataAula { get; set; }
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFim { get; set; }
        public int DiaSemana { get; set; }
        public string NomeDiaSemana { get; set; } = string.Empty;
    }
}
