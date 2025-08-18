namespace ApiIntegracao.DTOs
{
    /// <summary>
    /// DTO com detalhes completos de um cronograma
    /// </summary>
    public class CronogramaDetalheDto
    {
        /// <summary>
        /// ID interno do cronograma
        /// </summary>
        public Guid Id { get; set; }

        // === Informações da Turma ===
        /// <summary>
        /// ID da turma no Portal FAT
        /// </summary>
        public string IdTurmaFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome da turma
        /// </summary>
        public string NomeTurma { get; set; } = string.Empty;

        /// <summary>
        /// ID da turma na CETTPRO
        /// </summary>
        public Guid? IdTurmaCettpro { get; set; }

        // === Informações da Disciplina ===
        /// <summary>
        /// ID da disciplina no Portal FAT
        /// </summary>
        public string IdDisciplinaFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome da disciplina
        /// </summary>
        public string NomeDisciplina { get; set; } = string.Empty;

        // === Informações do Curso ===
        /// <summary>
        /// ID do curso no Portal FAT
        /// </summary>
        public string IdCursoFat { get; set; } = string.Empty;

        /// <summary>
        /// Nome do curso
        /// </summary>
        public string NomeCurso { get; set; } = string.Empty;

        /// <summary>
        /// Carga horária do curso
        /// </summary>
        public int? CargaHorariaCurso { get; set; }

        // === Período do Cronograma ===
        /// <summary>
        /// Data de início do cronograma
        /// </summary>
        public DateTime DataInicio { get; set; }

        /// <summary>
        /// Data de término do cronograma
        /// </summary>
        public DateTime DataTermino { get; set; }

        // === Estatísticas ===
        /// <summary>
        /// Total de aulas no cronograma
        /// </summary>
        public int TotalAulas { get; set; }

        /// <summary>
        /// Total de horas-aula
        /// </summary>
        public double TotalHoras { get; set; }

        /// <summary>
        /// Aulas já realizadas
        /// </summary>
        public int AulasRealizadas { get; set; }

        /// <summary>
        /// Aulas pendentes
        /// </summary>
        public int AulasPendentes { get; set; }

        /// <summary>
        /// Percentual de conclusão
        /// </summary>
        public double PercentualConcluido { get; set; }

        /// <summary>
        /// Média de horas por aula
        /// </summary>
        public double MediaHorasPorAula { get; set; }

        // === Horários ===
        /// <summary>
        /// Horários semanais das aulas
        /// </summary>
        public List<HorarioDto> Horarios { get; set; } = new();

        /// <summary>
        /// Distribuição de aulas por dia da semana
        /// </summary>
        public Dictionary<string, int> AulasPorDiaSemana { get; set; } = new();

        // === Lista de Aulas ===
        /// <summary>
        /// Lista completa das aulas geradas
        /// </summary>
        public List<AulaGeradaDto> Aulas { get; set; } = new();

        /// <summary>
        /// Próximas 5 aulas
        /// </summary>
        public List<AulaGeradaDto> ProximasAulas { get; set; } = new();

        /// <summary>
        /// Últimas 5 aulas realizadas
        /// </summary>
        public List<AulaGeradaDto> UltimasAulasRealizadas { get; set; } = new();

        // === Status e Controle ===
        /// <summary>
        /// Status do cronograma (Ativo, Inativo, Concluído, Cancelado)
        /// </summary>
        public string Status { get; set; } = "Ativo";

        /// <summary>
        /// Observações do cronograma
        /// </summary>
        public string? Observacoes { get; set; }

        // === Auditoria ===
        /// <summary>
        /// Data de criação do cronograma
        /// </summary>
        public DateTime DataCriacao { get; set; }

        /// <summary>
        /// Usuário que criou o cronograma
        /// </summary>
        public string? UsuarioCriacao { get; set; }

        /// <summary>
        /// Data da última atualização
        /// </summary>
        public DateTime? DataUltimaAtualizacao { get; set; }

        /// <summary>
        /// Usuário da última atualização
        /// </summary>
        public string? UsuarioAtualizacao { get; set; }

        /// <summary>
        /// Data da última sincronização com CETTPRO
        /// </summary>
        public DateTime? DataUltimaSincronizacao { get; set; }

        // === Metadados ===
        /// <summary>
        /// Metadados adicionais do cronograma
        /// </summary>
        public Dictionary<string, object> Metadados { get; set; } = new();

        /// <summary>
        /// Tags associadas ao cronograma
        /// </summary>
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Versão do cronograma
        /// </summary>
        public int Versao { get; set; } = 1;
    }

}
