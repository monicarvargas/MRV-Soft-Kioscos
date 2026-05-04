using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
namespace BlazorKioscos_v1.Models
{
    [Table("proveedores")]
    public class Proveedor:BaseModel
    {
        [PrimaryKey("id", false)]
        public int? Id { get; set; }
        [Column("kiosco_id")]
        public int? KioscoId { get; set; }
        [Column("nombre")]
        public string? Nombre { get; set;}
        [Column("cuit")]
        public string? Cuit { get; set; }
        [Column("telefono")]
        public string? Telefono { get; set; }
        [Column("email")]
        public string? Email { get; set; }
        [Column("direccion")]
        public string? Direccion { get; set; }
        [Column("created_ad")]
        public DateTime FechaCReacion { get; set; }
    }
}
