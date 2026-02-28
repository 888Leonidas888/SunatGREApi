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
                bool headerPopulated = false;

                foreach (var bien in guia.Bienes.OrderBy(b => b.NumOrden))
                {
                    if (string.IsNullOrWhiteSpace(bien.Partida))
                        continue;

                    // 1. Obtener lista de detalles desde SQL Server para la Partida de este bien
                    var detalles = await _sqlRepository.GetDetalleBienListAsync(bien.Partida);
                    
                    if (detalles == null || detalles.Count == 0)
                        continue;

                    // 2. Buscar coincidencia: bien.NombreComercial debe estar al inicio de d.nombreComercial
                    var searchKey = (bien.NombreComercial ?? string.Empty).Trim();
                    var partialSearch = searchKey.Length > 15 ? searchKey.Substring(0, 15) : searchKey;

                    var match = detalles.FirstOrDefault(d => 
                        !string.IsNullOrEmpty(partialSearch) &&
                        d.nombreComercial != null && 
                        d.nombreComercial.Contains(partialSearch, StringComparison.OrdinalIgnoreCase));


                    if (match != default)
                    {
                        // Enriquecer el bien individual
                        bien.CodigoTela = match.codigoTela;

                        // Alimentar cabecera solo con el primer match exitoso encontrado
                        if (!headerPopulated)
                        {
                            guia.OrdenCompra = match.ordenCompra;
                            headerPopulated = true;
                        }
                    }
                }

                // Fact: Si tras procesar todos los bienes no tenemos OC, intentamos SunatHelper (Fallback)
                if (string.IsNullOrWhiteSpace(guia.OrdenCompra) && !string.IsNullOrWhiteSpace(guia.Nota))
                {
                    guia.OrdenCompra = SunatHelper.GetOrdenCompra(guia.Nota);
                    if (!string.IsNullOrWhiteSpace(guia.OrdenCompra))
                    {
                        _logger.LogInformation("Orden de compra obtenida mediante fallback de SunatHelper para guia {GuiaId}.", guiaId);
                    }
                }

                // 3. Cascada de enriquecimiento de cabecera si tenemos Orden de Compra
                if (!string.IsNullOrWhiteSpace(guia.OrdenCompra))
                {
                    var (codigoClaseOrden, codigoEstadoOrden, codigoCentroCosto, codigoProveedor) = await _sqlRepository.GetCabeceraBienAsync(guia.OrdenCompra);
                    guia.CodigoClaseOrden = codigoClaseOrden;
                    guia.CodigoEstadoOrden = codigoEstadoOrden;
                    guia.CodigoCentroCosto = codigoCentroCosto;
                    guia.CodigoProveedor = codigoProveedor;

                    if (!string.IsNullOrWhiteSpace(guia.CodigoClaseOrden))
                    {
                        var tipoMovimiento = await _sqlRepository.GetMovimientoPorClaseAsync(guia.CodigoClaseOrden);
                        guia.TipoMovimiento = tipoMovimiento;
                    }
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Enriquecimiento completado para la guía {GuiaId}.", guiaId);
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
