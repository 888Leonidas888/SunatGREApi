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

        // GET: api/v1/Guia?page=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetGuias([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var totalRecords = await _context.Guias.CountAsync();
            var guias = await _context.Guias
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
            // Mapeo manual del DTO al Modelo de Base de Datos
            var guia = new Guia
            {
                Id = dto.Id,
                Serie = dto.NumSerie,
                Numero = dto.NumCpe.ToString(),
                RucEmisor = dto.NumRuc,
                TipoDocumento = dto.CodTipoCpe,
                FechaEmision = DateTime.TryParse(dto.Emision.FecEmision, out var fec) ? fec : DateTime.Now,
                Receptor = dto.Receptor.DesNombre,
                Estado = dto.DesEstado,
                FechaCarga = DateTime.Now,
                Nota = dto.Emision.DesNota,
                // OrdenCompra = SunatHelper.GetOrdenCompra(dto.Emision.DesNota),
                Bienes = dto.Traslado.Bien.Select(b => new GuiaBien
                {
                    GuiaId = dto.Id,
                    NumOrden = b.NumOrden,
                    CodTipoDocumento = b.CodTipoDocumento,
                    DesCortaTipoDocumento = b.DesCortaTipoDocumento,
                    NumSerie = b.NumSerie,
                    NumDocumento = b.NumDocumento,
                    NumItem = b.NumItem,
                    IndBienRegulado = b.IndBienRegulado,
                    CodBien = b.CodBien,
                    CodProductoSunat = b.CodProductoSunat,
                    CodSubPartida = b.CodSubPartida,
                    CodGtin = b.CodGtin,
                    DesBien = b.DesBien,
                    CodUniMedida = b.CodUniMedida,
                    DesUniMedida = b.DesUniMedida,
                    NumCantidad = b.NumCantidad,
                    IndFrecuente = b.IndFrecuente,
                    DocRelacionado = b.DocRelacionado,
                    NumDocTransporte = b.NumDocTransporte,
                    NumDetalle = b.NumDetalle,
                    NumContenedor = b.NumContenedor,
                    NumPrecinto = b.NumPrecinto,
                    IndContenedorVacio = b.IndContenedorVacio,
                    Partida = SunatHelper.GetPartida(b.DesBien ?? string.Empty),
                    Rollos = SunatHelper.GetRollos(b.DesBien ?? string.Empty),
                    PesoBruto = SunatHelper.GetPesoBruto(b.DesBien ?? string.Empty)
                }).ToList()
            };

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

            return CreatedAtAction(nameof(GetGuia), new { id = guia.Id }, guia);
        }

        // PUT: api/v1/Guia/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGuia(string id, Guia guia)
        {
            if (id != guia.Id)
            {
                return BadRequest("El ID no coincide.");
            }

            _context.Entry(guia).State = EntityState.Modified;

            // Para actualizaciones, podrías necesitar lógica más compleja para los Bienes 
            // (borrar los anteriores y poner nuevos, o actualizar uno a uno).
            // Por simplicidad en este flujo de "reemplazo", borramos y volvemos a insertar si se desea.
            // Pero por ahora solo marcamos la guía como modificada.

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Guias.AnyAsync(e => e.Id == id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

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
