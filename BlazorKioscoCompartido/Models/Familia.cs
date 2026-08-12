
using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;


namespace BlazorKioscoCompartido.Models
{
    [Table("familias")]
    public class Familia:BaseModel
    {
        [PrimaryKey("id",false)]
        public long Id { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("nombre")]
        public string Nombre { get; set; }
        [Column("descripcion")]
        public string? Descripcion { get; set; }
        [Column("created_ad")]
        public DateTime fechaCreacion { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
