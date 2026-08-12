using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models.Vistas
{
    [Table("vw_resumen_formas_pago")]
    public class ResumenFormaPago : BaseModel
    {
        [PrimaryKey("forma_pago_id", false)]
        public long FormaPagoId { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("forma_pago")]
        public string FormaPago { get; set; } = "";

        [Column("fecha")]
        public DateTime Fecha { get; set; }

        [Column("cantidad_ventas")]
        public int CantidadVentas { get; set; }

        [Column("total_recaudado")]
        public decimal TotalRecaudado { get; set; }
    }
}
