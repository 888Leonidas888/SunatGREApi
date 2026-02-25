using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SunatGreApi.Models
{
    public class GuiaBien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InternalId { get; set; }

        [Required]
        public string GuiaId { get; set; } = string.Empty;

        [JsonIgnore]
        [ForeignKey(nameof(GuiaId))]
        public Guia? Guia { get; set; }

        public int NumOrden { get; set; }
        public string? CodTipoDocumento { get; set; }
        public string? DesCortaTipoDocumento { get; set; }
        public string? NumSerie { get; set; }
        public string? NumDocumento { get; set; }
        public string? NumItem { get; set; }
        public string IndBienRegulado { get; set; } = string.Empty;
        public string CodBien { get; set; } = string.Empty;
        public string? CodProductoSunat { get; set; }
        public string? CodSubPartida { get; set; }
        public string? CodGtin { get; set; }
        public string DesBien { get; set; } = string.Empty;
        public string CodUniMedida { get; set; } = string.Empty;
        public string DesUniMedida { get; set; } = string.Empty;
        public double NumCantidad { get; set; }
        public string IndFrecuente { get; set; } = string.Empty;
        public string? DocRelacionado { get; set; }
        public string? NumDocTransporte { get; set; }
        public string? NumDetalle { get; set; }
        public string? NumContenedor { get; set; }
        public string? NumPrecinto { get; set; }
        public string? IndContenedorVacio { get; set; }
        public string? Partida { get; set; } = string.Empty;
        public double? PesoBruto { get; set; } = 0;
        public int? Rollos { get; set; } = 0;
        public string? CodigoTela { get; set; } = string.Empty;
    }
}
