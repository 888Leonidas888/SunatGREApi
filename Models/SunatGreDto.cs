using System.Text.Json.Serialization;

namespace SunatGreApi.Models.Dtos
{
    public class SunatGreDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("codCpe")]
        public string CodCpe { get; set; } = string.Empty;

        [JsonPropertyName("numRuc")]
        public string NumRuc { get; set; } = string.Empty;

        [JsonPropertyName("cantidad")]
        public double? Cantidad { get; set; }

        [JsonPropertyName("codTipoCpe")]
        public string CodTipoCpe { get; set; } = string.Empty;

        [JsonPropertyName("numSerie")]
        public string NumSerie { get; set; } = string.Empty;

        [JsonPropertyName("numCpe")]
        public int NumCpe { get; set; }

        [JsonPropertyName("codEstado")]
        public string CodEstado { get; set; } = string.Empty;

        [JsonPropertyName("desEstado")]
        public string DesEstado { get; set; } = string.Empty;

        [JsonPropertyName("emision")]
        public EmisionDto Emision { get; set; } = new();

        [JsonPropertyName("emisor")]
        public EmisorDto Emisor { get; set; } = new();

        [JsonPropertyName("traslado")]
        public TrasladoDto Traslado { get; set; } = new();

        [JsonPropertyName("receptor")]
        public ReceptorDto Receptor { get; set; } = new();
    }

    public class EmisionDto
    {
        [JsonPropertyName("fecEmision")]
        public string FecEmision { get; set; } = string.Empty;
        
        [JsonPropertyName("desNota")]
        public string? DesNota { get; set; }
    }

    public class EmisorDto
    {
        [JsonPropertyName("desNombre")]
        public string DesNombre { get; set; } = string.Empty;
    }

    public class ReceptorDto
    {
        [JsonPropertyName("desNombre")]
        public string DesNombre { get; set; } = string.Empty;

        [JsonPropertyName("numDocIdentidad")]
        public string NumDocIdentidad { get; set; } = string.Empty;
    }

    public class TrasladoDto
    {
        [JsonPropertyName("bien")]
        public List<BienDto> Bien { get; set; } = new();
    }

    public class BienDto
    {
        [JsonPropertyName("numOrden")]
        public int NumOrden { get; set; }

        [JsonPropertyName("codTipoDocumento")]
        public string? CodTipoDocumento { get; set; }

        [JsonPropertyName("desCortaTipoDocumento")]
        public string? DesCortaTipoDocumento { get; set; }

        [JsonPropertyName("numSerie")]
        public string? NumSerie { get; set; }

        [JsonPropertyName("numDocumento")]
        public string? NumDocumento { get; set; }

        [JsonPropertyName("numItem")]
        public string? NumItem { get; set; }

        [JsonPropertyName("indBienRegulado")]
        public string IndBienRegulado { get; set; } = string.Empty;

        [JsonPropertyName("codBien")]
        public string CodBien { get; set; } = string.Empty;

        [JsonPropertyName("codProductoSunat")]
        public string? CodProductoSunat { get; set; }

        [JsonPropertyName("codSubPartida")]
        public string? CodSubPartida { get; set; }

        [JsonPropertyName("codGtin")]
        public string? CodGtin { get; set; }

        [JsonPropertyName("desBien")]
        public string DesBien { get; set; } = string.Empty;

        [JsonPropertyName("codUniMedida")]
        public string CodUniMedida { get; set; } = string.Empty;

        [JsonPropertyName("desUniMedida")]
        public string DesUniMedida { get; set; } = string.Empty;

        [JsonPropertyName("numCantidad")]
        public double NumCantidad { get; set; }

        [JsonPropertyName("indFrecuente")]
        public string IndFrecuente { get; set; } = string.Empty;

        [JsonPropertyName("docRelacionado")]
        public string? DocRelacionado { get; set; }

        [JsonPropertyName("numDocTransporte")]
        public string? NumDocTransporte { get; set; }

        [JsonPropertyName("numDetalle")]
        public string? NumDetalle { get; set; }

        [JsonPropertyName("numContenedor")]
        public string? NumContenedor { get; set; }

        [JsonPropertyName("numPrecinto")]
        public string? NumPrecinto { get; set; }

        [JsonPropertyName("indContenedorVacio")]
        public string? IndContenedorVacio { get; set; }
    }
}
