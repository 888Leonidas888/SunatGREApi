using SunatGreApi.Models;

namespace SunatGreApi.Repositories
{
    /// <summary>
    /// Define las operaciones de persistencia para las Guías de Remisión en el almacenamiento local.
    /// </summary>
    public interface IGuiaRepository
    {
        /// <summary>
        /// Obtiene una lista de guías filtradas por fecha, estado de proceso y centro de costo.
        /// </summary>
        /// <param name="fecha">Fecha de emisión opcional.</param>
        /// <param name="estadoProceso">Estado de proceso (ej: PENDIENTE, OBSERVADO).</param>
        /// <param name="centrosCosto">Lista de códigos de centros de costo permitidos.</param>
        /// <param name="page">Número de página para paginación.</param>
        /// <param name="pageSize">Cantidad de registros por página.</param>
        /// <returns>Una lista de guías que cumplen con los criterios.</returns>
        Task<IEnumerable<Guia>> GetGuiasAsync(DateTime? fecha, string? estadoProceso, List<string>? centrosCosto, int page, int pageSize);

        /// <summary>
        /// Obtiene una guía por su identificador único (ID SUNAT).
        /// </summary>
        /// <param name="id">ID único de la guía.</param>
        /// <returns>La entidad Guia encontrada o null.</returns>
        Task<Guia?> GetByIdAsync(string id);

        /// <summary>
        /// Verifica si existe una guía por su identificador único.
        /// </summary>
        /// <param name="id">ID de SUNAT.</param>
        /// <returns>True si existe, False en caso contrario.</returns>
        Task<bool> ExistsByIdAsync(string id);

        /// <summary>
        /// Verifica si existe una guía por su clave natural (RUC, Tipo, Serie, Número).
        /// </summary>
        Task<bool> ExistsByNaturalKeyAsync(string ruc, string tipo, string serie, string numero);

        /// <summary>
        /// Agrega una nueva guía al almacenamiento local.
        /// </summary>
        /// <param name="guia">La entidad guía a persistir.</param>
        Task AddAsync(Guia guia);

        /// <summary>
        /// Actualiza una guía existente.
        /// </summary>
        /// <param name="guia">La entidad guía con cambios.</param>
        Task UpdateAsync(Guia guia);

        /// <summary>
        /// Elimina una guía por su ID.
        /// </summary>
        /// <param name="id">ID de SUNAT.</param>
        Task DeleteAsync(string id);

        /// <summary>
        /// Guarda los cambios en la base de datos local.
        /// </summary>
        Task SaveChangesAsync();
    }
}
