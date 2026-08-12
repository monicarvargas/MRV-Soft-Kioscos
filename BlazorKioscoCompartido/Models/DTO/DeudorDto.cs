using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models.DTO
{
    [Table("vw_resumen_deudores")] // Nombre de tu vista en PostgreSQL / Supabase
    public class DeudorDto : BaseModel
    {
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("persona_id")]
        public long PersonaId { get; set; }
        [Column("nombre")]
        public string NombrePersona { get; set; } = string.Empty;
        [Column("total_pago_entregado")]
        public decimal TotalPagoEntregado { get; set; }
        [Column("deuda_precio_actual")]
        public decimal DeudaPrecioActual { get; set; }
        [Column("deuda_precio_historico")]
        public decimal DeudaPrecioHistorico { get; set; }
       
    }
}
