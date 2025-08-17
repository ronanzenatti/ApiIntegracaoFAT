using ApiIntegracao.DTOs;

namespace ApiIntegracao.Services.Contracts
{
    public interface IFrequenciaService
    {
        Task<FrequenciaResponseDto> ProcessarFrequenciaAsync(
            FrequenciaRequestDto request,
            IFormFile arquivo);
    }
}
