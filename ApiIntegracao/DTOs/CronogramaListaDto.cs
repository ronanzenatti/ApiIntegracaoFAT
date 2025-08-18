namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO para listagem resumida de cronogramas
    /// </summary>
    public class CronogramaListaDto
    {
        /// <summary>
        /// ID interno do cronograma
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// ID da turma no Portal FAT
        /// </summary>
        public string IdTurmaFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome da turma
        /// </summary>
        public string NomeTurma { get; set; } = string.Empty;

        /// <summary>
        /// ID da disciplina no Portal FAT
        /// </summary>
        public string IdDisciplinaFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome da disciplina
        /// </summary>
        public string NomeDisciplina { get; set; } = string.Empty;

        /// <summary>
        /// ID do curso no Portal FAT
        /// </summary>
        public string IdCursoFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome do curso
        /// </summary>
        public string NomeCurso { get; set; } = string.Empty;

        /// <summary>
        /// Data de início do cronograma
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do cronograma
        /// </summary>
        public DateTime DataTermino { get; set; }

        /// <summary>
        /// Total de aulas no cronograma
        /// </summary>
        public int TotalAulas { get; set; }

        /// <summary>
        /// Total de horas-aula
        /// </summary>
        public double TotalHoras { get; set; }

        /// <summary>
        /// Status do cronograma (Ativo, Inativo, Concluído)
        /// </summary>
        public string Status { get; set; } = "Ativo";

        /// <summary>
        /// Percentual de conclusão
        /// </summary>
        public double PercentualConcluido { get; set; }

        /// <summary>
        /// Data de criação do cronograma
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime? DataUltimaAtualizacao { get; set; }

        /// <summary>
        /// Quantidade de aulas por semana
        /// </summary>
        public int AulasPorSemana { get; set; }

        /// <summary>
        /// Indica se tem aulas hoje
        /// </summary>
        public bool TemAulaHoje { get; set; }

        /// <summary>
        /// Próxima aula (se houver)
        /// </summary>
        public DateTime? ProximaAula { get; set; }
    }
}
