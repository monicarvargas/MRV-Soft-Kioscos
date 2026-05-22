using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscos_v1.Models
{
    [Table("venta_detalles")]
    public class VentaDetalle : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("venta_id")]
        public long VentaId { get; set; }
        [Column("producto_id")]
        public long ProductoId { get; set; }
        [Column("cantidad")]
        public int Cantidad { get; set; }
        [Column("precio_unitario")]
        public decimal PrecioUnitario { get; set; }
        [Column("created_at")]
        public DateTime FechaCreacion { get; set; }=DateTime.UtcNow.ToLocalTime();
        [Column("subtotal")]
        public decimal Subtotal { get; set; }

        // Referencias para cargar los nombres si los necesitás
        [Reference(typeof(Producto))]
        public Producto? Producto { get; set; }
    }
}