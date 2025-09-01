namespace ApiIntegracao.Services
{
    /// <summary>
    /// Serviço para validação e interpretação de status
    /// </summary>
    public static class StatusValidationService
    {
        /// <summary>
        /// Status válidos para turmas
        /// </summary>
        public static readonly Dictionary<int, string> StatusTurma = new()
        {
            { 731890001, "Aberta para Inscrições" },
            { 731890005, "Cancelada" },
            { 731890002, "Em Construção" },
            { 731890004, "Em Execução" },
            { 2, "Finalizada" },
            { 731890003, "Pronta para Execução" },
            { 731890008, "Suspensa" }
        };

        /// <summary>
        /// Status válidos para matrículas
        /// </summary>
        public static readonly Dictionary<int, string> StatusMatricula = new()
        {
            { 731890001, "Ativa" },
            { 731890002, "Inativa" },
            { 2, "Concluída" }
        };

        /// <summary>
        /// Verifica se um status de turma é válido
        /// </summary>
        public static bool IsValidTurmaStatus(int status)
        {
            return StatusTurma.ContainsKey(status);
        }

        /// <summary>
        /// Verifica se um status de matrícula é válido
        /// </summary>
        public static bool IsValidMatriculaStatus(int status)
        {
            return StatusMatricula.ContainsKey(status);
        }

        /// <summary>
        /// Obtém a descrição de um status de turma
        /// </summary>
        public static string GetTurmaStatusDescription(int status)
        {
            return StatusTurma.TryGetValue(status, out var description) ? description : "Status Desconhecido";
        }

        /// <summary>
        /// Obtém a descrição de um status de matrícula
        /// </summary>
        public static string GetMatriculaStatusDescription(int status)
        {
            return StatusMatricula.TryGetValue(status, out var description) ? description : "Status Desconhecido";
        }

        /// <summary>
        /// Verifica se uma turma está ativa (status que permite matrículas)
        /// </summary>
        public static bool IsTurmaAtiva(int status)
        {
            return status == 731890001 || // Aberta para Inscrições
                   status == 731890004 || // Em Execução
                   status == 731890003;   // Pronta para Execução
        }

        /// <summary>
        /// Verifica se uma matrícula está ativa
        /// </summary>
        public static bool IsMatriculaAtiva(int status)
        {
            return status == 731890001; // Ativa
        }
    }
}
