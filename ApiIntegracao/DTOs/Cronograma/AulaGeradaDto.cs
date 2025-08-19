namespace ApiIntegracao.DTOs.Cronograma
{
    /// <summary>
    /// DTO para representar uma aula gerada no cronograma
    /// </summary>
    public class AulaGeradaDto
    {
        /// <summary>
        /// ID da aula
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Data da aula
        /// </summary>
        public DateTime DataAula { get; set; }

        /// <summary>
        /// Hora de início
        /// </summary>
        public TimeSpan HoraInicio { get; set; }

        /// <summary>
        /// Hora de término
        /// </summary>
        public TimeSpan HoraFim { get; set; }

        /// <summary>
        /// Duração em horas
        /// </summary>
        public double DuracaoHoras => (HoraFim - HoraInicio).TotalHours;

        /// <summary>
        /// Dia da semana (0=Domingo, 6=Sábado)
        /// </summary>
        public int DiaSemana { get; set; }

        /// <summary>
        /// Nome do dia da semana
        /// </summary>
        public string NomeDiaSemana { get; set; } = string.Empty;

        /// <summary>
        /// Assunto/Conteúdo da aula
        /// </summary>
        public string? Assunto { get; set; }

        /// <summary>
        /// Descrição detalhada
        /// </summary>
        public string? Descricao { get; set; }

        /// <summary>
        /// Sala de aula
        /// </summary>
        public string? Sala { get; set; }

        /// <summary>
        /// Professor responsável
        /// </summary>
        public string? Professor { get; set; }

        /// <summary>
        /// Status da aula (Agendada, Realizada, Cancelada, Remarcada)
        /// </summary>
        public string Status { get; set; } = "Agendada";

        /// <summary>
        /// Indica se a aula foi realizada
        /// </summary>
        public bool Realizada { get; set; } = false;

        /// <summary>
        /// Data/hora de realização (se diferente da programada)
        /// </summary>
        public DateTime? DataRealizacao { get; set; }

        /// <summary>
        /// Indica se é aula online
        /// </summary>
        public bool IsOnline { get; set; } = false;

        /// <summary>
        /// Link para aula online
        /// </summary>
        public string? LinkAulaOnline { get; set; }

        /// <summary>
        /// Total de alunos presentes
        /// </summary>
        public int? TotalPresentes { get; set; }

        /// <summary>
        /// Total de alunos ausentes
        /// </summary>
        public int? TotalAusentes { get; set; }

        /// <summary>
        /// Observações da aula
        /// </summary>
        public string? Observacoes { get; set; }

        /// <summary>
        /// Indica se é feriado
        /// </summary>
        public bool IsFeriado { get; set; } = false;

        /// <summary>
        /// Nome do feriado (se aplicável)
        /// </summary>
        public string? NomeFeriado { get; set; }

        /// <summary>
        /// Número sequencial da aula
        /// </summary>
        public int NumeroAula { get; set; }

        /// <summary>
        /// Data de criação do registro
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime? DataAtualizacao { get; set; }
    }
}
