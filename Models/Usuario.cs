using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;


namespace BlazorKioscos_v1.Models
{
    [Table("usuarios")]
    public class Usuario:BaseModel
    {
        [PrimaryKey("id", false)]
        public string? Id { get; set; } // El UUID de Supabase

        [Column("username")]
        public string? Username { get; set; }

        [Column("password")]
        public string? Password { get; set; }

        [Column("id_kiosco")]
        public long IdKiosco { get; set; }

        [Column("rol")]
        public string? Rol { get; set; }
        // Esta es la propiedad para el Join
        [Reference(typeof(Kiosco))]
        public Kiosco? Kiosco { get; set; }
    }
}
