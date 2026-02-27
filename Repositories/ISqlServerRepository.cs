using System.Data;

namespace SunatGreApi.Repositories
{
    public interface ISqlServerRepository
    {
        Task<(string nombreComercial, string codigoTela, string ordenCompra, string codigoProveedor)> GetDetalleBienAsync(string partida);
        Task<(string codigoClaseOrden, string codigoCentroCosto)> GetCabeceraBienAsync(string ordenCompra);
        Task<string> GetMovimientoPorClaseAsync(string codigoClaseOrden);
    }
}
