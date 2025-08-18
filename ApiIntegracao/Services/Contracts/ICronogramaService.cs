using ApiIntegracao.DTOs;
using ApiIntegracao.DTOs.ApiIntegracao.DTOs;

namespace ApiIntegracao.Services.Contracts
{
    /// <summary>
    /// Interface para o serviço de gerenciamento de cronogramas
    /// </summary>
    public interface ICronogramaService
    {
        /// <summary>
        /// Gera um novo cronograma de aulas para uma turma específica
        /// </summary>
        /// <param name="request">Dados para geração do cronograma</param>
        /// <returns>Resultado da geração do cronograma com as aulas criadas</returns>
        /// <exception cref="ArgumentException">Quando os dados de entrada são inválidos</exception>
        /// <exception cref="InvalidOperationException">Quando já existe um cronograma para a turma/disciplina</exception>
        Task<CronogramaResponseDto> GerarCronogramaAsync(CronogramaRequestDto request);

        /// <summary>
        /// Lista todos os cronogramas cadastrados com opção de filtros
        /// </summary>
        /// <param name="idTurmaFat">Filtro opcional por ID da turma FAT</param>
        /// <param name="idDisciplinaFat">Filtro opcional por ID da disciplina FAT</param>
        /// <returns>Lista de cronogramas resumidos</returns>
        Task<IEnumerable<CronogramaListaDto>> ListarCronogramasAsync(
            string? idTurmaFat = null,
            string? idDisciplinaFat = null);

        /// <summary>
        /// Obtém os detalhes completos de um cronograma específico
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <returns>Detalhes do cronograma ou null se não encontrado</returns>
        Task<CronogramaDetalheDto?> ObterCronogramaAsync(
            string idTurmaFat,
            string idDisciplinaFat);

        /// <summary>
        /// Exclui um cronograma existente
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <returns>True se excluído com sucesso, False se não encontrado</returns>
        Task<bool> ExcluirCronogramaAsync(
            string idTurmaFat,
            string idDisciplinaFat);

        /// <summary>
        /// Atualiza um cronograma existente com novos horários
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <param name="request">Novos dados do cronograma</param>
        /// <returns>Cronograma atualizado</returns>
        /// <exception cref="ArgumentException">Quando os dados de entrada são inválidos</exception>
        /// <exception cref="InvalidOperationException">Quando o cronograma não existe</exception>
        Task<CronogramaResponseDto> AtualizarCronogramaAsync(
            string idTurmaFat,
            string idDisciplinaFat,
            CronogramaRequestDto request);

        /// <summary>
        /// Verifica se já existe um cronograma para a turma e disciplina especificadas
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <returns>True se o cronograma existe, False caso contrário</returns>
        Task<bool> ExisteCronogramaAsync(
            string idTurmaFat,
            string idDisciplinaFat);

        /// <summary>
        /// Obtém estatísticas dos cronogramas gerados
        /// </summary>
        /// <returns>Estatísticas consolidadas dos cronogramas</returns>
        Task<CronogramaEstatisticasDto> ObterEstatisticasAsync();

        /// <summary>
        /// Lista todas as aulas de um cronograma específico
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <param name="dataInicio">Filtro opcional de data inicial</param>
        /// <param name="dataFim">Filtro opcional de data final</param>
        /// <returns>Lista de aulas do cronograma</returns>
        Task<IEnumerable<AulaGeradaDto>> ListarAulasAsync(
            string idTurmaFat,
            string idDisciplinaFat,
            DateTime? dataInicio = null,
            DateTime? dataFim = null);

        /// <summary>
        /// Valida se os horários informados são válidos e não conflitantes
        /// </summary>
        /// <param name="horarios">Lista de horários a validar</param>
        /// <returns>Resultado da validação com mensagens de erro se houver</returns>
        Task<ValidacaoHorariosResult> ValidarHorariosAsync(List<HorarioDto> horarios);

        /// <summary>
        /// Calcula o total de horas-aula que serão geradas com base nos parâmetros
        /// </summary>
        /// <param name="dataInicio">Data de início do cronograma</param>
        /// <param name="dataTermino">Data de término do cronograma</param>
        /// <param name="horarios">Horários das aulas</param>
        /// <returns>Total de horas-aula e quantidade de aulas</returns>
        Task<CalculoHorasResult> CalcularTotalHorasAulaAsync(
            DateTime dataInicio,
            DateTime dataTermino,
            List<HorarioDto> horarios);

        /// <summary>
        /// Exporta um cronograma em formato específico (PDF, Excel, etc.)
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT</param>
        /// <param name="idDisciplinaFat">ID da disciplina no FAT</param>
        /// <param name="formato">Formato de exportação (PDF, XLSX, CSV)</param>
        /// <returns>Arquivo exportado como byte array</returns>
        Task<ExportacaoCronogramaResult> ExportarCronogramaAsync(
            string idTurmaFat,
            string idDisciplinaFat,
            FormatoExportacao formato);

        /// <summary>
        /// Gera cronogramas em lote para múltiplas turmas
        /// </summary>
        /// <param name="requests">Lista de requisições de cronograma</param>
        /// <returns>Resultado do processamento em lote</returns>
        Task<CronogramaLoteResult> GerarCronogramasEmLoteAsync(
            List<CronogramaRequestDto> requests);

        /// <summary>
        /// Sincroniza cronogramas com o sistema CETTPRO
        /// </summary>
        /// <param name="idTurmaFat">ID da turma no FAT (opcional para sincronizar todas)</param>
        /// <returns>Resultado da sincronização</returns>
        Task<SincronizacaoCronogramaResult> SincronizarComCettproAsync(
            string? idTurmaFat = null);
    }

}