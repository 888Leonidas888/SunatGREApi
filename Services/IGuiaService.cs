namespace SunatGreApi.Services
{
    public interface IGuiaService
    {
        Task<bool> EnrichGuiaAsync(string guiaId);
        Task<bool> ValidateGuiaAsync(string guiaId);
    }
}
