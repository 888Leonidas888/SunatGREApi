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
                LogProceso = "",
                Bienes = dto.Traslado.Bien.Select(b => new GuiaBien
                {
                    GuiaId = dto.Id,
                    NumOrden = b.NumOrden,
                    CodBien = b.CodBien,
                    DesBien = b.DesBien,
                    NombreComercial = SunatHelper.GetNombreComercial(b.DesBien ?? string.Empty),
                    CodUniMedida = b.CodUniMedida,
                    DesUniMedida = b.DesUniMedida,
                    NumCantidad = b.NumCantidad,
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
