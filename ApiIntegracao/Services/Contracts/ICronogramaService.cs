using ApiIntegracao.DTOs;

namespace ApiIntegracao.Services.Contracts
{
    public interface ICronogramaService
    {
        Task<CronogramaResponseDto> GerarCronogramaAsync(CronogramaRequestDto request);
    }
}