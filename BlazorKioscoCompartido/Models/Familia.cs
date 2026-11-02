
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
        /// <summary>% de utilidad de la familia (ej: FERRETERIA 60). null = la general (40). Script 37.</summary>
        [Column("utilidad", Newtonsoft.Json.NullValueHandling.Ignore)]
        public decimal? Utilidad { get; set; }

        [Column("descripcion")]
        public string? Descripcion { get; set; }
        [Column("created_at")]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
