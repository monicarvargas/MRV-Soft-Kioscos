using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;
namespace BlazorKioscoCompartido.Models
{
    [Table("productos_varios_ventas")]
    public class ProductoVarioVenta : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("numero_unico_identificatorio_movimiento")]
        public long NumeroUnicoIdentificatorioMovimiento { get; set; } // El ID del Producto "Padre" (que tiene es_combo = true)
        [Column("nombre_producto")]
        public string? NombreProducto { get; set; } // El ID del Producto "Padre" (que tiene es_combo = true)
        [Column("cantidad")]
        public int Cantidad { get; set; }
        [Column("preciounitario")]
        public decimal PrecioUnitario { get; set; }
        [Column("created_at")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow.ToLocalTime();
        [Column("subtotal")]
        public decimal Subtotal { get; set; }
    }
}