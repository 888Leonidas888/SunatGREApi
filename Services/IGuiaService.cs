using SunatGreApi.Models;
using SunatGreApi.Models.Dtos;

namespace SunatGreApi.Services
{
    /// <summary>
    /// Define la lógica de negocio para el procesamiento de Guías de Remisión.
    /// </summary>
    public interface IGuiaService
    {
        /// <summary>
        /// Obtiene guías filtradas y paginadas.
        /// </summary>
        Task<(IEnumerable<Guia> Data, int TotalRecords)> GetPagedGuiasAsync(DateTime? fecha, string? estadoProceso, string? etapa, int page, int pageSize);

        /// <summary>
        /// Obtiene una guía por su ID.
        /// </summary>
        Task<Guia?> GetGuiaByIdAsync(string id);

        /// <summary>
        /// Procesa y registra una nueva guía desde un DTO de SUNAT.
        /// </summary>
        /// <param name="dto">Datos de la guía enviados por SUNAT.</param>
        /// <returns>La guía registrada o null si fue omitida por reglas de negocio.</returns>
        Task<Guia?> ProcessAndRegisterGuiaAsync(SunatGreDto dto);

        /// <summary>
        /// Actualiza el estado de proceso de una guía.
        /// </summary>
        Task<bool> UpdateEstadoProcesoAsync(string id, string estadoProceso);

        /// <summary>
        /// Elimina una guía.
        /// </summary>
        Task<bool> DeleteGuiaAsync(string id);

        /// <summary>
        /// Enriquece los datos de la guía consultando fuentes externas.
        /// </summary>
        Task<bool> EnrichGuiaAsync(string guiaId);

        /// <summary>
        /// Valida la consistencia de los datos de la guía.
        /// </summary>
        Task<bool> ValidateGuiaAsync(string guiaId);

        /// <summary>
        /// Mapea un DTO de SUNAT a la entidad de dominio Guia.
        /// </summary>
        Guia MapToEntity(SunatGreDto greDto);
    }
}
