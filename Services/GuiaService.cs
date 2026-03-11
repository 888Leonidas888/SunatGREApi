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

        // ================================================================
        // ==============      ENRICH (RESTORED VERSION)      =============
        // ================================================================
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

                    var detalles = await _sqlRepository.GetDetalleBienListAsync(bien.Partida);

                    if (detalles == null || detalles.Count == 0)
                        continue;

                    var searchKey = (bien.NombreComercial ?? string.Empty).Trim();
                    var partialSearch = searchKey.Length > 15 ? searchKey.Substring(0, 15) : searchKey;

                    var match = detalles.FirstOrDefault(d =>
                        !string.IsNullOrEmpty(partialSearch) &&
                        d.nombreComercial != null &&
                        d.nombreComercial.Contains(partialSearch, StringComparison.OrdinalIgnoreCase));

                    if (match != default)
                    {
                        bien.CodigoTela = match.codigoTela;

                        if (!headerPopulated)
                        {
                            guia.OrdenCompra = match.ordenCompra;
                            headerPopulated = true;
                        }
                    }
                }

                // NO MÁS fallback desde Nota — se quitó por requerimiento
                // Si SQL no llena OC, así se queda.

                // Si tenemos OC, poblar cabecera desde SQL
                if (!string.IsNullOrWhiteSpace(guia.OrdenCompra))
                {
                    var (codigoClaseOrden, codigoEstadoOrden, codigoCentroCosto, codigoProveedor)
                        = await _sqlRepository.GetCabeceraBienAsync(guia.OrdenCompra);

                    guia.CodigoClaseOrden = codigoClaseOrden;
                    guia.CodigoEstadoOrden = codigoEstadoOrden;
                    guia.CodigoCentroCosto = codigoCentroCosto;
                    guia.CodigoProveedor = codigoProveedor;

                    if (!string.IsNullOrWhiteSpace(guia.CodigoClaseOrden))
                    {
                        var tipoMovimiento =
                            await _sqlRepository.GetMovimientoPorClaseAsync(guia.CodigoClaseOrden);

                        guia.TipoMovimiento = tipoMovimiento;
                    }
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el enriquecimiento de la guía {GuiaId}.", guiaId);
                return false;
            }
        }

        // ================================================================
        // ==============  VALIDATE (ENUNCIADO EXACTO) ===================
        // ================================================================
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

            // 🔥 1. ORDEN DE COMPRA OBLIGATORIA (bloqueante)
            if (string.IsNullOrWhiteSpace(guia.OrdenCompra))
            {
                guia.EstadoProceso = "OBSERVADO";
                guia.LogProceso = "orden de compra no ubicada, no se puede continuar la validación";
                await _dbContext.SaveChangesAsync();
                return false;
            }

            // 🔥 2. PARTIDA OBLIGATORIA (bloqueante)
            foreach (var bien in guia.Bienes)
            {
                if (string.IsNullOrWhiteSpace(bien.Partida))
                {
                    guia.EstadoProceso = "OBSERVADO";
                    guia.LogProceso = "partida no ubicada, no se puede continuar la validación";
                    await _dbContext.SaveChangesAsync();
                    return false;
                }
            }

            // 🔥 3. CÓDIGO DE TELA OBLIGATORIO (bloqueante)
            foreach (var bien in guia.Bienes)
            {
                if (string.IsNullOrWhiteSpace(bien.CodigoTela))
                {
                    guia.EstadoProceso = "OBSERVADO";
                    guia.LogProceso =
                        $"descripcion comercial tela no ubicado para partida {bien.Partida}";
                    await _dbContext.SaveChangesAsync();
                    return false;
                }
            }

            // ============================================================
            //   SI LLEGA HASTA AQUÍ → YA CUMPLE LAS 3 VALIDACIONES
            // ============================================================

            var errors = new List<string>();

            // Validaciones restantes de cabecera
            if (string.IsNullOrWhiteSpace(guia.Serie)) errors.Add("Serie es requerida.");
            if (string.IsNullOrWhiteSpace(guia.Numero)) errors.Add("Numero es requerido.");
            if (guia.FechaEmision == default) errors.Add("Fecha de Emision es requerida.");
            if (string.IsNullOrWhiteSpace(guia.CodigoProveedor)) errors.Add("Codigo de proveedor es requerido.");
            if (string.IsNullOrWhiteSpace(guia.TipoMovimiento)) errors.Add("Tipo de movimiento es requerido.");
            if (string.IsNullOrWhiteSpace(guia.CodigoCentroCosto)) errors.Add("Codigo de centro de costo es requerido.");

            // Centros costos autorizados
            var authorizedCostCenters = new List<string> { "1040001", "1040004" };
            if (!string.IsNullOrWhiteSpace(guia.CodigoCentroCosto) &&
                !authorizedCostCenters.Contains(guia.CodigoCentroCosto))
            {
                errors.Add($"Centro de costo {guia.CodigoCentroCosto} no autorizado.");
            }

            // Estado de orden
            if (string.IsNullOrWhiteSpace(guia.CodigoEstadoOrden))
            {
                errors.Add("Codigo de estado de orden es requerido/esta vacio.");
            }
            else if (guia.CodigoEstadoOrden.Trim().Equals("C", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Codigo de estado de orden no puede ser 'C'(cerrado).");
            }

            // Validación detalle (sin mensajes tipo "Detalle OC ...")
            foreach (var bien in guia.Bienes)
            {
                if (bien.NumCantidad <= 0) errors.Add("Cantidad debe ser mayor que 0.");
                if (bien.PesoBruto <= 0) errors.Add("Peso bruto debe ser mayor que 0.");
                if (bien.Rollos <= 0) errors.Add("Rollos debe ser mayor que 0.");

                if (bien.PesoBruto < bien.NumCantidad)
                {
                    errors.Add($"Bien {bien.CodBien}: Peso bruto ({bien.PesoBruto}) debe ser igual o mayor al peso neto/cantidad ({bien.NumCantidad}).");
                }
            }

            // Resultado final
            if (errors.Any())
            {
                guia.EstadoProceso = "OBSERVADO";
                guia.LogProceso = string.Join("; ", errors);
            }
            else
            {
                guia.EstadoProceso = "PENDIENTE";
                guia.LogProceso = "Validación exitosa.";
            }

            await _dbContext.SaveChangesAsync();
            return !errors.Any();
        }
    }
}