using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    [Table("lista_pedidos")]
    public class ListaPedido : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("proveedor_id")]
        public long ProveedorId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        [Column("monto_estimado")]
        public decimal MontoEstimado { get; set; }

        [Column("estado")]
        public string Estado { get; set; } = "Pendiente";

        [Column("observaciones")]
        public string? Observaciones { get; set; }
        [Column("nombre_listado")]
        public string? NombreListado { get; set; }
    }
}