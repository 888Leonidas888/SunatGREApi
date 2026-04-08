using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SunatGreApi.Data;
using SunatGreApi.Models;
using SunatGreApi.Utils;
using SunatGreApi.Services;

namespace SunatGreApi.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class GuiaController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IGuiaService _guiaService;

        public GuiaController(AppDbContext context, IGuiaService guiaService)
        {
            _context = context;
            _guiaService = guiaService;
        }

        // GET: api/v1/Guia?page=1&pageSize=100&fecha=2026-03-11&estadoProceso=PENDIENTE&etapa=PRODUCCION
        [HttpGet]
        public async Task<IActionResult> GetGuias(
            [FromQuery] DateTime? fecha = null,
            [FromQuery] string? estadoProceso = null,
            [FromQuery] string? etapa = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 100;

            var query = _context.Guias.AsQueryable();

            // Filtrar por fecha si se proporciona
            if (fecha.HasValue)
            {
                var fechaBusqueda = fecha.Value.Date;
                query = query.Where(g => g.FechaEmision.Date == fechaBusqueda);
            }

            // Filtrar por estado de proceso si se proporciona
            if (!string.IsNullOrEmpty(estadoProceso))
            {
                query = query.Where(g => g.EstadoProceso == estadoProceso);
            }

            // Filtrar por etapa (PRODUCCION o DESARROLLO)
            if (!string.IsNullOrEmpty(etapa))
            {
                var centroCosto = new List<string>();
                if (etapa.Equals("PRODUCCION", StringComparison.OrdinalIgnoreCase))
                {
                    centroCosto.Add("1040001");
                    centroCosto.Add("1040004");
                }
                else if (etapa.Equals("DESARROLLO", StringComparison.OrdinalIgnoreCase))
                {
                    centroCosto.Add("1050001");
                }
                else
                {
                    centroCosto.Add("1050001");
                }
                query = query.Where(g => centroCosto.Contains(g.CodigoCentroCosto));
            }

            var totalRecords = await query.CountAsync();
            var guias = await query
                .Include(g => g.Bienes)
                .OrderByDescending(g => g.FechaEmision)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                Data = guias
            });
        }

        // GET: api/v1/Guia/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGuia(string id)
        {
            var guia = await _context.Guias
                .Include(g => g.Bienes)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (guia == null)
            {
                return NotFound();
            }

            return Ok(guia);
        }

        // POST: api/v1/Guia
        [HttpPost]
        public async Task<IActionResult> PostGuia(SunatGreApi.Models.Dtos.SunatGreDto dto)
        {
            // Validar si el estado es BAJA para omitir el procesamiento
            if (!string.IsNullOrEmpty(dto.DesEstado) && dto.DesEstado.Equals("BAJA", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new { Message = "La guía tiene estado BAJA y fue omitida." });
            }

            // Validar omitir ingresos si el detalle contiene 'TWILL'
            if (dto.Traslado?.Bien != null && dto.Traslado.Bien.Any(b => b.DesBien != null && b.DesBien.Contains("TWILL", StringComparison.OrdinalIgnoreCase)))
            {
                return Ok(new { Message = "La guía tiene descripción con la palabra TWILL y fue omitida." });
            }

            // Mapeo manual del DTO al Modelo de Base de Datos manejado ahora por la capa de servicio
            var guia = _guiaService.MapToEntity(dto);

            // 1. Validar si ya existe por ID (GUI de SUNAT)
            var existePorId = await _context.Guias.AnyAsync(g => g.Id == guia.Id);
            if (existePorId)
            {
                return Conflict(new { Message = $"La guía con el ID {guia.Id} ya está registrada." });
            }

            // 2. Validar si ya existe por clave natural (Ruc-Tipo-Serie-Numero)
            var existePorClaveNatural = await _context.Guias.AnyAsync(g => 
                g.RucEmisor == guia.RucEmisor && 
                g.TipoDocumento == guia.TipoDocumento && 
                g.Serie == guia.Serie && 
                g.Numero == guia.Numero);

            if (existePorClaveNatural)
            {
                return Conflict(new { Message = "Ya existe una guía registrada con el mismo RUC, Tipo, Serie y Número." });
            }

            _context.Guias.Add(guia);
            await _context.SaveChangesAsync();

            // Enriquecimiento de datos
            await _guiaService.EnrichGuiaAsync(guia.Id);

            // validacion de campos
            await _guiaService.ValidateGuiaAsync(guia.Id);

            return CreatedAtAction(nameof(GetGuia), new { id = guia.Id }, guia);
        }

        // PUT: api/v1/Guia/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuia(string id, string estadoProceso)
        {
            // 1. Definir la lista de palabras permitidas
            var estadosValidos = new[] { "PENDIENTE", "PROCESADO", "ERROR", "COMPLETADO" };

            // 2. Validar (usamos ToUpper para que no importe si escriben en minúsculas)
            if (string.IsNullOrEmpty(estadoProceso) || !estadosValidos.Contains(estadoProceso.ToUpper()))
            {
                return BadRequest(new 
                { 
                    Message = $"Estado no válido. Use uno de los siguientes: {string.Join(", ", estadosValidos)}" 
                });
            }

            var guia = await _context.Guias.FindAsync(id);
            if (guia == null)
            {
                return NotFound();
            }

            // 3. Asignar el valor (opcionalmente en mayúsculas para mantener orden)
            guia.EstadoProceso = estadoProceso.ToUpper();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/v1/Guia/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGuia(string id)
        {
            var guia = await _context.Guias.FindAsync(id);
            if (guia == null)
            {
                return NotFound();
            }

            _context.Guias.Remove(guia);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
