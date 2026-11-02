using Supabase.Postgrest.Models;
using Supabase.Postgrest.Attributes;

namespace BlazorKioscoCompartido.Models
{
    [Table("personas")]
    public class Persona:BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }
        [Column("kiosco_id")]
        public long KioscoId { get; set; }
        [Column("nombre")]
        public string Nombre { get; set; } = "";
        [Column("habilitado")]
        public bool Habilitado { get; set; } = true;

        // --- datos del ABM de Clientes (si quedan vacíos no se mandan) ---
        [Column("telefono", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? Telefono { get; set; }

        /// <summary>Empleado del negocio (los que sacan fiado a precio de día).</summary>
        [Column("es_empleado", Newtonsoft.Json.NullValueHandling.Ignore)]
        public bool? EsEmpleado { get; set; }

        [Column("nota", Newtonsoft.Json.NullValueHandling.Ignore)]
        public string? Nota { get; set; }
    }
}
