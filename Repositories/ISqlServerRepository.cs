using System.Data;

namespace SunatGreApi.Repositories
{
    public interface ISqlServerRepository
    {
        Task<List<(string nombreComercial, string codigoTela, string ordenCompra)>> GetDetalleBienListAsync(string partida);
        Task<(string codigoClaseOrden, string codigoEstadoOrden,string codigoCentroCosto, string codigoProveedor)> GetCabeceraBienAsync(string ordenCompra);
        Task<string> GetMovimientoPorClaseAsync(string codigoClaseOrden);
    }
}
