namespace SunatGreApi.Services
{
    public interface IGuiaService
    {
        Task<bool> EnrichGuiaAsync(string guiaId);
        Task<bool> ValidateGuiaAsync(string guiaId);
        SunatGreApi.Models.Guia MapToEntity(SunatGreApi.Models.Dtos.SunatGreDto dto);
    }
}
