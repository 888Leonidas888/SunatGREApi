using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SunatGreApi.Models
{
    [Index(nameof(RucEmisor), nameof(TipoDocumento), nameof(Serie), nameof(Numero), IsUnique = true)]
    public class Guia
    {
        [Key]
        [Required]
        public string Id { get; set; } = string.Empty; // GUI de SUNAT

        [Required]
        public string Serie { get; set; } = string.Empty;

        [Required]
        public string Numero { get; set; } = string.Empty;

        [Required]
        public string RucEmisor { get; set; } = string.Empty;

        [Required]
        public string TipoDocumento { get; set; } = string.Empty; // Ej: 09 para Guía de Remisión

        public DateTime FechaEmision { get; set; }

        public string? Receptor { get; set; }

        public string? XmlPath { get; set; }

        public string? ZipPath { get; set; }

        public string? Estado { get; set; } // Ej: ACEPTADA, RECHAZADA, etc.

        public DateTime FechaCarga { get; set; } = DateTime.Now;

        public List<GuiaBien> Bienes { get; set; } = new();

        public string? Nota { get; set; } = string.Empty;
    }
}
