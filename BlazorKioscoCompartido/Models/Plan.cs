using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    /// <summary>Plan comercial (Básico, Medio, Avanzado...) con las funciones que trae. Ver _sql\20.planes_funciones.sql</summary>
    [Table("planes")]
    public class Plan : BaseModel
    {
        [PrimaryKey("codigo", true)]
        public string Codigo { get; set; } = "";

        [Column("nombre")]
        public string Nombre { get; set; } = "";

        [Column("orden")]
        public int Orden { get; set; }

        [Column("precio")]
        public decimal? Precio { get; set; }

        [Column("funciones")]
        public List<string> Funciones { get; set; } = new();
    }
}
