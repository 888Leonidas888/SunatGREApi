using Microsoft.AspNetCore.Mvc;
using SunatGreApi.Models.Dtos;
using SunatGreApi.Services;

namespace SunatGreApi.Controllers
{
    /// <summary>
    /// Controlador para la gestión de Guías de Remisión Electrónica.
    /// </summary>
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GuiaController : ControllerBase
    {
        private readonly IGuiaService _guiaService;
        private readonly ILogger<GuiaController> _logger;

        public GuiaController(IGuiaService guiaService, ILogger<GuiaController> logger)
        {
            _guiaService = guiaService;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene una lista paginada de guías con filtros opcionales.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetGuias(
            [FromQuery] DateTime? fecha = null,
            [FromQuery] string? estadoProceso = null,
            [FromQuery] string? etapa = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                var result = await _guiaService.GetPagedGuiasAsync(fecha, estadoProceso, etapa, page, pageSize);
                return Ok(new
                {
                    TotalRecords = result.TotalRecords,
                    Page = page,
                    PageSize = pageSize,
                    Data = result.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener guías.");
                return StatusCode(500, "Error interno del servidor.");
            }
        }

        /// <summary>
        /// Obtiene el detalle de una guía por su identificador.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGuia(string id)
        {
            var guia = await _guiaService.GetGuiaByIdAsync(id);
            return guia == null ? NotFound() : Ok(guia);
        }

        /// <summary>
        /// Registra y procesa una nueva guía de SUNAT.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PostGuia(SunatGreDto dto)
        {
            try
            {
                var guia = await _guiaService.ProcessAndRegisterGuiaAsync(dto);
                if (guia == null)
                {
                    return Ok(new { Message = "La guía fue omitida por reglas de negocio." });
                }

                return CreatedAtAction(nameof(GetGuia), new { id = guia.Id }, guia);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar guía.");
                return StatusCode(500, "Error al procesar la guía.");
            }
        }

        /// <summary>
        /// Actualiza el estado de proceso de una guía específica.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuia(string id, [FromQuery] string estadoProceso)
        {
            var success = await _guiaService.UpdateEstadoProcesoAsync(id, estadoProceso);
            if (!success)
            {
                return BadRequest(new { Message = "Estado no válido o guía no encontrada." });
            }

            return NoContent();
        }

        /// <summary>
        /// Elimina una guía del sistema.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuia(string id)
        {
            var success = await _guiaService.DeleteGuiaAsync(id);
            return success ? NoContent() : NotFound();
        }
    }
}
