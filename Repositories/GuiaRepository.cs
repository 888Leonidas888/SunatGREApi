using Microsoft.EntityFrameworkCore;
using SunatGreApi.Data;
using SunatGreApi.Models;

namespace SunatGreApi.Repositories
{
    /// <summary>
    /// Implementación concreta del repositorio para SQLite utilizando Entity Framework Core.
    /// </summary>
    public class GuiaRepository : IGuiaRepository
    {
        private readonly AppDbContext _context;

        public GuiaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Guia>> GetGuiasAsync(DateTime? fecha, string? estadoProceso, List<string>? centrosCosto, int page, int pageSize)
        {
            var query = _context.Guias.AsQueryable();

            if (fecha.HasValue)
            {
                var fechaBusqueda = fecha.Value.Date;
                query = query.Where(g => g.FechaEmision.Date == fechaBusqueda);
            }

            if (!string.IsNullOrEmpty(estadoProceso))
            {
                query = query.Where(g => (g.EstadoProceso ?? string.Empty).ToUpper() == estadoProceso.ToUpper());
            }

            if (centrosCosto != null && centrosCosto.Any())
            {
                query = query.Where(g => centrosCosto.Contains(g.CodigoCentroCosto ?? string.Empty));
            }

            return await query
                .Include(g => g.Bienes)
                .OrderByDescending(g => g.FechaEmision)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Guia?> GetByIdAsync(string id)
        {
            return await _context.Guias
                .Include(g => g.Bienes)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<bool> ExistsByIdAsync(string id)
        {
            return await _context.Guias.AnyAsync(g => g.Id == id);
        }

        public async Task<bool> ExistsByNaturalKeyAsync(string ruc, string tipo, string serie, string numero)
        {
            return await _context.Guias.AnyAsync(g =>
                g.RucEmisor == ruc &&
                g.TipoDocumento == tipo &&
                g.Serie == serie &&
                g.Numero == numero);
        }

        public async Task AddAsync(Guia guia)
        {
            await _context.Guias.AddAsync(guia);
        }

        public async Task UpdateAsync(Guia guia)
        {
            _context.Guias.Update(guia);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(string id)
        {
            var guia = await _context.Guias.FindAsync(id);
            if (guia != null)
            {
                _context.Guias.Remove(guia);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
