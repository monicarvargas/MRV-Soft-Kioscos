using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BlazorKioscoCompartido.Models
{
    [Table("proveedores")]
    public class Proveedor:BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("nombre")]
        public string Nombre { get; set;}
        [Column("cuit")]
        public string? Cuit { get; set; }
        [Column("telefono")]
        public string? Telefono { get; set; }
        [Column("email")]
        public string? Email { get; set; }
        [Column("direccion")]
        public string? Direccion { get; set; }
        [Column("created_at")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow.ToLocalTime();
        [Column("columnas_pedido")]
        public string? ColumnasPedido { get; set; }
        /// <summary>false = dado de baja: no aparece para elegir en ningún lado (se conserva por el historial).
        /// null = la base todavía no tiene la columna (correr SQL 18) → se toma como activo.</summary>
        [Column("activo", Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? Activo { get; set; }
        public bool EstaActivo() => Activo != false;
    }
}
