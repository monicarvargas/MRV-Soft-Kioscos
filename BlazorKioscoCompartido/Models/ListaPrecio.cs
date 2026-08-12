using Supabase;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace BlazorKioscoCompartido.Models
{
    [Table("listas_precios")]
    public class ListaPrecio : BaseModel
    {
        [PrimaryKey("id", false)]
        public long Id { get; set; }

        [Column("kiosco_id")]
        public long KioscoId { get; set; }

        [Column("nombre_columna_productos")]
        public string? NombreColumnaProductos { get; set; }

        [Column("nombre_lista")]
        public string? NombreLista { get; set; }
        [Column("mostrar_en_buscador")]
        public bool MostrarEnBuscador { get; set; }

    }
}