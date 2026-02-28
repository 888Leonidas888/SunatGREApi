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
        public string CodBien { get; set; } = string.Empty;
        public string DesBien { get; set; } = string.Empty;
        public string? NombreComercial { get; set; } = string.Empty;
        public string CodUniMedida { get; set; } = string.Empty;
        public string DesUniMedida { get; set; } = string.Empty;
        public double NumCantidad { get; set; } = 0;
        public string? Partida { get; set; } = string.Empty;
        public double? PesoBruto { get; set; } = 0;
        public int? Rollos { get; set; } = 0;
        public string? CodigoTela { get; set; } = string.Empty;
    }
}
