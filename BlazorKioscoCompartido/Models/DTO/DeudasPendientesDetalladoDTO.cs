using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;


namespace BlazorKioscoCompartido.Models.DTO
{
    [Table("vw_deudas_pendientes_detallado")]
    public class DeudasPendientesDetalladoDTO:BaseModel
    {
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("persona_id")]
        public long PersonaId { get; set; }
        [Column("nombre")]
        public string NombrePersona { get; set; } = string.Empty;
        [Column("numero_unico_identificatorio_movimiento")]
        public long NumeroUnicoIdentificatorioMovimiento { get; set; }
        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow.ToLocalTime();
        [Column("producto_id")]
        public long ProductoId { get; set; }
        [Column("codigo_barras")]
        public string? CodigoBarras { get; set; }
        [Column("producto")]
        public string? Producto { get; set; }
        
        [Column("precio_actual")]
        public decimal PrecioActual { get; set; }
        [Column("precio_historico")]
        public decimal PrecioHistorico { get; set; }
        [Column("subtotal_actual")]
        public decimal SubtotalActual { get; set; }
        [Column("subtotal_historico")]
        public decimal SubtotalHistorico { get; set; }
    }
}
