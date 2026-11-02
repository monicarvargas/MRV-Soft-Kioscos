using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>
    /// Cuánto de una entrega se usó para una deuda de fiado (ver _sql\22.fiado_fifo.sql).
    /// La calcula sola la base (FIFO): la app solo la lee.
    /// </summary>
    [Table("fiado_aplicaciones")]
    public class FiadoAplicacion : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("persona_id")]
        public long PersonaId { get; set; }

        /// <summary>Renglón de ventas de la entrega (total negativo).</summary>
        [Column("pago_venta_id")]
        public long PagoVentaId { get; set; }

        /// <summary>Renglón de ventas de la deuda (fiado).</summary>
        [Column("deuda_venta_id")]
        public long DeudaVentaId { get; set; }

        [Column("monto")]
        public decimal Monto { get; set; }
    }
}
