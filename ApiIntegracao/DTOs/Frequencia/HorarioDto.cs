using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs.Frequencia
{
    /// <summary>
    /// DTO para representar um horário de aula
    /// </summary>
    public class HorarioDto : IValidatableObject
    {
        /// <summary>
        /// Dia da semana (0=Domingo, 1=Segunda, ..., 6=Sábado)
        /// </summary>
        [Required(ErrorMessage = "O dia da semana é obrigatório")]
        [Range(0, 6, ErrorMessage = "O dia da semana deve estar entre 0 (Domingo) e 6 (Sábado)")]
        public int DiaSemana { get; set; }

        /// <summary>
        /// Nome do dia da semana (gerado automaticamente)
        /// </summary>
        public string NomeDiaSemana => ObterNomeDiaSemana();

        /// <summary>
        /// Hora de início da aula (formato HH:mm)
        /// </summary>
        [Required(ErrorMessage = "A hora de início é obrigatória")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$",
            ErrorMessage = "A hora de início deve estar no formato HH:mm (ex: 08:00)")]
        public string Inicio { get; set; } = string.Empty;

        /// <summary>
        /// Hora de término da aula (formato HH:mm)
        /// </summary>
        [Required(ErrorMessage = "A hora de término é obrigatória")]
        [RegularExpression(@"^([0-1]?[0-9]|2[0-3]):[0-5][0-9]$",
            ErrorMessage = "A hora de término deve estar no formato HH:mm (ex: 12:00)")]
        public string Fim { get; set; } = string.Empty;

        /// <summary>
        /// Duração da aula em minutos (calculado)
        /// </summary>
        public int DuracaoMinutos
        {
            get
            {
                if (TimeSpan.TryParse(Inicio, out var horaInicio) &&
                    TimeSpan.TryParse(Fim, out var horaFim))
                {
                    return (int)(horaFim - horaInicio).TotalMinutes;
                }
                return 0;
            }
        }

        /// <summary>
        /// Duração da aula em horas (calculado)
        /// </summary>
        public double DuracaoHoras => DuracaoMinutos / 60.0;

        /// <summary>
        /// Descrição do horário (ex: "Segunda-feira das 08:00 às 12:00")
        /// </summary>
        public string Descricao => $"{NomeDiaSemana} das {Inicio} às {Fim}";

        /// <summary>
        /// Sala de aula (opcional)
        /// </summary>
        [StringLength(50, ErrorMessage = "A sala deve ter no máximo 50 caracteres")]
        public string? Sala { get; set; }

        /// <summary>
        /// Professor responsável (opcional)
        /// </summary>
        [StringLength(100, ErrorMessage = "O nome do professor deve ter no máximo 100 caracteres")]
        public string? Professor { get; set; }

        /// <summary>
        /// Indica se é aula online
        /// </summary>
        public bool IsOnline { get; set; } = false;

        /// <summary>
        /// Link para aula online (se aplicável)
        /// </summary>
        [Url(ErrorMessage = "O link deve ser uma URL válida")]
        [StringLength(500, ErrorMessage = "O link deve ter no máximo 500 caracteres")]
        public string? LinkAulaOnline { get; set; }

        /// <summary>
        /// Observações do horário
        /// </summary>
        [StringLength(200, ErrorMessage = "As observações devem ter no máximo 200 caracteres")]
        public string? Observacoes { get; set; }

        /// <summary>
        /// Validação customizada do horário
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // Validar formato e valores dos horários
            if (!TimeSpan.TryParse(Inicio, out var horaInicio))
            {
                yield return new ValidationResult(
                    "Hora de início inválida",
                    new[] { nameof(Inicio) });
            }

            if (!TimeSpan.TryParse(Fim, out var horaFim))
            {
                yield return new ValidationResult(
                    "Hora de término inválida",
                    new[] { nameof(Fim) });
            }

            // Validar se hora de fim é maior que hora de início
            if (horaFim <= horaInicio)
            {
                yield return new ValidationResult(
                    "A hora de término deve ser posterior à hora de início",
                    new[] { nameof(Fim) });
            }

            // Validar duração mínima (30 minutos)
            var duracao = horaFim - horaInicio;
            if (duracao.TotalMinutes < 30)
            {
                yield return new ValidationResult(
                    "A duração mínima da aula deve ser de 30 minutos",
                    new[] { nameof(Inicio), nameof(Fim) });
            }

            // Validar duração máxima (8 horas)
            if (duracao.TotalHours > 8)
            {
                yield return new ValidationResult(
                    "A duração máxima da aula deve ser de 8 horas",
                    new[] { nameof(Inicio), nameof(Fim) });
            }

            // Validar link se aula é online
            if (IsOnline && string.IsNullOrWhiteSpace(LinkAulaOnline))
            {
                yield return new ValidationResult(
                    "O link é obrigatório para aulas online",
                    new[] { nameof(LinkAulaOnline) });
            }
        }

        /// <summary>
        /// Obtém o nome do dia da semana
        /// </summary>
        private string ObterNomeDiaSemana()
        {
            return DiaSemana switch
            {
                0 => "Domingo",
                1 => "Segunda-feira",
                2 => "Terça-feira",
                3 => "Quarta-feira",
                4 => "Quinta-feira",
                5 => "Sexta-feira",
                6 => "Sábado",
                _ => "Desconhecido"
            };
        }
    }
}
