using Microsoft.EntityFrameworkCore;
using SunatGreApi.Data;
using SunatGreApi.Models;
using SunatGreApi.Repositories;
using SunatGreApi.Utils;

namespace SunatGreApi.Services
{
    public class GuiaService : IGuiaService
    {
        private readonly AppDbContext _dbContext;
        private readonly ISqlServerRepository _sqlRepository;
        private readonly ILogger<GuiaService> _logger;

        public GuiaService(AppDbContext dbContext, ISqlServerRepository sqlRepository, ILogger<GuiaService> logger)
        {
            _dbContext = dbContext;
            _sqlRepository = sqlRepository;
            _logger = logger;
        }

        public async Task<bool> EnrichGuiaAsync(string guiaId)
        {
            var guia = await _dbContext.Guias
                .Include(g => g.Bienes)
                .FirstOrDefaultAsync(g => g.Id == guiaId);

            if (guia == null)
            {
                _logger.LogWarning("Guia con ID {GuiaId} no encontrada para enriquecimiento.", guiaId);
                return false;
            }

            var primerBien = guia.Bienes.OrderBy(b => b.NumOrden).FirstOrDefault();
            if (primerBien == null || string.IsNullOrWhiteSpace(primerBien.Partida))
            {
                _logger.LogInformation("No se encontró Partida en el primer bien de la guía {GuiaId}. Se detiene el enriquecimiento.", guiaId);
                return false;
            }

            try
            {
                // 1. Obtener detalles básicos desde SQL Server (usp_gre_detalle_bien)
                var (nombreComercial, codigoTela, ordenCompra, codigoProveedor) = await _sqlRepository.GetDetalleBienAsync(primerBien.Partida);

                // Actualizar campos de cabecera y el primer bien
                guia.CodigoProveedor = codigoProveedor;
                primerBien.CodigoTela = codigoTela;
                
                // Si la DB devolvió una orden de compra, la usamos
                if (!string.IsNullOrWhiteSpace(ordenCompra))
                {
                    guia.OrdenCompra = ordenCompra;
                }
                
                // Fallback: Si no hay OrdenCompra desde la DB, usamos SunatHelper sobre Guia.Nota
                if (string.IsNullOrWhiteSpace(guia.OrdenCompra) && !string.IsNullOrWhiteSpace(guia.Nota))
                {
                    guia.OrdenCompra = SunatHelper.GetOrdenCompra(guia.Nota);
                    _logger.LogInformation("Orden de compra obtenida mediante fallback de SunatHelper para guia {GuiaId}.", guiaId);
                }

                // 2. Si tenemos Orden de Compra, seguimos enriqueciendo
                if (!string.IsNullOrWhiteSpace(guia.OrdenCompra))
                {
                    var (codigoClaseOrden, codigoCentroCosto) = await _sqlRepository.GetCabeceraBienAsync(guia.OrdenCompra);
                    guia.CodigoClaseOrden = codigoClaseOrden;
                    guia.CodigoCentroCosto = codigoCentroCosto;

                    // 3. Si tenemos Clase de Orden, obtenemos el Tipo de Movimiento
                    if (!string.IsNullOrWhiteSpace(guia.CodigoClaseOrden))
                    {
                        var tipoMovimiento = await _sqlRepository.GetMovimientoPorClaseAsync(guia.CodigoClaseOrden);
                        guia.TipoMovimiento = tipoMovimiento;
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Enriquecimiento completado exitosamente para la guía {GuiaId}.", guiaId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el enriquecimiento de la guía {GuiaId}.", guiaId);
                return false;
            }
        }
    }
}
