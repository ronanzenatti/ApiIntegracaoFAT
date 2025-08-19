using ApiIntegracao.DTOs.Frequencia;
using System.ComponentModel.DataAnnotations;

namespace ApiIntegracao.DTOs
{
    namespace ApiIntegracao.DTOs
    {
        /// <summary>
        /// DTO para requisição de geração de cronograma
        /// </summary>
        public class CronogramaRequestDto : IValidatableObject
        {
            /// <summary>
            /// ID do curso no Portal FAT
            /// </summary>
            [Required(ErrorMessage = "O ID do curso FAT é obrigatório")]
            [StringLength(50, ErrorMessage = "O ID do curso FAT deve ter no máximo 50 caracteres")]
            public string IdCursoFat { get; set; } = string.Empty;

            /// <summary>
            /// ID da turma no Portal FAT
            /// </summary>
            [Required(ErrorMessage = "O ID da turma FAT é obrigatório")]
            [StringLength(50, ErrorMessage = "O ID da turma FAT deve ter no máximo 50 caracteres")]
            public string IdTurmaFat { get; set; } = string.Empty;

            /// <summary>
            /// ID da disciplina no Portal FAT
            /// </summary>
            [Required(ErrorMessage = "O ID da disciplina FAT é obrigatório")]
            [StringLength(50, ErrorMessage = "O ID da disciplina FAT deve ter no máximo 50 caracteres")]
            public string IdDisciplinaFat { get; set; } = string.Empty;

            /// <summary>
            /// Nome da disciplina no Portal FAT
            /// </summary>
            [Required(ErrorMessage = "O nome da disciplina FAT é obrigatório")]
            [StringLength(200, MinimumLength = 3, ErrorMessage = "O nome da disciplina deve ter entre 3 e 200 caracteres")]
            public string NomeDisciplinaFat { get; set; } = string.Empty;

            /// <summary>
            /// Data de início do cronograma
            /// </summary>
            [Required(ErrorMessage = "A data de início é obrigatória")]
            [DataType(DataType.Date)]
            public DateTime DataInicio { get; set; }

            /// <summary>
            /// Data de término do cronograma
            /// </summary>
            [Required(ErrorMessage = "A data de término é obrigatória")]
            [DataType(DataType.Date)]
            public DateTime DataTermino { get; set; }

            /// <summary>
            /// Lista de horários das aulas
            /// </summary>
            [Required(ErrorMessage = "Pelo menos um horário deve ser informado")]
            [MinLength(1, ErrorMessage = "Pelo menos um horário deve ser informado")]
            [MaxLength(7, ErrorMessage = "Máximo de 7 horários (um por dia da semana)")]
            public List<HorarioDto> Horarios { get; set; } = new();

            /// <summary>
            /// Observações adicionais (opcional)
            /// </summary>
            [StringLength(500, ErrorMessage = "As observações devem ter no máximo 500 caracteres")]
            public string? Observacoes { get; set; }

            /// <summary>
            /// Indica se deve sobrescrever cronograma existente
            /// </summary>
            public bool SobrescreverExistente { get; set; } = false;

            /// <summary>
            /// Validação customizada do objeto
            /// </summary>
            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                // Validar período
                if (DataTermino <= DataInicio)
                {
                    yield return new ValidationResult(
                        "A data de término deve ser posterior à data de início",
                        new[] { nameof(DataTermino) });
                }

                // Validar período máximo
                var diasTotal = (DataTermino - DataInicio).TotalDays;
                if (diasTotal > 365)
                {
                    yield return new ValidationResult(
                        "O período do cronograma não pode exceder 365 dias",
                        new[] { nameof(DataTermino) });
                }

                // Validar horários duplicados
                var diasDuplicados = Horarios
                    .GroupBy(h => h.DiaSemana)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key);

                if (diasDuplicados.Any())
                {
                    yield return new ValidationResult(
                        $"Existem horários duplicados para o(s) dia(s): {string.Join(", ", diasDuplicados)}",
                        new[] { nameof(Horarios) });
                }
            }
        }

    }
}