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

        public async Task<bool> ValidateGuiaAsync(string guiaId)
        {
            var guia = await _dbContext.Guias
                .Include(g => g.Bienes)
                .FirstOrDefaultAsync(g => g.Id == guiaId);

            if (guia == null)
            {
                _logger.LogWarning("Guia con ID {GuiaId} no encontrada para validación.", guiaId);
                return false;
            }

            var errors = new List<string>();

            // Validación de cabecera
            if (string.IsNullOrWhiteSpace(guia.Serie)) errors.Add("Serie es requerida.");
            if (string.IsNullOrWhiteSpace(guia.Numero)) errors.Add("Numero es requerido.");
            if (guia.FechaEmision == default) errors.Add("Fecha de Emision es requerida.");
            if (string.IsNullOrWhiteSpace(guia.OrdenCompra)) errors.Add("Orden de compra es requerida.");
            if (string.IsNullOrWhiteSpace(guia.CodigoProveedor)) errors.Add("Codigo de proveedor es requerido.");
            if (string.IsNullOrWhiteSpace(guia.TipoMovimiento)) errors.Add("Tipo de movimiento es requerido.");
            if (string.IsNullOrWhiteSpace(guia.CodigoCentroCosto)) errors.Add("Codigo de centro de costo es requerido.");

            // 1. Centro de costos autorizados
            var authorizedCostCenters = new List<string> { "1040001", "1040004" };
            if (!string.IsNullOrWhiteSpace(guia.CodigoCentroCosto) && !authorizedCostCenters.Contains(guia.CodigoCentroCosto))
            {
                errors.Add($"Centro de costo {guia.CodigoCentroCosto} no autorizado.");
            }

            // 2. Validación de estado de orden
            if (string.IsNullOrWhiteSpace(guia.CodigoEstadoOrden))
            {
                errors.Add("Codigo de estado de orden es requerido/esta vacio.");
            }
            else if (guia.CodigoEstadoOrden.Trim().Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Codigo de estado de orden no puede ser 'C'(cerrado).");
            }

            // Validación de detalle
            foreach (var bien in guia.Bienes)
            {
                var detailErrors = new List<string>();
                // 0. Campos requeridos
                if (bien.NumCantidad <= 0) detailErrors.Add("Cantidad debe ser mayor que 0.");
                if (string.IsNullOrWhiteSpace(bien.Partida)) detailErrors.Add("Partida es requerida.");
                if (bien.PesoBruto <= 0) detailErrors.Add("Peso bruto debe ser mayor que 0.");
                if (bien.Rollos <= 0) detailErrors.Add("Rollos debe ser mayor que 0.");
                if (string.IsNullOrWhiteSpace(bien.CodigoTela)) detailErrors.Add("Codigo de tela es requerido.");

                // 1. Peso Bruto >= Peso Neto (NumCantidad)
                if (bien.PesoBruto < bien.NumCantidad)
                {
                    detailErrors.Add($"Peso bruto ({bien.PesoBruto}) debe ser igual o mayor al peso neto/cantidad ({bien.NumCantidad}).");
                }

                if (detailErrors.Any())
                {
                    errors.Add($"Detalle OC {guia.OrdenCompra} - Bien {bien.CodBien}: {string.Join(" | ", detailErrors)}");
                }
            }

            if (errors.Any())
            {
                guia.EstadoProceso = "OBSERVADO";
                guia.LogProceso = string.Join("; ", errors);
                _logger.LogInformation("Guia {GuiaId} validada con errores. Estado: OBSERVADO.", guiaId);
            }
            else
            {
                guia.EstadoProceso = "PENDIENTE";
                guia.LogProceso = "Validación exitosa.";
                _logger.LogInformation("Guia {GuiaId} validada exitosamente. Estado: PENDIENTE.", guiaId);
            }

            await _dbContext.SaveChangesAsync();
            return !errors.Any();
        }
    }
}
