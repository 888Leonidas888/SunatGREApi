using Microsoft.Extensions.Logging;
using SunatGreApi.Models;
using SunatGreApi.Models.Dtos;
using SunatGreApi.Repositories;
using SunatGreApi.Utils;

namespace SunatGreApi.Services
{
    /// <summary>
    /// Implementación de la lógica de negocio para la gestión de guías.
    /// </summary>
    public class GuiaService : IGuiaService
    {
        private readonly IGuiaRepository _guiaRepository;
        private readonly ISqlServerRepository _sqlRepository;
        private readonly ILogger<GuiaService> _logger;

        public GuiaService(IGuiaRepository guiaRepository, ISqlServerRepository sqlRepository, ILogger<GuiaService> logger)
        {
            _guiaRepository = guiaRepository;
            _sqlRepository = sqlRepository;
            _logger = logger;
        }

        public async Task<(IEnumerable<Guia> Data, int TotalRecords)> GetPagedGuiasAsync(DateTime? fecha, string? estadoProceso, string? etapa, int page, int pageSize)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;

            var centrosCosto = new List<string>();
            if (!string.IsNullOrEmpty(etapa))
            {
                if (etapa.Equals("PRODUCCION", StringComparison.OrdinalIgnoreCase))
                {
                    centrosCosto.AddRange(new[] { "1040001", "1040004" });
                }
                else if (etapa.Equals("DESARROLLO", StringComparison.OrdinalIgnoreCase))
                {
                    centrosCosto.Add("1050001");
                }
                else
                {
                    centrosCosto.Add("1050001");
                }
            }

            var data = await _guiaRepository.GetGuiasAsync(fecha, estadoProceso, centrosCosto, page, pageSize);
            return (data, data.Count());
        }

        public async Task<Guia?> GetGuiaByIdAsync(string id)
        {
            return await _guiaRepository.GetByIdAsync(id);
        }

        public async Task<Guia?> ProcessAndRegisterGuiaAsync(SunatGreDto dto)
        {
            // Regla de negocio: Omitir si el estado es BAJA
            if (!string.IsNullOrEmpty(dto.DesEstado) && dto.DesEstado.Equals("BAJA", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Guía {Id} omitida por estado BAJA.", dto.Id);
                return null;
            }

            // Regla de negocio: Omitir si contiene 'TWILL' en la descripción del bien
            if (dto.Traslado?.Bien != null && dto.Traslado.Bien.Any(b => b.DesBien != null && b.DesBien.Contains("TWILL", StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Guía {Id} omitida por contener palabra 'TWILL'.", dto.Id);
                return null;
            }

            var guia = MapToEntity(dto);

            // Validar duplicados
            if (await _guiaRepository.ExistsByIdAsync(guia.Id) || 
                await _guiaRepository.ExistsByNaturalKeyAsync(guia.RucEmisor, guia.TipoDocumento, guia.Serie, guia.Numero))
            {
                throw new InvalidOperationException($"La guía {guia.Id} ya está registrada.");
            }

            await _guiaRepository.AddAsync(guia);
            await _guiaRepository.SaveChangesAsync();

            // Enriquecimiento y Validación automáticos
            await EnrichGuiaAsync(guia.Id);
            await ValidateGuiaAsync(guia.Id);

            return guia;
        }

        public async Task<bool> UpdateEstadoProcesoAsync(string id, string estadoProceso)
        {
            var estadosValidos = new[] { "PENDIENTE", "PROCESADO", "ERROR", "COMPLETADO" };
            if (string.IsNullOrEmpty(estadoProceso) || !estadosValidos.Contains(estadoProceso.ToUpper()))
                return false;

            var guia = await _guiaRepository.GetByIdAsync(id);
            if (guia == null) return false;

            guia.EstadoProceso = estadoProceso.ToUpper();
            await _guiaRepository.UpdateAsync(guia);
            await _guiaRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteGuiaAsync(string id)
        {
            var exists = await _guiaRepository.ExistsByIdAsync(id);
            if (!exists) return false;

            await _guiaRepository.DeleteAsync(id);
            await _guiaRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> EnrichGuiaAsync(string guiaId)
        {
            var guia = await _guiaRepository.GetByIdAsync(guiaId);
            if (guia == null) return false;

            try
            {
                bool headerPopulated = false;

                foreach (var bien in guia.Bienes.OrderBy(b => b.NumOrden))
                {
                    if (string.IsNullOrWhiteSpace(bien.Partida)) continue;

                    var detalles = await _sqlRepository.GetDetalleBienListAsync(bien.Partida);
                    if (detalles == null || !detalles.Any()) continue;

                    var searchKey = (bien.NombreComercial ?? string.Empty).Trim();
                    var firstWord = searchKey.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";

                    var match = detalles.FirstOrDefault(d => 
                        !string.IsNullOrEmpty(firstWord) && 
                        d.nombreComercial != null && 
                        d.nombreComercial.Contains(firstWord, StringComparison.OrdinalIgnoreCase));

                    // Fallback Levenshtein
                    if (match == default && firstWord.Length >= 4)
                    {
                        match = detalles.FirstOrDefault(d =>
                        {
                            if (string.IsNullOrEmpty(d.nombreComercial)) return false;
                            var dbFirstWord = d.nombreComercial.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                            return dbFirstWord.Length >= 4 && SunatHelper.ComputeLevenshteinDistance(firstWord.ToUpper(), dbFirstWord.ToUpper()) <= 2;
                        });
                    }

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

                if (string.IsNullOrWhiteSpace(guia.OrdenCompra) && !string.IsNullOrWhiteSpace(guia.Nota))
                {
                    guia.OrdenCompra = SunatHelper.GetOrdenCompra(guia.Nota);
                }

                if (!string.IsNullOrWhiteSpace(guia.OrdenCompra))
                {
                    var (codigoClaseOrden, codigoEstadoOrden, codigoCentroCosto, codigoProveedor) = await _sqlRepository.GetCabeceraBienAsync(guia.OrdenCompra);
                    guia.CodigoClaseOrden = codigoClaseOrden;
                    guia.CodigoEstadoOrden = codigoEstadoOrden;
                    guia.CodigoCentroCosto = codigoCentroCosto;
                    guia.CodigoProveedor = codigoProveedor;

                    if (!string.IsNullOrWhiteSpace(guia.CodigoClaseOrden))
                    {
                        guia.TipoMovimiento = await _sqlRepository.GetMovimientoPorClaseAsync(guia.CodigoClaseOrden);
                    }
                }

                await _guiaRepository.UpdateAsync(guia);
                await _guiaRepository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enriquecer guía {GuiaId}.", guiaId);
                return false;
            }
        }

        public async Task<bool> ValidateGuiaAsync(string guiaId)
        {
            var guia = await _guiaRepository.GetByIdAsync(guiaId);
            if (guia == null) return false;

            var errors = new List<string>();

            // Validaciones críticas
            if (guia.Bienes.Any(b => string.IsNullOrWhiteSpace(b.Partida))) errors.Add("Partida no encontrada.");
            if (guia.Bienes.Any(b => string.IsNullOrWhiteSpace(b.CodigoTela))) errors.Add("Código de tela no encontrado.");
            if (string.IsNullOrWhiteSpace(guia.OrdenCompra)) errors.Add("Orden de compra no ubicada.");

            // Validaciones de cabecera
            if (string.IsNullOrWhiteSpace(guia.Serie)) errors.Add("Serie requerida.");
            if (string.IsNullOrWhiteSpace(guia.Numero)) errors.Add("Número requerido.");
            if (string.IsNullOrWhiteSpace(guia.CodigoProveedor)) errors.Add("Proveedor requerido.");
            if (string.IsNullOrWhiteSpace(guia.TipoMovimiento)) errors.Add("Tipo movimiento requerido.");

            var authorizedCC = new[] { "1040001", "1040004" };
            if (!string.IsNullOrWhiteSpace(guia.CodigoCentroCosto) && !authorizedCC.Contains(guia.CodigoCentroCosto))
                errors.Add("Centro de costo no autorizado.");

            if (!string.IsNullOrWhiteSpace(guia.CodigoEstadoOrden) && guia.CodigoEstadoOrden.Trim().Equals("C", StringComparison.OrdinalIgnoreCase))
                errors.Add("Orden cerrada.");

            foreach (var bien in guia.Bienes)
            {
                if (bien.NumCantidad <= 0 || bien.PesoBruto <= 0 || bien.Rollos <= 0) errors.Add("Cantidades/Peso deben ser > 0.");
                if (bien.PesoBruto < bien.NumCantidad) errors.Add("Peso bruto < peso neto.");
            }

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

            await _guiaRepository.UpdateAsync(guia);
            await _guiaRepository.SaveChangesAsync();
            return !errors.Any();
        }

        public Guia MapToEntity(SunatGreDto greDto)
        {
            return new Guia
            {
                Id = greDto.Id,
                Serie = greDto.NumSerie,
                Numero = greDto.NumCpe.ToString(),
                RucEmisor = greDto.NumRuc,
                TipoDocumento = greDto.CodTipoCpe,
                FechaEmision = DateTime.TryParse(greDto.Emision?.FecEmision, out var fec) ? fec : DateTime.Now,
                Receptor = greDto.Receptor?.DesNombre,
                Estado = greDto.DesEstado,
                FechaCarga = DateTime.Now,
                Nota = greDto.Emision?.DesNota,
                LogProceso = "",
                Bienes = greDto.Traslado?.Bien?.Select(b => new GuiaBien
                {
                    GuiaId = greDto.Id,
                    NumOrden = b.NumOrden,
                    CodBien = b.CodBien,
                    DesBien = b.DesBien,
                    NombreComercial = SunatHelper.GetNombreComercial(b.DesBien ?? ""),
                    CodUniMedida = b.CodUniMedida,
                    DesUniMedida = b.DesUniMedida,
                    NumCantidad = b.NumCantidad,
                    Partida = SunatHelper.GetPartida(b.DesBien ?? ""),
                    Rollos = SunatHelper.GetRollos(b.DesBien ?? ""),
                    PesoBruto = SunatHelper.GetPesoBruto(b.DesBien ?? "")
                }).ToList() ?? new List<GuiaBien>()
            };
        }
    }
}
