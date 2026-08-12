using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BlazorKioscoCompartido.Models
{
    [Table("personas")]
    internal class Persona:BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("nombre")]
        public string Nombre { get; set; }
        [Column("habilitado")]
        public bool Habilitado { get; set; } = true;
    }
}
