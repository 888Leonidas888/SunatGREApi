using System.Data;

namespace SunatGreApi.Repositories
{
    public interface ISqlServerRepository
    {
        Task<List<(string nombreComercial, string codigoTela, string ordenCompra, string codigoProveedor)>> GetDetalleBienListAsync(string partida);
        Task<(string codigoClaseOrden, string codigoCentroCosto)> GetCabeceraBienAsync(string ordenCompra);
        Task<string> GetMovimientoPorClaseAsync(string codigoClaseOrden);
    }
}
