using ApiIntegracao.DTOs.Frequencia;

namespace ApiIntegracao.Services.Contracts
{
    public interface IFrequenciaService
    {
        Task<FrequenciaResponseDto> ProcessarFrequenciaAsync(
            FrequenciaRequestDto request,
            IFormFile arquivo);
    }
}
